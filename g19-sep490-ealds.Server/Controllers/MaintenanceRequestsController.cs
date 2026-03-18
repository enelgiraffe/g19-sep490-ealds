using System;
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
            AssetId = dto.AssetId,
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

        var task = new MaintenaceTask
        {
            ScheduleId = dto.ScheduleId,
            AssetRequestId = assetRequest.AssetRequestId,
            AssetId = dto.AssetId,
            PlannedDate = planned ?? DateTime.UtcNow,
            AssignTo = dto.AssignTo,
            Address = dto.Address,
            Status = 0,
            CreatDate = DateTime.UtcNow,
            CreateBy = dto.CreatedBy
        };

        _db.MaintenaceTasks.Add(task);

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

        // update schedule next due date if possible
        if (schedule != null && schedule.IntervalMonths.HasValue)
        {
            var baseDate = schedule.NextDueDate ?? schedule.StartDate;
            schedule.NextDueDate = baseDate.AddMonths(schedule.IntervalMonths.Value);
            _db.MaintenanceSchedules.Update(schedule);
        }

        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, taskId = task.TaskId });
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartMaintenance(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar == null) return NotFound();

        if (ar.Status != (int)AssetRequestStatus.Approved)
            return BadRequest("Only approved requests can be started.");

        var userRole = await _db.UserRoles.Include(ur => ur.Role).AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);
        var role = userRole?.Role;
        var allowed = role != null && (
            (role.Code != null && (role.Code.Equals("DepartmentManager", StringComparison.OrdinalIgnoreCase) || role.Code.Equals("Accountant", StringComparison.OrdinalIgnoreCase)))
            || (role.Name != null && (role.Name.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0 || role.Name.IndexOf("Director", StringComparison.OrdinalIgnoreCase) >= 0 || role.Name.IndexOf("Accountant", StringComparison.OrdinalIgnoreCase) >= 0))
        );

        if (!allowed) return Forbid();

        var from = ar.Status;
        ar.Status = (int)AssetRequestStatus.ConfirmedStart;

        // mark related maintenance task as in-progress
        var task = await _db.MaintenaceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == ar.AssetRequestId);
        if (task != null)
        {
            task.Status = 1; // in-progress
            _db.MaintenaceTasks.Update(task);
        }

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = from,
            ToStatus = ar.Status,
            Action = 2,
            ActionByUserId = dto.ApprovedBy,
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
        var task = await _db.MaintenaceTasks.FindAsync(taskId);
        if (task == null) return NotFound();

        var mr = new MaintenanceRecord
        {
            TaskId = task.TaskId,
            ExecutionDate = dto.ExecutionDate ?? DateTime.UtcNow,
            TotalCost = dto.TotalCost,
            WorkPerformed = dto.WorkPerformed ?? string.Empty,
            ConditionBefore = dto.ConditionBefore ?? string.Empty,
            ConditionAfter = dto.ConditionAfter ?? string.Empty,
            TechnicalNote = dto.TechnicalNote,
            Status = 1
        };

        _db.MaintenanceRecords.Add(mr);

        task.Status = 2; // completed
        _db.MaintenaceTasks.Update(task);

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
