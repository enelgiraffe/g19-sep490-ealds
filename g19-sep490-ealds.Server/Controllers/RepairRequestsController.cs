using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/repair")]
public class RepairRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public RepairRequestsController(EaldsDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRepairRequest([FromBody] RepairRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest("Reason is required.");

        if (dto.DamageDate.HasValue && dto.DamageDate.Value.Date > DateTime.UtcNow.Date)
            return BadRequest("Ngày hỏng không được lớn hơn ngày hiện tại.");

        var title = dto.Title ?? $"Repair request for asset {dto.AssetId}";

        var assetRequest = new AssetRequest
        {
            UserId = dto.CreatedBy,
            RequestTypeId = dto.RequestTypeId,
            AssetId = dto.AssetId,
            Title = title,
            Description = dto.Description ?? dto.Reason,
            ProposedData = null,
            Status = (int)AssetRequestStatus.Draft,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var repairTask = new RepairTask
        {
            AssetRequestId = assetRequest.AssetRequestId,
            AssetId = dto.AssetId,
            EstimatedCost = dto.EstimatedCost,
            Reason = dto.Reason,
            Status = 0
        };

        _db.RepairTasks.Add(repairTask);

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
            Comment = "Repair requested",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, taskId = repairTask.TaskId });
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartRepair(int id, [FromBody] ApprovalActionDto dto)
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

        var task = await _db.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == ar.AssetRequestId);
        if (task != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.DamageCondition))
                task.Reason = dto.DamageCondition;
            if (dto.EstimatedCost.HasValue)
                task.EstimatedCost = dto.EstimatedCost.Value;
            // Persist new start fields
            task.RepairDate = dto.RepairDate;
            task.ExpectedCompletionDate = dto.ExpectedCompletionDate ?? dto.ExpectedCompletionTo;
            task.RepairProgressStatus = dto.RepairProgressStatus;
            task.Status = 1; // in-progress
            _db.RepairTasks.Update(task);
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
    public async Task<IActionResult> CompleteRepair(int taskId, [FromBody] Models.DTOs.RepairCompleteDto dto)
    {
        var task = await _db.RepairTasks.FindAsync(taskId);
        if (task == null) return NotFound();

        var rr = new RepairRecord
        {
            TaskId = task.TaskId,
            ActualCost = dto.ActualCost,
            RepairDate = repairDate,
            Result = resultText,
            SupplierId = dto.SupplierId,
            DamageDate = dto.DamageDate,
            DamageCondition = dto.DamageCondition
        };

        _db.RepairRecords.Add(rr);

        task.Status = 2; // completed
        _db.RepairTasks.Update(task);

        // add asset request record if linked
        if (task.AssetRequestId != 0)
        {
            var ar = await _db.AssetRequests.FindAsync(task.AssetRequestId);
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
                    Comment = "Repair completed",
                    OccurredAt = DateTime.UtcNow
                };

                _db.AssetRequestRecords.Add(rec);
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { recordId = rr.RecordId, taskId = task.TaskId });
    }
}
