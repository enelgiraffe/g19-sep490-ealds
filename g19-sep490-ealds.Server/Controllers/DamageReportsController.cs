using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/report-damage")]
public class DamageReportsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public DamageReportsController(EaldsDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Report([FromBody] ReportDamageDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (dto.AssetId <= 0 || dto.ReportedBy <= 0)
            return BadRequest("AssetId and ReportedBy are required.");

        if (dto.ReportDate == default)
            return BadRequest("ReportDate is required.");

        var assetRequest = new AssetRequest
        {
            UserId = dto.ReportedBy,
            RequestTypeId = dto.RequestTypeId ?? 0,
            AssetId = dto.AssetId,
            // store a short title using the report date
            Title = $"Damage report - {dto.ReportDate:yyyy-MM-dd}",
            Description = dto.Description,
            // store document reference (if any) in ProposedData so frontend can retrieve it
            ProposedData = dto.DocumentId.HasValue ? dto.DocumentId.Value.ToString() : null,
            Status = (int)AssetRequestStatus.Draft,
            CreatedBy = dto.ReportedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ReportedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = (int)AssetRequestStatus.Draft,
            ToStatus = (int)AssetRequestStatus.Draft,
            Action = 0,
            ActionByUserId = dto.ReportedBy,
            ActionRoleId = actionRoleId,
            Comment = "Damage reported",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        // If a DocumentId was provided, validate it exists and optionally link/downloadable info
        if (dto.DocumentId.HasValue)
        {
            var doc = await _db.Documents.FindAsync(dto.DocumentId.Value);
            if (doc == null)
                return BadRequest("Document not found.");
        }

        // Return Created pointing to the repair request path where frontend can proceed to create a repair request.
        var location = $"/api/Assets/Requests/repair";
        return Created(location, new { assetRequestId = assetRequest.AssetRequestId });
    }
}
