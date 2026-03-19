using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/accountant")]
public class AccountantRejectController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _transferRequestTypeId;

    public AccountantRejectController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar==null) return NotFound();
        var fromStatus = ar.Status;
        if (ar.Status != (int)AssetRequestStatus.Draft)
            return BadRequest("Only draft requests can be rejected by accountant.");

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);

        // Ensure Approval.StepId references an existing WorkflowStep (avoid FK violation on StepId=0)
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
                step = await _db.WorkflowSteps.AsNoTracking()
                    .Where(s => s.WorkflowId == workflowId)
                    .OrderBy(s => s.StepOrder)
                    .FirstOrDefaultAsync();
            }
        }
        if (step != null)
        {
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
        }
        
        ar.Status = (int)AssetRequestStatus.Rejected;
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
        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status });
    }
}
