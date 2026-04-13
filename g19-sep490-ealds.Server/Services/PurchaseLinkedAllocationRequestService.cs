using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.Allocation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services;

/// <summary>Auto-create allocation requests from approved purchase requisitions; unlock when PO is fully received.</summary>
public static class PurchaseLinkedAllocationRequestService
{
    /// <summary>Fully received PO (same code as PurchaseOrdersController.StatusCompleted).</summary>
    private const int ProcurementFullyReceivedStatus = 3;

    /// <summary>After director approves PR: one allocation request (same lines as PR) in status AwaitingGoodsReceipt.</summary>
    public static async Task TryCreateAwaitingGoodsReceiptAllocationAsync(
        EaldsDbContext db,
        AssetRequest purchaseRequest,
        int allocationRequestTypeId,
        int departmentHeadRoleId,
        int recordActorUserId,
        CancellationToken cancellationToken = default)
    {
        if (purchaseRequest.RequestTypeId <= 0)
            return;

        var exists = await db.AssetRequests.AsNoTracking()
            .AnyAsync(
                r => r.RequestTypeId == allocationRequestTypeId &&
                     r.SourcePurchaseRequestId == purchaseRequest.AssetRequestId,
                cancellationToken);
        if (exists)
            return;

        if (!await db.RequestTypes.AsNoTracking().AnyAsync(rt => rt.RequestTypeId == allocationRequestTypeId, cancellationToken))
            return;

        var workflowId = await db.RequestTypes.AsNoTracking()
            .Where(rt => rt.RequestTypeId == allocationRequestTypeId)
            .Select(rt => (int?)rt.WorkflowId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!workflowId.HasValue || workflowId.Value <= 0)
            return;

        var initialStepId = await db.WorkflowSteps.AsNoTracking()
            .Where(ws => ws.WorkflowId == workflowId.Value)
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!initialStepId.HasValue)
            return;

        await PurchaseRequestLineHelper.EnsureLinesAsync(db, purchaseRequest, cancellationToken);

        var purchaseLines = await db.AssetRequestPurchaseLines.AsNoTracking()
            .Where(l => l.AssetRequestId == purchaseRequest.AssetRequestId && l.AssetId != null && l.AssetId > 0)
            .ToListAsync(cancellationToken);
        if (purchaseLines.Count == 0)
            return;

        var grouped = purchaseLines
            .GroupBy(l => l.AssetId!.Value)
            .Select(g => new { AssetId = g.Key, Quantity = g.Sum(x => x.Quantity < 1 ? 1 : x.Quantity) })
            .ToList();

        var departmentId = await db.Employees.AsNoTracking()
            .Where(e => e.UserId == purchaseRequest.CreatedBy)
            .OrderBy(e => e.EmployeeId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!departmentId.HasValue)
            return;

        var lineInputs = new List<AllocationLineInputDto>();
        foreach (var g in grouped)
        {
            var asset = await db.Assets.AsNoTracking()
                .FirstOrDefaultAsync(a => a.AssetId == g.AssetId, cancellationToken);
            if (asset == null)
                continue;

            lineInputs.Add(new AllocationLineInputDto
            {
                AssetTypeId = asset.AssetTypeId,
                AssetId = g.AssetId,
                Quantity = g.Quantity,
                Reason = $"Theo yêu cầu mua #{purchaseRequest.AssetRequestId}"
            });
        }

        if (lineInputs.Count == 0)
            return;

        var proposedJson = AllocationOrderWorkflow.BuildProposedDataJson(departmentId.Value, lineInputs);
        if (!AllocationOrderWorkflow.TryParseProposedData(proposedJson, out var root, out _) || root == null)
            return;

        var validation = await AllocationOrderWorkflow.ValidateLinesAgainstDatabaseAsync(
            db,
            root,
            cancellationToken,
            validateWarehouseAvailability: false);
        if (validation != null)
            return;

        var titleBase = string.IsNullOrWhiteSpace(purchaseRequest.Title)
            ? $"YC mua #{purchaseRequest.AssetRequestId}"
            : purchaseRequest.Title.Trim();
        var assetRequest = new AssetRequest
        {
            UserId = purchaseRequest.CreatedBy,
            RequestTypeId = allocationRequestTypeId,
            AssetId = null,
            AssetInstanceId = null,
            Title = $"Cấp phát theo PR: {titleBase}",
            Description = $"Tự động từ yêu cầu mua #{purchaseRequest.AssetRequestId} sau duyệt giám đốc. Chờ nhận đủ hàng (biên nhận) mới chuyển chờ kế toán.",
            ProposedData = proposedJson,
            Status = AllocationOrderWorkflow.RequestStatusAwaitingGoodsReceipt,
            CreatedBy = recordActorUserId,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value,
            AllocationTargetDepartmentId = departmentId.Value,
            SourcePurchaseRequestId = purchaseRequest.AssetRequestId
        };

        assetRequest.AssetRequestRecords.Add(new AssetRequestRecord
        {
            FromStatus = 0,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = recordActorUserId,
            ActionRoleId = departmentHeadRoleId,
            Comment = "Tự động: yêu cầu cấp phát từ PR (chờ nhận hàng)",
            OccurredAt = DateTime.UtcNow
        });

        db.AssetRequests.Add(assetRequest);
    }

    /// <summary>When procurement is fully received, move linked allocation requests to pending accountant.</summary>
    public static async Task<IReadOnlyList<int>> TryPromoteAwaitingGoodsReceiptForProcurementAsync(
        EaldsDbContext db,
        int procurementId,
        int allocationRequestTypeId,
        CancellationToken cancellationToken = default)
    {
        var proc = await db.Procurements.AsNoTracking()
            .Where(p => p.ProcurementId == procurementId)
            .Select(p => new { p.AssetRequestId, p.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (proc == null || proc.AssetRequestId == null || proc.Status != ProcurementFullyReceivedStatus)
            return Array.Empty<int>();

        var prId = proc.AssetRequestId.Value;
        var rows = await db.AssetRequests
            .Where(r =>
                r.RequestTypeId == allocationRequestTypeId &&
                r.SourcePurchaseRequestId == prId &&
                r.Status == AllocationOrderWorkflow.RequestStatusAwaitingGoodsReceipt)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<int>();

        foreach (var r in rows)
            r.Status = AllocationOrderWorkflow.RequestStatusPendingAccountant;

        await db.SaveChangesAsync(cancellationToken);

        return rows.Select(r => r.AssetRequestId).ToList();
    }
}
