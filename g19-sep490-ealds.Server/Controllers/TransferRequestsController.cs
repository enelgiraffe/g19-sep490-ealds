using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/transfer")]
public class TransferRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public TransferRequestsController(EaldsDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransferRequest([FromBody] TransferRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        var title = dto.Title ?? $"Transfer asset {dto.AssetId} from {dto.FromLocationId} to {dto.ToLocationId}";

        var assetRequest = new AssetRequest
        {
            UserId = dto.CreatedBy,
            RequestTypeId = dto.RequestTypeId,
            AssetId = dto.AssetId,
            Title = title,
            Description = dto.Description,
            ProposedData = null,
            Status = 0,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var transfer = new TransferRecord
        {
            AssetId = dto.AssetId,
            AssetRequestId = assetRequest.AssetRequestId,
            FromLocationId = dto.FromLocationId,
            ToLocationId = dto.ToLocationId,
            FromUserId = dto.FromUserId,
            ToUserId = dto.ToUserId,
            TransferDate = dto.TransferDate ?? DateTime.UtcNow,
            ExecuteBy = dto.ExecuteBy
        };

        _db.TransferRecords.Add(transfer);

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = 0,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Transfer requested",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        return Ok(new { assetRequest.AssetRequestId, transfer.RecordId });
    }
}
