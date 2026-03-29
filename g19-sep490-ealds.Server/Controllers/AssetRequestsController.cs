using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/purchase")]
public class AssetRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public AssetRequestsController(EaldsDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AssetRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

            var assetRequest = new AssetRequest
        {
            UserId = dto.UserId,
            RequestTypeId = dto.RequestTypeId,
            AssetInstanceId = dto.AssetId,
            Title = dto.Title,
            Description = dto.Description,
            ProposedData = dto.ProposedData,
            Status = (int)AssetRequestStatus.Draft,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = (int)AssetRequestStatus.Draft,
            ToStatus = (int)AssetRequestStatus.Draft,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Created request",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = assetRequest.AssetRequestId });
    }

    [HttpGet]
    [Route("/api/Assets/Requests/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ar = await _db.AssetRequests
            .Include(x => x.AssetInstance)
            .Include(x => x.User)
            .Include(x => x.RequestType)
            .Include(x => x.Approvals)
            .Include(x => x.AssetRequestRecords)
            .Include(x => x.MaintenanceTasks)
            .Include(x => x.RepairTasks)
            .Include(x => x.TransferRecords)
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
            user = ar.User == null ? null : new { ar.User.UserId, ar.User.Email },
            requestType = ar.RequestType == null ? null : new { ar.RequestType.RequestTypeId, ar.RequestType.WorkflowId },
            assetInstance = ar.AssetInstance == null ? null : new { ar.AssetInstance.AssetInstanceId, ar.AssetInstance.InstanceCode },
            approvals = ar.Approvals.Select(a => new { a.ApprovalId, a.DecisionDate, a.ApprovedUserId, a.ApprovedRoleId, a.StepId }),
            records = ar.AssetRequestRecords.Select(r => new { r.RecordId, r.FromStatus, r.ToStatus, r.Action, r.ActionByUserId, r.ActionRoleId, r.Comment, r.OccurredAt }),
            maintenanceTasks = ar.MaintenanceTasks.Select(t => new { t.TaskId, t.PlannedDate, t.Status, t.AssignTo }),
            repairTasks = ar.RepairTasks.Select(t => new { t.TaskId, t.EstimatedCost, t.Reason, t.Status }),
            transferRecords = ar.TransferRecords.Select(tr => new { tr.TransferId, tr.FromLocationId, tr.ToLocationId, tr.TransferDate }),
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

        var query = _db.AssetRequests.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (requestTypeId.HasValue)
            query = query.Where(x => x.RequestTypeId == requestTypeId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        var total = await query.CountAsync();

        var items = await query
            .Include(x => x.AssetInstance)
            .Include(x => x.User)
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
                assetInstanceId = ar.AssetInstanceId,
                requestTypeId = ar.RequestTypeId
            })
            .ToListAsync();

        var result = new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        };

        return Ok(result);
    }
}
