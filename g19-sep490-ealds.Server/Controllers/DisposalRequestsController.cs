using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Disposal")]
public class DisposalRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public DisposalRequestsController(EaldsDbContext db)
    {
        _db = db;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] AssetDisposalRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        if (!dto.AssetId.HasValue)
            return BadRequest("AssetId is required.");

        var asset = await _db.Assets.FindAsync(dto.AssetId.Value);
        if (asset == null)
            return NotFound($"Asset with id {dto.AssetId.Value} not found.");

        // permission check: only department managers can submit disposal
        var userRoles = await _db.UserRoles.Include(ur => ur.Role).AsNoTracking().Where(ur => ur.UserId == dto.CreatedBy).ToListAsync();
        var isDeptManager = userRoles.Any(ur =>
            (ur.Role.Code != null && ur.Role.Code.Equals("DepartmentManager", StringComparison.OrdinalIgnoreCase)) ||
            (ur.Role.Name != null && ur.Role.Name.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0)
        );

        if (!isDeptManager)
            return Forbid();

        var assetRequest = new AssetRequest
        {
            UserId = dto.UserId,
            RequestTypeId = dto.RequestTypeId ?? 0,
            AssetId = dto.AssetId,
            Title = dto.Title,
            Description = dto.Description,
            ProposedData = null,
            Status = 1,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var diposal = new DiposalRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            AssetId = dto.AssetId.Value,
            DiposalMethod = dto.DiposalMethod,
            DiposalValue = dto.DiposalValue,
            DiposalDate = dto.DiposalDate,
            Reason = dto.Reason,
            ExecutedBy = dto.CreatedBy
        };

        _db.DiposalRecords.Add(diposal);

        // mark asset as disposed
        asset.Status = (int)AssetStatus.Disposed;

        await _db.SaveChangesAsync();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = 1,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Submitted disposal request",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, diposalId = diposal.DiposalId });
    }
}
