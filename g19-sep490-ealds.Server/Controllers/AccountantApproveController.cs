using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/accountant")]
public class AccountantApproveController : ControllerBase
{
    private readonly EaldsDbContext _db;
    public AccountantApproveController(EaldsDbContext db) => _db = db;

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar==null) return NotFound();

        var rt = await _db.RequestTypes.FindAsync(ar.RequestTypeId);
        int workflowId = rt?.WorkflowId ?? 0;
        var steps = await _db.WorkflowSteps.Where(s=>s.WorkflowId==workflowId).OrderBy(s=>s.StepOrder).ToListAsync();

        WorkflowStep currentStep = null!;
        if (ar.StepId==0) currentStep = steps.FirstOrDefault();
        else currentStep = steps.FirstOrDefault(s=>s.StepId==ar.StepId) ?? steps.FirstOrDefault();

        if (currentStep==null)
        {
            ar.Status = (int)AssetRequestStatus.Approved; ar.ApproveDate = DateTime.UtcNow;
        }
        else
        {
            var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);
            var approval = new Approval
            {
                StepId = currentStep.StepId,
                AssetRequestId = ar.AssetRequestId,
                Decision = 1,
                DecisionDate = DateTime.UtcNow,
                ApprovedUserId = dto.ApprovedBy,
                ApprovedRoleId = userRole?.RoleId ?? 0,
                Comment = dto.Comment
            };
            _db.Approvals.Add(approval);

            if (currentStep.IsFinalStep)
            {
                ar.Status = (int)AssetRequestStatus.Approved; ar.ApproveDate = DateTime.UtcNow;
            }
            else
            {
                var next = steps.FirstOrDefault(s => s.StepOrder > currentStep.StepOrder);
                if (next!=null) ar.StepId = next.StepId; else { ar.Status = (int)AssetRequestStatus.Approved; ar.ApproveDate = DateTime.UtcNow; }
            }
        }

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = (int)AssetRequestStatus.Draft,
            ToStatus = ar.Status,
            Action = 1,
            ActionByUserId = dto.ApprovedBy,
            ActionRoleId = (await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur=>ur.UserId==dto.ApprovedBy))?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };
        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();
        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status });
    }
}
