using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/director")]
public class DirectorRejectController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _transferRequestTypeId;

    public DirectorRejectController(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar == null) return NotFound();
        var fromStatus = ar.Status;
        var isTransfer =
            ar.RequestTypeId == _transferRequestTypeId
            || await _db.TransferRecords.AsNoTracking().AnyAsync(tr => tr.AssetRequestId == ar.AssetRequestId);

        // Director rejection:
        // - Purchase/etc: status=1 -> 3 (Rejected)
        // - Transfer: status=2 -> 3 (Rejected)
        if (!(ar.Status == 1 || (isTransfer && ar.Status == 2)))
            return BadRequest("Only requests awaiting director decision can be rejected by director.");

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
            Decision = 2,
            DecisionDate = DateTime.UtcNow,
            ApprovedUserId = dto.ApprovedBy,
            ApprovedRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment
        };
        _db.Approvals.Add(approval);

        ar.Status = 3; // rejected
        ar.ApproveDate = DateTime.UtcNow;

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = ar.Status,
            Action = 2,
            ActionByUserId = dto.ApprovedBy,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };
        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        await _requestNotifications.NotifySenderDecisionAsync(ar.AssetRequestId, false, dto.ApprovedBy);

        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status });
    }
}
