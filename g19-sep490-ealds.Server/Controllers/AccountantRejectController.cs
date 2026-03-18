using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/accountant")]
public class AccountantRejectController : ControllerBase
{
    private readonly EaldsDbContext _db;
    public AccountantRejectController(EaldsDbContext db) => _db = db;

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar==null) return NotFound();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);
        var approval = new Approval
        {
            StepId = ar.StepId==0?0:ar.StepId,
            AssetRequestId = ar.AssetRequestId,
            Decision = 2,
            DecisionDate = DateTime.UtcNow,
            ApprovedUserId = dto.ApprovedBy,
            ApprovedRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment
        };
        _db.Approvals.Add(approval);

        ar.Status = (int)AssetRequestStatus.Rejected; ar.ApproveDate = DateTime.UtcNow;

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = (int)AssetRequestStatus.Draft,
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
