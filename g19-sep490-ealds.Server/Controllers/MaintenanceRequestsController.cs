using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

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
                        t.AssetRequest.Status == 0 ? "Nháp" :
                        t.AssetRequest.Status == 1 ? "Đã nộp" :
                        t.AssetRequest.Status == 2 ? "Hợp lệ" :
                        t.AssetRequest.Status == 3 ? "Chờ phê duyệt" :
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
            Status = 0,
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
            FromStatus = 0,
            ToStatus = 0,
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
}
