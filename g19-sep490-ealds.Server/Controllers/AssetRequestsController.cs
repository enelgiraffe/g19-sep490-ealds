using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/purchase")]
public class AssetRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _purchaseRequestTypeId;

    public AssetRequestsController(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _purchaseRequestTypeId = configuration.GetValue<int>("App:PurchaseRequestTypeId", 1);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int? requestTypeId)
    {
        var typeId = requestTypeId ?? _purchaseRequestTypeId;
        var list = await _db.AssetRequests
            .AsNoTracking()
            .Include(r => r.Asset)
            .Include(r => r.User)
            .Where(r => r.RequestTypeId == typeId)
            .OrderByDescending(r => r.CreateDate)
            .Select(r => new AssetRequestListItemDTO
            {
                AssetRequestId = r.AssetRequestId,
                AssetId = r.AssetId,
                Title = r.Title,
                Description = r.Description,
                ProposedData = r.ProposedData,
                Status = r.Status,
                CreateDate = r.CreateDate,
                UserId = r.UserId,
                CreatedBy = r.CreatedBy,
                CreatorName = r.User != null
                    ? r.User.EmployeeUsers
                        .Select(e => e.Name)
                        .FirstOrDefault() ?? r.User.Email
                    : null,
                CreatorDepartmentName = r.User != null
                    ? r.User.EmployeeUsers.Select(e => e.Department != null ? e.Department.Name : null).FirstOrDefault()
                    : null,
                AssetCode = r.Asset != null ? r.Asset.Code : null,
                AssetName = r.Asset != null ? r.Asset.Name : null,
                AssetQuantity = r.Asset != null ? (int?)r.Asset.Quantity : null
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _db.AssetRequests
            .AsNoTracking()
            .Where(r => r.AssetRequestId == id && r.RequestTypeId == _purchaseRequestTypeId)
            .Select(r => new
            {
                r.AssetRequestId,
                r.AssetId,
                r.Title,
                r.Description,
                r.ProposedData,
                r.Status,
                r.CreateDate,
                r.UserId,
                r.CreatedBy,
                CreatorName = r.User != null
                    ? r.User.EmployeeUsers
                        .Select(e => e.Name)
                        .FirstOrDefault() ?? r.User.Email
                    : null,
                CreatorDepartmentName = r.User != null
                    ? r.User.EmployeeUsers.Select(e => e.Department != null ? e.Department.Name : null).FirstOrDefault()
                    : null,
                AssetCode = r.Asset != null ? r.Asset.Code : null,
                AssetName = r.Asset != null ? r.Asset.Name : null,
                AccountantComment = r.Approvals
                    .Where(a => a.ApprovedRole != null && a.ApprovedRole.Code == "ACCOUNTANT")
                    .OrderByDescending(a => a.DecisionDate)
                    .Select(a => a.Comment)
                    .FirstOrDefault(),
                DirectorComment = r.Approvals
                    .Where(a => a.ApprovedRole != null && a.ApprovedRole.Code == "DIRECTOR")
                    .OrderByDescending(a => a.DecisionDate)
                    .Select(a => a.Comment)
                    .FirstOrDefault(),
                Approvals = r.Approvals
                    .OrderByDescending(a => a.DecisionDate)
                    .Select(a => new
                    {
                        a.ApprovalId,
                        a.DecisionDate,
                        a.Comment,
                        RoleCode = a.ApprovedRole != null ? a.ApprovedRole.Code : null
                    })
            })
            .FirstOrDefaultAsync();
        if (request == null)
            return NotFound();
        return Ok(request);
    }

    /// <summary>
    /// Ensures purchase lines exist from ProposedData.equipment and returns them (for multi-line capitalization).
    /// </summary>
    [HttpGet("{id:int}/lines")]
    public async Task<IActionResult> GetPurchaseLines(int id)
    {
        var ar = await _db.AssetRequests
            .FirstOrDefaultAsync(r => r.AssetRequestId == id && r.RequestTypeId == _purchaseRequestTypeId);
        if (ar == null)
            return NotFound();

        await PurchaseRequestLineHelper.EnsureLinesAsync(_db, ar);

        var rows = await _db.AssetRequestPurchaseLines
            .AsNoTracking()
            .Where(l => l.AssetRequestId == id)
            .OrderBy(l => l.LineIndex)
            .Select(l => new AssetRequestPurchaseLineResponseDTO
            {
                LineId = l.LineId,
                LineIndex = l.LineIndex,
                ItemName = l.ItemName,
                Quantity = l.Quantity,
                Unit = l.Unit,
                ModelCode = l.ModelCode,
                EstimatedPrice = l.EstimatedPrice,
                AssetId = l.AssetId,
                CapitalizedAt = l.CapitalizedAt,
                AssetCode = l.Asset != null ? l.Asset.Code : null,
                AssetName = l.Asset != null ? l.Asset.Name : null,
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AssetRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        var desiredStatus = dto.Status ?? 0;
        if (desiredStatus != 0 && desiredStatus != -1)
            return BadRequest("Invalid status. Allowed: -1 (Draft), 0 (Submitted).");

        var requestTypeExists = await _db.RequestTypes
            .AsNoTracking()
            .AnyAsync(rt => rt.RequestTypeId == _purchaseRequestTypeId);
        if (!requestTypeExists)
        {
            return BadRequest($"Configured purchase RequestTypeId '{_purchaseRequestTypeId}' does not exist in RequestType table.");
        }
        var initialStepId = await _db.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _purchaseRequestTypeId)
            .SelectMany(rt => _db.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            return BadRequest($"No workflow step configured for RequestTypeId '{_purchaseRequestTypeId}'.");

        var assetRequest = new AssetRequest
        {
            UserId = dto.UserId,
            RequestTypeId = _purchaseRequestTypeId,
            AssetId = dto.AssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = dto.Title,
            Description = dto.Description,
            ProposedData = dto.ProposedData,
            Status = desiredStatus,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = assetRequest.Status,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = assetRequest.Status == -1 ? "Created draft request" : "Created request",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        if (desiredStatus == 0)
            await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return Ok(new { assetRequestId = assetRequest.AssetRequestId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AssetRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        var desiredStatus = dto.Status ?? -1;

        var ar = await _db.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == id && x.RequestTypeId == _purchaseRequestTypeId);
        if (ar == null) return NotFound();

        // Only draft (-1) can be edited; after submission/approval the request is immutable via this endpoint.
        if (ar.Status != -1)
            return BadRequest("Only draft purchase requests (status=-1) can be edited.");
        if (desiredStatus != -1 && desiredStatus != 0)
            return BadRequest("Invalid status. Allowed: -1 (Draft), 0 (Sent).");

        var fromStatus = ar.Status;

        ar.Title = dto.Title;
        ar.Description = dto.Description;
        ar.ProposedData = dto.ProposedData;
        ar.AssetId = dto.AssetId;
        ar.UserId = dto.UserId;
        ar.CreatedBy = dto.CreatedBy;
        ar.Status = desiredStatus;

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = ar.Status,
            Action = (fromStatus == -1 && desiredStatus == 0) ? 1 : 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment =
                (fromStatus == -1 && desiredStatus == 0) ? "Submitted request"
                : "Updated draft request",
            OccurredAt = DateTime.UtcNow
        };
        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        if (fromStatus == -1 && desiredStatus == 0)
            await _requestNotifications.NotifyFirstApproversAsync(ar.AssetRequestId);

        return Ok(new { assetRequestId = ar.AssetRequestId });
    }

    [HttpGet]
    [Route("/api/Assets/Requests/{id}")]
    public async Task<IActionResult> GetDetails(int id)
    {
        var ar = await _db.AssetRequests
            .Include(x => x.Asset)
            .Include(x => x.User)
            .Include(x => x.RequestType)
            .Include(x => x.Approvals)
            .Include(x => x.AssetRequestRecords)
            .Include(x => x.MaintenanceTasks)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(x => x.RepairTasks)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(x => x.RepairTasks)
                .ThenInclude(t => t.Supplier)
            .Include(x => x.TransferRecords)
                .ThenInclude(tr => tr.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(x => x.Procurements)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetRequestId == id);

        if (ar == null)
            return NotFound();

        var result = new
        {
            id = ar.AssetRequestId,
            title = ar.Title,
            description = ar.Description,
            proposedData = ar.ProposedData,
            status = ar.Status,
            createDate = ar.CreateDate,
            approveDate = ar.ApproveDate,
            stepId = ar.StepId,
            requestTypeId = ar.RequestTypeId,
            user = ar.User == null ? null : new { ar.User.UserId, ar.User.Email },
            requestType = ar.RequestType == null ? null : new { ar.RequestType.RequestTypeId, ar.RequestType.WorkflowId },
            // Catalog asset (for purchase / damage-report requests)
            asset = ar.Asset == null ? null : new { ar.Asset.AssetId, ar.Asset.Name, ar.Asset.Code, ar.Asset.Quantity },
            approvals = ar.Approvals.Select(a => new { a.ApprovalId, a.DecisionDate, a.ApprovedUserId, a.ApprovedRoleId, a.StepId }),
            records = ar.AssetRequestRecords.Select(r => new { r.RecordId, r.FromStatus, r.ToStatus, r.Action, r.ActionByUserId, r.ActionRoleId, r.Comment, r.OccurredAt }),
            // Child tasks — each stores the specific instance via AssetInstanceId
            maintenanceTasks = ar.MaintenanceTasks.Select(t => new
            {
                t.TaskId,
                t.PlannedDate,
                t.Status,
                t.AssignTo,
                t.AssetInstanceId,
                InstanceCode = t.AssetInstance != null ? t.AssetInstance.InstanceCode : null,
                AssetName = t.AssetInstance != null && t.AssetInstance.Asset != null ? t.AssetInstance.Asset.Name : null
            }),
            repairTasks = ar.RepairTasks.Select(t => new
            {
                t.TaskId,
                t.EstimatedCost,
                damageCondition = t.Reason,
                t.Status,
                t.RepairDate,
                t.AssetInstanceId,
                t.SupplierId,
                supplierName = t.Supplier != null ? t.Supplier.Name : null,
                InstanceCode = t.AssetInstance != null ? t.AssetInstance.InstanceCode : null,
                AssetName = t.AssetInstance != null && t.AssetInstance.Asset != null ? t.AssetInstance.Asset.Name : null
            }),
            transferRecords = ar.TransferRecords.Select(tr => new
            {
                tr.TransferId,
                tr.AssetRequestId,
                tr.AssetInstanceId,
                tr.FromLocationId,
                tr.ToLocationId,
                tr.TransferDate,
                InstanceCode = tr.AssetInstance != null ? tr.AssetInstance.InstanceCode : null,
                AssetName = tr.AssetInstance != null && tr.AssetInstance.Asset != null ? tr.AssetInstance.Asset.Name : null
            }),
            procurements = ar.Procurements.Select(p => new { p.ProcurementId })
        };

        return Ok(result);
    }

    [HttpGet]
    [Route("/api/Assets/Requests")]
    public async Task<IActionResult> List([FromQuery] int? status, [FromQuery] int? requestTypeId, [FromQuery] int? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _db.AssetRequests
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (requestTypeId.HasValue)
            query = query.Where(x => x.RequestTypeId == requestTypeId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreateDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ar => new
            {
                id = ar.AssetRequestId,
                title = ar.Title,
                description = ar.Description,
                status = ar.Status,
                createDate = ar.CreateDate,
                userId = ar.UserId,
                userEmail = ar.User != null ? ar.User.Email : null,
                assetId = ar.AssetId,
                assetInstanceId = ar.AssetInstanceId,
                assetCode = ar.Asset != null ? ar.Asset.Code : null,
                assetInstanceCode = ar.AssetInstance != null ? ar.AssetInstance.InstanceCode : null,
                assetName = ar.Asset != null ? ar.Asset.Name : null,
                assetQuantity = ar.Asset != null ? (int?)ar.Asset.Quantity : null,
                currentDepartmentName = ar.AssetInstance != null
                    ? ar.AssetInstance.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Department != null ? al.Department.Name : null)
                        .FirstOrDefault()
                    : null,
                currentLocation = ar.AssetInstance != null
                    ? ar.AssetInstance.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Note)
                        .FirstOrDefault() ?? (ar.AssetInstance.Warehouse != null ? ar.AssetInstance.Warehouse.Name : null)
                    : null,
                requestTypeId = ar.RequestTypeId
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    /// <summary>
    /// POST /api/Assets/Requests/purchase/{id}/revert-to-draft
    /// Reverts a submitted purchase request (status=0) back to draft (status=-1).
    /// Only the creator can revert their own request.
    /// </summary>
    [HttpPost("{id:int}/revert-to-draft")]
    public async Task<IActionResult> RevertToDraft(int id, [FromBody] RevertToDraftDTO dto)
    {
        if (dto == null || dto.UserId <= 0)
            return BadRequest(new { message = "UserId is required." });

        var request = await _db.AssetRequests
            .Where(r => r.AssetRequestId == id && r.RequestTypeId == _purchaseRequestTypeId)
            .FirstOrDefaultAsync();

        if (request == null)
            return NotFound(new { message = $"Purchase request with id {id} not found." });

        if (request.Status != 0)
            return BadRequest(new { message = "Only submitted requests (status=0) can be reverted to draft." });

        if (request.CreatedBy != dto.UserId)
            return Forbid();

        request.Status = -1;
        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = request.AssetRequestId, status = request.Status });
    }
}
