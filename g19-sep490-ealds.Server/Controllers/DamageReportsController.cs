using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/report-damage")]
public class DamageReportsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private const int DamageRequestTypeId = 4;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private const string DamageReportTitlePrefix = "Damage report -";

    public DamageReportsController(EaldsDbContext db, IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
    }

    [HttpPost]
    public async Task<IActionResult> Report([FromBody] ReportDamageDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (dto.AssetInstanceId <= 0 || dto.ReportedBy <= 0)
            return BadRequest("AssetInstanceId and ReportedBy are required.");

        if (dto.ReportDate == default)
            return BadRequest("ReportDate is required.");

        var instance = await _db.AssetInstances.FindAsync(dto.AssetInstanceId);
        if (instance == null)
            return NotFound("Asset instance not found.");
        if (!dto.RequestTypeId.HasValue || dto.RequestTypeId.Value <= 0)
            return BadRequest("RequestTypeId is required.");

        var initialStepId = await _db.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == dto.RequestTypeId.Value)
            .SelectMany(rt => _db.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            return BadRequest($"No workflow step configured for RequestTypeId '{dto.RequestTypeId.Value}'.");

        var assetRequest = new AssetRequest
        {
            UserId = dto.ReportedBy,
            RequestTypeId = dto.RequestTypeId.Value,
            AssetId = instance.AssetId,
            AssetInstanceId = instance.AssetInstanceId,
            // store a short title using the report date
            Title = $"Damage report - {dto.ReportDate:yyyy-MM-dd}",
            Description = dto.Description,
            // store document reference (if any) in ProposedData so frontend can retrieve it
            ProposedData = dto.DocumentId.HasValue ? dto.DocumentId.Value.ToString() : null,
            // Submitted to approval flow immediately
            Status = 1,
            CreatedBy = dto.ReportedBy,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value
        };

        _db.AssetRequests.Add(assetRequest);

        // Mark physical instance as damaged immediately after reporting
        instance.Status = (int)AssetStatus.Damaged;

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

        await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

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

        // Accept both legacy title-based rows and request-type-based rows.
        var isDamageByTitle = !string.IsNullOrWhiteSpace(ar.Title) && ar.Title.StartsWith(DamageReportTitlePrefix, StringComparison.OrdinalIgnoreCase);
        var isDamageByType = ar.RequestTypeId == DamageRequestTypeId;
        if (!isDamageByTitle && !isDamageByType)
            return BadRequest("Only damage report requests can be deleted via this endpoint.");

        // Remove children first because FK delete behavior is ClientSetNull (no cascade)
        var approvals = await _db.Approvals.Where(x => x.AssetRequestId == id).ToListAsync();
        var records = await _db.AssetRequestRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var repairTasks = await _db.RepairTasks.Where(x => x.AssetRequestId == id).ToListAsync();
        var maintenanceTasks = await _db.MaintenanceTasks.Where(x => x.AssetRequestId == id).ToListAsync();
        var disposalRecords = await _db.DisposalRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var transferRecords = await _db.TransferRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var procurements = await _db.Procurements.Where(x => x.AssetRequestId == id).ToListAsync();

        if (approvals.Count > 0) _db.Approvals.RemoveRange(approvals);
        if (records.Count > 0) _db.AssetRequestRecords.RemoveRange(records);
        if (repairTasks.Count > 0) _db.RepairTasks.RemoveRange(repairTasks);
        if (maintenanceTasks.Count > 0) _db.MaintenanceTasks.RemoveRange(maintenanceTasks);
        if (disposalRecords.Count > 0) _db.DisposalRecords.RemoveRange(disposalRecords);
        if (transferRecords.Count > 0) _db.TransferRecords.RemoveRange(transferRecords);
        if (procurements.Count > 0) _db.Procurements.RemoveRange(procurements);

        var tracked = new AssetRequest { AssetRequestId = id };
        _db.AssetRequests.Attach(tracked);
        _db.AssetRequests.Remove(tracked);

        await _db.SaveChangesAsync();
        return Ok(new { assetRequestId = id, deleted = true });
    }
}
