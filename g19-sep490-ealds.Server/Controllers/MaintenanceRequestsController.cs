using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/maintenance")]
public class MaintenanceRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _maintenanceRequestTypeId;

    public MaintenanceRequestsController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _maintenanceRequestTypeId = configuration.GetValue<int>("App:MaintenanceRequestTypeId", 2);
    }

        /// <summary>
        /// GET /api/Assets/Requests/maintenance/list - Danh sách yêu cầu bảo dưỡng
        /// Trả về dạng TransferRequestListItemDTO (mã SBB..., asset, phòng ban, trạng thái, lý do).
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<TransferRequestListItemDTO>>> GetList()
        {
            var query = _db.MaintenaceTasks
                .AsNoTracking()
                .Include(t => t.Asset)
                    .ThenInclude(a => a.AssetLocations)
                        .ThenInclude(al => al.Department)
                .Include(t => t.AssetRequest)
                .Where(t => t.AssetRequest != null && t.AssetRequest.RequestTypeId == _maintenanceRequestTypeId)
                .OrderByDescending(t => t.PlannedDate);

            var list = await query
                .Select(t => new TransferRequestListItemDTO
                {
                    RecordId = t.TaskId,
                    AssetRequestId = t.AssetRequestId ?? 0,
                    Code = "SBB" + t.TaskId,
                    TransferDate = t.PlannedDate,
                    AssetCode = t.Asset.Code,
                    AssetName = t.Asset.Name,
                    // Với bảo dưỡng, phòng ban hiện tại quản lý tài sản được dùng cho cả From/To.
                    FromDepartment = t.Asset.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Department.Name)
                        .FirstOrDefault() ?? string.Empty,
                    ToDepartment = t.Asset.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Department.Name)
                        .FirstOrDefault() ?? string.Empty,
                    Quantity = t.Asset.Quantity,
                    Status = t.AssetRequest!.Status,
                    StatusName =
                        t.AssetRequest.Status == -1 ? "Nháp" :
                        t.AssetRequest.Status == 0 ? "Đã gửi" :
                        t.AssetRequest.Status == 1 ? "Chờ phê duyệt" :
                        t.AssetRequest.Status == 2 ? "Phê duyệt" :
                        t.AssetRequest.Status == 3 ? "Từ chối" :
                        t.AssetRequest.Status == 4 ? "Phê duyệt" :
                        "Không xác định",
                    Reason = t.AssetRequest.Description
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> RequestExecution([FromBody] MaintenanceRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.AssetId == dto.AssetId);
        if (asset == null)
            return NotFound($"AssetId {dto.AssetId} not found.");

        var schedule = dto.ScheduleId.HasValue && dto.ScheduleId.Value > 0
            ? await _db.MaintenanceSchedules.FindAsync(dto.ScheduleId)
            : null;

        var title = dto.Title ?? $"Maintenance request for asset {dto.AssetId}";

        var assetRequest = new AssetRequest
        {
            UserId = dto.CreatedBy,
            RequestTypeId = _maintenanceRequestTypeId,
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

        var assignTo = dto.AssignTo > 0 ? dto.AssignTo : dto.CreatedBy;

        var task = new MaintenaceTask
        {
            ScheduleId = dto.ScheduleId,
            AssetRequestId = assetRequest.AssetRequestId,
            AssetId = dto.AssetId,
            PlannedDate = planned ?? DateTime.UtcNow,
            AssignTo = assignTo,
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
            Comment = "Maintenance request created",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);

        // Update asset status to InMaintenance when maintenance is reported/created
        // (Previously: only created request + task, asset status stayed unchanged)
        var oldAssetStatus = asset.Status;
        if (oldAssetStatus != (int)AssetStatus.InMaintenance)
        {
            asset.Status = (int)AssetStatus.InMaintenance;
            _db.Assets.Update(asset);

            _db.AssetLifeCycles.Add(new AssetLifeCycle
            {
                AssetId = asset.AssetId,
                ActionType = (int)AssetLifeActionType.StatusChanged,
                RelatedEntityType = 1, // 1 = Asset
                RelatedEntityId = asset.AssetId,
                ActorUserId = dto.CreatedBy,
                ActorRoleId = actionRoleId,
                Description =
                    $"Status changed from {(AssetStatus)oldAssetStatus} " +
                    $"to {(AssetStatus)AssetStatus.InMaintenance}",
                OccurredAt = DateTime.UtcNow
            });
        }

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

    /// <summary>
    /// DELETE /api/Assets/Requests/maintenance/{assetRequestId} - Xóa đề xuất bảo dưỡng (chỉ khi chưa duyệt).
    /// </summary>
    [HttpDelete("{assetRequestId:int}")]
    public async Task<IActionResult> DeleteMaintenanceRequest(int assetRequestId)
    {
        var task = await _db.MaintenaceTasks
            .Include(t => t.AssetRequest)
            .FirstOrDefaultAsync(t => t.AssetRequestId == assetRequestId);

        // If there is no task, still allow deleting the AssetRequest if it exists
        var ar = task?.AssetRequest ?? await _db.AssetRequests.FirstOrDefaultAsync(r => r.AssetRequestId == assetRequestId);
        if (ar == null)
            return NotFound(new { message = $"Maintenance request {assetRequestId} not found." });

        // Only allow deleting when request is still in Draft state.
        if (ar.Status != (int)AssetRequestStatus.Draft)
            return BadRequest("Chỉ được xóa đề xuất bảo dưỡng khi đang ở trạng thái Nháp.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var approvals = await _db.Approvals.Where(x => x.AssetRequestId == assetRequestId).ToListAsync();
            var records = await _db.AssetRequestRecords.Where(x => x.AssetRequestId == assetRequestId).ToListAsync();
            var maintenanceTasks = await _db.MaintenaceTasks.Where(x => x.AssetRequestId == assetRequestId).ToListAsync();

            if (approvals.Count > 0) _db.Approvals.RemoveRange(approvals);
            if (records.Count > 0) _db.AssetRequestRecords.RemoveRange(records);
            if (maintenanceTasks.Count > 0) _db.MaintenaceTasks.RemoveRange(maintenanceTasks);

            _db.AssetRequests.Remove(ar);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
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
