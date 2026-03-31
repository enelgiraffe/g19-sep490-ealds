using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/director")]
public class DirectorApproveController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _transferRequestTypeId;
    private readonly int _purchaseRequestTypeId;
    public DirectorApproveController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
        _purchaseRequestTypeId = configuration.GetValue<int>("App:PurchaseRequestTypeId", 1);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar==null) return NotFound();
        var fromStatus = ar.Status;
        var isTransfer =
            ar.RequestTypeId == _transferRequestTypeId
            || await _db.TransferRecords.AsNoTracking().AnyAsync(tr => tr.AssetRequestId == ar.AssetRequestId);

        // Director approval:
        // - Purchase/etc: status=1 (Waiting director approval) -> 2 (Approved)
        // - Transfer: status=2 (Waiting director approval after accountant) -> 4 (Approved)
        if (!(ar.Status == 1 || (isTransfer && ar.Status == 2)))
            return BadRequest("Only requests awaiting director decision can be approved by director.");

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);
        
        // Ensure Approval.StepId references an existing WorkflowStep (avoid FK violation when AssetRequest.StepId=0 or stale)
        WorkflowStep? step = null;
        if (ar.StepId != 0)
        {
            step = await _db.WorkflowSteps.AsNoTracking().FirstOrDefaultAsync(s => s.StepId == ar.StepId);
        }
        if (step == null)
        {
            var rt = await _db.RequestTypes.AsNoTracking().FirstOrDefaultAsync(x => x.RequestTypeId == ar.RequestTypeId);
            var workflowId = rt?.WorkflowId ?? 0;
            if (workflowId != 0)
            {
                var steps = await _db.WorkflowSteps.AsNoTracking()
                    .Where(s => s.WorkflowId == workflowId)
                    .OrderBy(s => s.StepOrder)
                    .ToListAsync();
                if (steps.Count > 0)
                {
                    var preferredRoleId = userRole?.RoleId ?? 0;
                    step =
                        (preferredRoleId != 0 ? steps.FirstOrDefault(s => s.RoleId == preferredRoleId) : null)
                        ?? steps.LastOrDefault()
                        ?? steps.FirstOrDefault();
                }
            }
        }
        if (step == null)
        {
            // Fallback for legacy data where RequestType.WorkflowId / WorkflowStep is not configured.
            // Use any existing WorkflowStep matching the approver's role to satisfy FK.
            var preferredRoleId = userRole?.RoleId ?? 0;
            step = preferredRoleId != 0
                ? await _db.WorkflowSteps.AsNoTracking()
                    .OrderBy(s => s.StepOrder)
                    .FirstOrDefaultAsync(s => s.RoleId == preferredRoleId)
                : null;
            step ??= await _db.WorkflowSteps.AsNoTracking().OrderBy(s => s.StepOrder).FirstOrDefaultAsync();
        }
        if (step == null)
            return BadRequest("No workflow steps exist in the system. Please configure WorkflowStep data first.");

        ar.StepId = step.StepId;
        var approval = new Approval
        {
            StepId = step.StepId,
            AssetRequestId = ar.AssetRequestId,
            Decision = 1,
            DecisionDate = DateTime.UtcNow,
            ApprovedUserId = dto.ApprovedBy,
            ApprovedRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment
        };
        _db.Approvals.Add(approval);

        ar.Status = isTransfer ? 4 : 2; // approved
        ar.ApproveDate = DateTime.UtcNow;

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = ar.Status,
            Action = 1,
            ActionByUserId = dto.ApprovedBy,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };
        _db.AssetRequestRecords.Add(record);



        // Bug 3 fix: create a Procurement record when a purchase request is approved by director
        var isPurchase = ar.RequestTypeId == _purchaseRequestTypeId;
        if (isPurchase && ar.Status == 2)
        {
            var alreadyHasProcurement = await _db.Procurements.AsNoTracking()
                .AnyAsync(p => p.AssetRequestId == ar.AssetRequestId);
            if (!alreadyHasProcurement)
            {
                var procurement = new Procurement
                {
                    AssetRequestId = ar.AssetRequestId,
                    ContractNo = string.Empty,      // To be filled by accountant later
                    ContractDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    TotalAmount = 0,
                    AdvanceAmount = 0,
                    RemainingAmount = 0,
                    Status = 0,                     // 0 = pending/initial
                    CreatedBy = dto.ApprovedBy,
                    CreateDate = DateTime.UtcNow
                };
                _db.Procurements.Add(procurement);
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status });
    }
}

