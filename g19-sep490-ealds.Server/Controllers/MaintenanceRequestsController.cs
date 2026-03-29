using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/maintenance")]
public class MaintenanceRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public MaintenanceRequestsController(EaldsDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> RequestExecution([FromBody] MaintenanceRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        var schedule = await _db.MaintenanceSchedules.FindAsync(dto.ScheduleId);

        var title = dto.Title ?? $"Maintenance request for asset {dto.AssetId}";

        var assetRequest = new AssetRequest
        {
            UserId = dto.CreatedBy,
            RequestTypeId = dto.RequestTypeId,
            AssetInstanceId = dto.AssetId,
            Title = title,
            Description = dto.Description,
            ProposedData = null,
            Status = (int)AssetRequestStatus.Draft,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var planned = dto.PlannedDate;
        if (!planned.HasValue && schedule != null)
            planned = schedule.NextDueDate ?? schedule.StartDate;

        var task = new MaintenanceTask
        {
            ScheduleId = dto.ScheduleId,
            AssetRequestId = assetRequest.AssetRequestId,
            AssetInstanceId = dto.AssetId,
            PlannedDate = planned ?? DateTime.UtcNow,
            AssignTo = dto.AssignTo,
            Address = dto.Address,
            Status = 0,
            CreateDate = DateTime.UtcNow,
            CreateBy = dto.CreatedBy
        };

        _db.MaintenanceTasks.Add(task);

        // create request record
        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = (int)AssetRequestStatus.Draft,
            ToStatus = (int)AssetRequestStatus.Draft,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Maintenance execution requested",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);

        // NOTE: Asset status is NOT changed here – only changed when maintenance actually starts (StartMaintenance).

        // update schedule next due date if possible
        if (schedule != null && schedule.IntervalValue.HasValue)
        {
            var baseDate = schedule.NextDueDate ?? schedule.StartDate;
            schedule.NextDueDate = baseDate.AddMonths(schedule.IntervalValue.Value);
            _db.MaintenanceSchedules.Update(schedule);
        }

        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, taskId = task.TaskId });
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartMaintenance(int id, [FromBody] MaintenanceStartDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar == null) return NotFound();

        if (ar.Status != (int)AssetRequestStatus.Approved)
            return BadRequest("Only approved requests can be started.");

        var userRole = await _db.UserRoles.Include(ur => ur.Role).AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.StartedBy);
        var role = userRole?.Role;
        var allowed = role != null && (
            (role.Code != null && (role.Code.Equals("DepartmentManager", StringComparison.OrdinalIgnoreCase) || role.Code.Equals("Accountant", StringComparison.OrdinalIgnoreCase)))
            || (role.Name != null && (role.Name.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0 || role.Name.IndexOf("Director", StringComparison.OrdinalIgnoreCase) >= 0 || role.Name.IndexOf("Accountant", StringComparison.OrdinalIgnoreCase) >= 0))
        );

        if (!allowed) return Forbid();

        var from = ar.Status;
        ar.Status = (int)AssetRequestStatus.ConfirmedStart;

        // mark related maintenance task as in-progress and persist start fields
        var task = await _db.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == ar.AssetRequestId);
        if (task != null)
        {
            if (dto.MaintenanceDate.HasValue)
                task.PlannedDate = dto.MaintenanceDate.Value;
            if (dto.PerformerUserId.HasValue && dto.PerformerUserId.Value > 0)
            {
                task.AssignTo = dto.PerformerUserId.Value;
                task.PerformerUserId = dto.PerformerUserId.Value;
            }
            if (!string.IsNullOrWhiteSpace(dto.Location))
                task.Address = dto.Location;
            task.MaintenanceProvider = dto.MaintenanceProvider;
            task.ExpectedCompletionDate = dto.ExpectedCompletionDate ?? dto.ExpectedCompletionTo;
            task.MaintenanceContent = dto.MaintenanceContent;
            task.LocationType = dto.LocationType;
            task.Status = 1;
            _db.MaintenanceTasks.Update(task);
        }

        if (!string.IsNullOrWhiteSpace(dto.DetailedDescription))
            ar.Description = dto.DetailedDescription;

        // Set AssetInstance status to UnderMaintenance when maintenance starts
        var linkedInstanceId = task?.AssetInstanceId ?? 0;
        if (linkedInstanceId > 0)
        {
            var instance = await _db.AssetInstances.FindAsync(linkedInstanceId);
            if (instance != null && instance.Status != (int)AssetStatus.UnderMaintenance)
            {
                instance.Status = (int)AssetStatus.UnderMaintenance;
                _db.AssetInstances.Update(instance);
                _db.AssetLifeCycles.Add(new AssetLifeCycle
                {
                    AssetInstanceId = instance.AssetInstanceId,
                    ActionType = 1, // StatusChanged
                    RelatedEntityType = 1,
                    RelatedEntityId = instance.AssetInstanceId,
                    ActorUserId = dto.StartedBy,
                    ActorRoleId = userRole?.RoleId ?? 0,
                    Description = $"Asset set to UnderMaintenance (maintenance started)",
                    OccurredAt = DateTime.UtcNow
                });
            }
        }

        Dictionary<string, object?> startData = new()
        {
            ["flowType"] = "maintenance-start",
            ["reportNumber"] = dto.ReportNumber,
            ["maintenanceDate"] = dto.MaintenanceDate,
            ["performerUserId"] = dto.PerformerUserId,
            ["maintenanceProvider"] = dto.MaintenanceProvider,
            ["estimatedCost"] = dto.EstimatedCost,
            ["expectedCompletionDate"] = dto.ExpectedCompletionDate,
            ["expectedCompletionFrom"] = dto.ExpectedCompletionFrom,
            ["expectedCompletionTo"] = dto.ExpectedCompletionTo,
            ["maintenanceContent"] = dto.MaintenanceContent,
            ["detailedDescription"] = dto.DetailedDescription,
            ["locationType"] = dto.LocationType,
            ["location"] = dto.Location,
            ["attachmentDocumentIds"] = dto.AttachmentDocumentIds,
            ["attachmentUrls"] = dto.AttachmentUrls
        };
        if (!string.IsNullOrWhiteSpace(ar.ProposedData))
            startData["legacyProposedData"] = ar.ProposedData;
        ar.ProposedData = JsonSerializer.Serialize(startData);

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = from,
            ToStatus = ar.Status,
            Action = 2,
            ActionByUserId = dto.StartedBy,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status, taskId = task?.TaskId });
    }

    [HttpPost("tasks/{taskId}/complete")]
    public async Task<IActionResult> CompleteMaintenance(int taskId, [FromBody] Models.DTOs.MaintenanceCompleteDto dto)
    {
        var task = await _db.MaintenanceTasks.FindAsync(taskId);
        if (task == null) return NotFound();

        var mr = new MaintenanceRecord
        {
            TaskId = task.TaskId,
            AssetInstanceId = task.AssetInstanceId,
            ExecutionDate = dto.ExecutionDate ?? DateTime.UtcNow,
            TotalCost = dto.TotalCost,
            WorkPerformed = dto.WorkPerformed ?? string.Empty,
            ConditionBefore = dto.ConditionBefore ?? string.Empty,
            ConditionAfter = dto.ConditionAfter ?? string.Empty,
            Status = 1
        };

        _db.MaintenanceRecords.Add(mr);

        task.Status = 2; // completed
        _db.MaintenanceTasks.Update(task);

        // add asset request record if linked
        if (task.AssetRequestId.HasValue)
        {
            var ar = await _db.AssetRequests.FindAsync(task.AssetRequestId.Value);
            if (ar != null)
            {
                var rec = new AssetRequestRecord
                {
                    AssetRequestId = ar.AssetRequestId,
                    FromStatus = ar.Status,
                    ToStatus = ar.Status,
                    Action = 3,
                    ActionByUserId = dto.CompletedBy,
                    ActionRoleId = 0,
                    Comment = "Maintenance completed",
                    OccurredAt = DateTime.UtcNow
                };

                _db.AssetRequestRecords.Add(rec);
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { recordId = mr.RecordId, taskId = task.TaskId });
    }
}
