using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/director")]
public class DirectorApproveController : ControllerBase
{
    private readonly EaldsDbContext _db;
    public DirectorApproveController(EaldsDbContext db) => _db = db;

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar==null) return NotFound();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);
        var approval = new Approval
        {
            StepId = ar.StepId,
            AssetRequestId = ar.AssetRequestId,
            Decision = 1,
            DecisionDate = DateTime.UtcNow,
            ApprovedUserId = dto.ApprovedBy,
            ApprovedRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment
        };
        _db.Approvals.Add(approval);

        ar.Status = 1; ar.ApproveDate = DateTime.UtcNow;

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = 0,
            ToStatus = ar.Status,
            Action = 1,
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
