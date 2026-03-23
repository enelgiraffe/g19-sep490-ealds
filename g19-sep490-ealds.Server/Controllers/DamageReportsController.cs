using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/report-damage")]
public class DamageReportsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private const string DamageReportTitlePrefix = "Damage report -";

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

        var asset = await _db.Assets.FindAsync(dto.AssetId);
        if (asset == null)
            return NotFound("Asset not found.");

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
            // Submitted to approval flow immediately
            Status = 1,
            CreatedBy = dto.ReportedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);

        // Mark asset as damaged immediately after reporting
        asset.Status = (int)AssetStatus.Damaged;

        await _db.SaveChangesAsync();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ReportedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 1,
            ToStatus = 1,
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

        // Return location of the created request resource.
        var location = $"/api/Assets/Requests/{assetRequest.AssetRequestId}";
        return Created(location, new { assetRequestId = assetRequest.AssetRequestId });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ar = await _db.AssetRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetRequestId == id);

        if (ar == null)
            return NotFound();

        // Only allow deleting damage-report requests created by this controller
        if (string.IsNullOrWhiteSpace(ar.Title) || !ar.Title.StartsWith(DamageReportTitlePrefix))
            return BadRequest("Only damage report requests can be deleted via this endpoint.");

        // Remove children first because FK delete behavior is ClientSetNull (no cascade)
        var approvals = await _db.Approvals.Where(x => x.AssetRequestId == id).ToListAsync();
        var records = await _db.AssetRequestRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var repairTasks = await _db.RepairTasks.Where(x => x.AssetRequestId == id).ToListAsync();
        var maintenanceTasks = await _db.MaintenaceTasks.Where(x => x.AssetRequestId == id).ToListAsync();
        var disposalRecords = await _db.DiposalRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var transferRecords = await _db.TransferRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var procurements = await _db.Procurements.Where(x => x.AssetRequestId == id).ToListAsync();

        if (approvals.Count > 0) _db.Approvals.RemoveRange(approvals);
        if (records.Count > 0) _db.AssetRequestRecords.RemoveRange(records);
        if (repairTasks.Count > 0) _db.RepairTasks.RemoveRange(repairTasks);
        if (maintenanceTasks.Count > 0) _db.MaintenaceTasks.RemoveRange(maintenanceTasks);
        if (disposalRecords.Count > 0) _db.DiposalRecords.RemoveRange(disposalRecords);
        if (transferRecords.Count > 0) _db.TransferRecords.RemoveRange(transferRecords);
        if (procurements.Count > 0) _db.Procurements.RemoveRange(procurements);

        var tracked = new AssetRequest { AssetRequestId = id };
        _db.AssetRequests.Attach(tracked);
        _db.AssetRequests.Remove(tracked);

        await _db.SaveChangesAsync();
        return Ok(new { assetRequestId = id, deleted = true });
    }
}
