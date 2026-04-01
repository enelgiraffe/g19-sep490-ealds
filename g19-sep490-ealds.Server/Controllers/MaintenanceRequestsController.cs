using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/maintenance")]
public class MaintenanceRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _maintenanceRequestTypeId;

    public MaintenanceRequestsController(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _maintenanceRequestTypeId = configuration.GetValue<int>("App:MaintenanceRequestTypeId", 2);
    }

        /// <summary>
        /// GET /api/Assets/Requests/maintenance/list - Danh sách yêu cầu bảo dưỡng
        /// Trả về dạng TransferRequestListItemDTO (mã SBB..., asset, phòng ban, trạng thái, lý do).
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<TransferRequestListItemDTO>>> GetList()
        {
            var query = _db.MaintenanceTasks
                .AsNoTracking()
                .Include(t => t.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
                .Include(t => t.AssetInstance)
                    .ThenInclude(ai => ai.AssetLocations)
                        .ThenInclude(al => al.Department)
                .Include(t => t.AssetRequest)
                .Where(t => t.AssetRequest != null && t.AssetRequest.RequestTypeId == _maintenanceRequestTypeId)
                .OrderByDescending(t => t.PlannedDate);

            var list = await query
                .Select(t => new TransferRequestListItemDTO
                {
                    RecordId = t.TaskId,
                    AssetRequestId = t.AssetRequestId ?? 0,
                    Code = "SBD" + t.TaskId,
                    TransferDate = t.PlannedDate,
                    AssetCode = t.AssetInstance.InstanceCode,
                    AssetName = t.AssetInstance.Asset.Name,
                    AssetTypeName = t.AssetInstance.Asset.AssetType != null
                        ? t.AssetInstance.Asset.AssetType.Name
                        : null,
                    AssetInstanceId = t.AssetInstanceId,
                    InstanceCode = t.AssetInstance.InstanceCode,
                    FromDepartment = t.AssetInstance.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Department.Name)
                        .FirstOrDefault() ?? string.Empty,
                    ToDepartment = t.AssetInstance.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Department.Name)
                        .FirstOrDefault() ?? string.Empty,
                    Quantity = 1,
                    Status = t.AssetRequest!.Status,
                    StatusName =
                        t.AssetRequest.Status == -1 ? "Nháp" :
                        t.AssetRequest.Status == 0 ? "Đã gửi" :
                        t.AssetRequest.Status == 1 ? "Chờ phê duyệt" :
                        t.AssetRequest.Status == 2 ? "Phê duyệt" :
                        t.AssetRequest.Status == 3 ? "Từ chối" :
                        t.AssetRequest.Status == 4 ? "Đang thực hiện" :
                        "Không xác định",
                    Reason = t.AssetRequest.Description,
                    FromDepartmentId = t.AssetInstance.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.DepartmentId)
                        .FirstOrDefault(),
                    ToDepartmentId = t.AssetInstance.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.DepartmentId)
                        .FirstOrDefault(),
                    CreatedBy = t.AssetRequest.CreatedBy,
                    IsSenderConfirmed = false,
                    IsReceiverConfirmed = false
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> RequestExecution([FromBody] MaintenanceRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        var assetInstance = await _db.AssetInstances
            .Include(ai => ai.Asset)
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == dto.AssetInstanceId);
        if (assetInstance == null)
            return NotFound($"AssetInstanceId {dto.AssetInstanceId} not found.");

        var schedule = dto.ScheduleId.HasValue && dto.ScheduleId.Value > 0
            ? await _db.MaintenanceSchedules.FindAsync(dto.ScheduleId)
            : null;

        var title = dto.Title ?? $"Maintenance request for instance {dto.AssetInstanceId}";
        var initialStepId = await _db.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _maintenanceRequestTypeId)
            .SelectMany(rt => _db.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            return BadRequest($"No workflow step configured for RequestTypeId '{_maintenanceRequestTypeId}'.");

        var assetRequest = new AssetRequest
        {
            UserId = dto.CreatedBy,
            RequestTypeId = _maintenanceRequestTypeId,
            AssetId = assetInstance.AssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = title,
            Description = dto.Description,
            ProposedData = null,
            // Vừa tạo là "Đã nộp" để tránh hiển thị "Chưa gửi"
            Status = 1,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var planned = dto.PlannedDate;
        if (!planned.HasValue && schedule != null)
            planned = schedule.NextDueDate ?? schedule.StartDate;

        var assignTo = dto.AssignTo > 0 ? dto.AssignTo : dto.CreatedBy;

        var task = new MaintenanceTask
        {
            ScheduleId = dto.ScheduleId,
            AssetRequestId = assetRequest.AssetRequestId,
            AssetInstanceId = dto.AssetInstanceId,
            PlannedDate = planned ?? DateTime.UtcNow,
            AssignTo = assignTo,
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
            FromStatus = assetRequest.Status,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Maintenance request created",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);

        // NOTE: Asset status is NOT changed here – only changed when maintenance actually starts (StartMaintenance).

        if (schedule != null && schedule.IntervalValue.HasValue && schedule.IntervalUnit.HasValue)
        {
            var baseDate = schedule.NextDueDate ?? schedule.StartDate;
            var v = schedule.IntervalValue.Value;
            var u = (MaintenanceRepeatIntervalUnit)schedule.IntervalUnit.Value;
            schedule.NextDueDate = u switch
            {
                MaintenanceRepeatIntervalUnit.Day => baseDate.AddDays(v),
                MaintenanceRepeatIntervalUnit.Week => baseDate.AddDays(7 * v),
                MaintenanceRepeatIntervalUnit.Month => baseDate.AddMonths(v),
                MaintenanceRepeatIntervalUnit.Year => baseDate.AddYears(v),
                _ => baseDate.AddMonths(v)
            };
            _db.MaintenanceSchedules.Update(schedule);
        }

        await _db.SaveChangesAsync();

        await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, taskId = task.TaskId });
    }

    /// <summary>
    /// DELETE /api/Assets/Requests/maintenance/{assetRequestId} - Xóa đề xuất bảo dưỡng (chỉ khi chưa duyệt).
    /// </summary>
    [HttpDelete("{assetRequestId:int}")]
    public async Task<IActionResult> DeleteMaintenanceRequest(int assetRequestId)
    {
        var task = await _db.MaintenanceTasks
            .Include(t => t.AssetRequest)
            .FirstOrDefaultAsync(t => t.AssetRequestId == assetRequestId);

        // If there is no task, still allow deleting the AssetRequest if it exists
        var ar = task?.AssetRequest ?? await _db.AssetRequests.FirstOrDefaultAsync(r => r.AssetRequestId == assetRequestId);
        if (ar == null)
            return NotFound(new { message = $"Maintenance request {assetRequestId} not found." });

        // Allow delete only when request is Draft(0) or Submitted(1)
        if (ar.Status > 1)
            return BadRequest("Chỉ được xóa đề xuất bảo dưỡng khi đang ở trạng thái Nháp hoặc Đã nộp.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var approvals = await _db.Approvals.Where(x => x.AssetRequestId == assetRequestId).ToListAsync();
            var records = await _db.AssetRequestRecords.Where(x => x.AssetRequestId == assetRequestId).ToListAsync();
            var maintenanceTasks = await _db.MaintenanceTasks.Where(x => x.AssetRequestId == assetRequestId).ToListAsync();

            if (approvals.Count > 0) _db.Approvals.RemoveRange(approvals);
            if (records.Count > 0) _db.AssetRequestRecords.RemoveRange(records);
            if (maintenanceTasks.Count > 0) _db.MaintenanceTasks.RemoveRange(maintenanceTasks);

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
    public async Task<IActionResult> StartMaintenance(int id, [FromBody] MaintenanceStartDto dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");
        if (dto.StartedBy <= 0)
            return BadRequest("StartedBy is required.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorUserId = int.TryParse(userIdClaim, out var parsedUserId) && parsedUserId > 0
            ? parsedUserId
            : dto.StartedBy;

        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar == null) return NotFound();

        var isFinalApproved = await IsFinalApprovedByWorkflowAsync(ar);
        if (!isFinalApproved)
            return BadRequest("Only requests approved at final workflow step can be started.");

        var task = await _db.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == ar.AssetRequestId);

        var hasAllowedRoleFromToken =
            User.IsInRole("DIRECTOR") ||
            User.IsInRole("DepartmentManager") ||
            User.IsInRole("DEPARTMENT_MANAGER") ||
            User.IsInRole("DEPT_MANAGER") ||
            User.IsInRole("DEPARTMENT_HEAD") ||
            User.IsInRole("HEAD_OF_DEPARTMENT") ||
            User.IsInRole("TRUONG_PHONG") ||
            User.IsInRole("TRUONGPHONG") ||
            User.IsInRole("ACCOUNTANT");

        var userRoles = await _db.UserRoles
            .Include(ur => ur.Role)
            .AsNoTracking()
            .Where(ur => ur.UserId == actorUserId)
            .ToListAsync();

        var allowedRoleIds = await _db.Roles
            .AsNoTracking()
            .Where(r =>
                (r.Code != null &&
                    (
                        r.Code.ToUpper() == "DIRECTOR" ||
                        r.Code.ToUpper() == "DEPARTMENTMANAGER" ||
                        r.Code.ToUpper() == "DEPARTMENT_MANAGER" ||
                        r.Code.ToUpper() == "DEPT_MANAGER" ||
                        r.Code.ToUpper() == "DEPARTMENT_HEAD" ||
                        r.Code.ToUpper() == "HEAD_OF_DEPARTMENT" ||
                        r.Code.ToUpper() == "TRUONG_PHONG" ||
                        r.Code.ToUpper() == "TRUONGPHONG" ||
                        r.Code.ToUpper() == "ACCOUNTANT"
                    )) ||
                (r.Name != null &&
                    (
                        r.Name.ToUpper().Contains("MANAGER") ||
                        r.Name.ToUpper().Contains("DIRECTOR") ||
                        r.Name.ToUpper().Contains("ACCOUNTANT") ||
                        r.Name.ToUpper().Contains("TRUONG PHONG")
                    ))
            )
            .Select(r => r.RoleId)
            .ToListAsync();

        var allowedByRole = hasAllowedRoleFromToken || userRoles.Any(ur =>
            allowedRoleIds.Contains(ur.RoleId) ||
            (ur.Role?.Code != null &&
                (
                    ur.Role.Code.ToUpper() == "DIRECTOR" ||
                    ur.Role.Code.ToUpper() == "DEPARTMENTMANAGER" ||
                    ur.Role.Code.ToUpper() == "DEPARTMENT_MANAGER" ||
                    ur.Role.Code.ToUpper() == "DEPT_MANAGER" ||
                    ur.Role.Code.ToUpper() == "DEPARTMENT_HEAD" ||
                    ur.Role.Code.ToUpper() == "HEAD_OF_DEPARTMENT" ||
                    ur.Role.Code.ToUpper() == "TRUONG_PHONG" ||
                    ur.Role.Code.ToUpper() == "TRUONGPHONG" ||
                    ur.Role.Code.ToUpper() == "ACCOUNTANT"
                )) ||
            (ur.Role?.Name != null &&
                (
                    ur.Role.Name.ToUpper().Contains("MANAGER") ||
                    ur.Role.Name.ToUpper().Contains("DIRECTOR") ||
                    ur.Role.Name.ToUpper().Contains("ACCOUNTANT") ||
                    ur.Role.Name.ToUpper().Contains("TRUONG PHONG")
                ))
        );
        var allowedByOwnership =
            ar.CreatedBy == actorUserId ||
            task?.CreateBy == actorUserId ||
            task?.AssignTo == actorUserId ||
            task?.PerformerUserId == actorUserId;
        var allowed = allowedByRole || allowedByOwnership;

        if (!allowed)
        {
            return StatusCode(403, new
            {
                message = "Bạn không có quyền bắt đầu bảo dưỡng.",
                actorUserId,
                currentUserRoleIds = userRoles.Select(x => x.RoleId).Distinct().ToArray(),
                currentUserRoleCodes = userRoles.Select(x => x.Role?.Code).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray(),
                currentUserRoleNames = userRoles.Select(x => x.Role?.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray()
            });
        }

        var from = ar.Status;
        ar.Status = 4;

        // mark related maintenance task as in-progress and persist start fields
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
            task.Status = 1; // in-progress
            _db.MaintenanceTasks.Update(task);
        }

        if (!string.IsNullOrWhiteSpace(dto.DetailedDescription))
            ar.Description = dto.DetailedDescription;

        var linkedInstanceId = task?.AssetInstanceId ?? 0;
        if (linkedInstanceId > 0)
        {
            var linkedInstance = await _db.AssetInstances.FindAsync(linkedInstanceId);
            if (linkedInstance != null && linkedInstance.Status != (int)AssetStatus.InMaintenance)
            {
                var oldStatus = linkedInstance.Status;
                linkedInstance.Status = (int)AssetStatus.InMaintenance;
                _db.AssetInstances.Update(linkedInstance);
                _db.AssetLifeCycles.Add(new AssetLifeCycle
                {
                    AssetInstanceId = linkedInstance.AssetInstanceId,
                    ActionType = (int)AssetLifeActionType.StatusChanged,
                    RelatedEntityType = 1,
                    RelatedEntityId = linkedInstance.AssetInstanceId,
                    ActorUserId = actorUserId,
                    ActorRoleId = userRoles.FirstOrDefault()?.RoleId ?? 0,
                    Description = $"Status changed from {(AssetStatus)oldStatus} to {(AssetStatus)AssetStatus.InMaintenance} (maintenance started)",
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
            ActionByUserId = actorUserId,
            ActionRoleId = userRoles.FirstOrDefault()?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status, taskId = task?.TaskId });
    }

    private async Task<bool> IsFinalApprovedByWorkflowAsync(AssetRequest ar)
    {
        // Backward-compatible: many existing flows mark "approved" directly on AssetRequest.Status
        // without persisting final-step approval rows.
        if (ar.Status == 2 || ar.Status == 4)
            return true;

        var workflowId = await _db.RequestTypes.AsNoTracking()
            .Where(rt => rt.RequestTypeId == ar.RequestTypeId)
            .Select(rt => (int?)rt.WorkflowId)
            .FirstOrDefaultAsync();

        if (!workflowId.HasValue || workflowId.Value == 0)
            return ar.Status == 2 || ar.Status == 4;

        var finalStepId = await _db.WorkflowSteps.AsNoTracking()
            .Where(ws => ws.WorkflowId == workflowId.Value)
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .LastOrDefaultAsync();

        if (!finalStepId.HasValue)
            return ar.Status == 2 || ar.Status == 4;

        return await _db.Approvals.AsNoTracking().AnyAsync(a =>
            a.AssetRequestId == ar.AssetRequestId
            && a.StepId == finalStepId.Value
            && a.Decision == 1);
    }

    [HttpPost("tasks/{taskId}/complete")]
    public async Task<IActionResult> CompleteMaintenance(int taskId, [FromBody] Models.DTOs.MaintenanceCompleteDto dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");
        if (dto.CompletedBy <= 0)
            return BadRequest("CompletedBy is required.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorUserId = int.TryParse(userIdClaim, out var parsedUserId) && parsedUserId > 0
            ? parsedUserId
            : dto.CompletedBy;

        var actionRoleId = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == actorUserId)
            .Select(ur => (int?)ur.RoleId)
            .FirstOrDefaultAsync();

        if (!actionRoleId.HasValue)
        {
            actionRoleId = await _db.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleId)
                .Select(r => (int?)r.RoleId)
                .FirstOrDefaultAsync();
        }

        if (!actionRoleId.HasValue)
            return BadRequest("Không tìm thấy Role hợp lệ để ghi nhận lịch sử hoàn thành bảo dưỡng.");

        var task = await _db.MaintenanceTasks.FindAsync(taskId);
        if (task == null) return NotFound();

        // Chỉ cho phép hoàn thành khi task đã được bắt đầu (Start) — Status = 1 đang thực hiện.
        if (task.Status != 1)
            return BadRequest("Maintenance can only be completed while the task is in progress.");

        AssetRequest? linkedRequest = null;
        if (task.AssetRequestId.HasValue)
        {
            linkedRequest = await _db.AssetRequests.FindAsync(task.AssetRequestId.Value);
            if (linkedRequest != null
                && linkedRequest.RequestTypeId == _maintenanceRequestTypeId
                && linkedRequest.Status != 4)
                return BadRequest("Maintenance can only be completed for requests in the in-progress maintenance state (status 4).");
        }

        var executionDate = dto.CompletionDate ?? dto.ExecutionDate ?? DateTime.UtcNow;
        var totalCost = dto.ActualCost ?? dto.TotalCost;
        var workPerformed = dto.MaintenanceContent ?? dto.WorkPerformed ?? string.Empty;
        var conditionBefore = dto.ConditionBefore ?? string.Empty;
        var conditionAfter = dto.DetailedDescription ?? dto.ConditionAfter ?? string.Empty;

        var mr = new MaintenanceRecord
        {
            TaskId = task.TaskId,
            AssetInstanceId = task.AssetInstanceId,
            ExecutionDate = executionDate,
            TotalCost = totalCost,
            WorkPerformed = workPerformed,
            ConditionBefore = conditionBefore,
            ConditionAfter = conditionAfter,
            Status = 1
        };

        _db.MaintenanceRecords.Add(mr);

        task.Status = 2; // completed
        _db.MaintenanceTasks.Update(task);

        if (linkedRequest != null)
        {
            var fromRequestStatus = linkedRequest.Status;
            // Close maintenance workflow request after task completion.
            linkedRequest.Status = 2;

            var completionNode = new JsonObject
            {
                ["flowType"] = "maintenance-complete",
                ["reportNumber"] = dto.ReportNumber,
                ["completionDate"] = JsonSerializer.SerializeToNode(executionDate),
                ["returnToUseDate"] = dto.ReturnToUseDate.HasValue
                    ? JsonSerializer.SerializeToNode(dto.ReturnToUseDate.Value)
                    : null,
                ["actualCost"] = totalCost,
                ["attachmentDocumentIds"] = dto.AttachmentDocumentIds != null
                    ? JsonSerializer.SerializeToNode(dto.AttachmentDocumentIds)
                    : null,
                ["attachmentUrls"] = dto.AttachmentUrls != null
                    ? JsonSerializer.SerializeToNode(dto.AttachmentUrls)
                    : null,
                ["completedAt"] = JsonSerializer.SerializeToNode(DateTime.UtcNow)
            };

            JsonObject root;
            if (string.IsNullOrWhiteSpace(linkedRequest.ProposedData))
                root = new JsonObject();
            else
            {
                try
                {
                    var parsed = JsonNode.Parse(linkedRequest.ProposedData);
                    root = parsed as JsonObject ?? new JsonObject { ["legacy"] = parsed };
                }
                catch
                {
                    root = new JsonObject { ["legacyProposedDataRaw"] = linkedRequest.ProposedData };
                }
            }

            root["maintenanceCompletion"] = completionNode;
            linkedRequest.ProposedData = root.ToJsonString();

            var rec = new AssetRequestRecord
            {
                AssetRequestId = linkedRequest.AssetRequestId,
                FromStatus = fromRequestStatus,
                ToStatus = linkedRequest.Status,
                Action = 3,
                ActionByUserId = actorUserId,
                ActionRoleId = actionRoleId.Value,
                Comment = "Maintenance completed",
                OccurredAt = DateTime.UtcNow
            };

            _db.AssetRequestRecords.Add(rec);
        }

        var linkedInstanceId = task.AssetInstanceId;
        if (linkedInstanceId > 0)
        {
            var linkedInstance = await _db.AssetInstances.FindAsync(linkedInstanceId);
            if (linkedInstance != null && linkedInstance.Status == (int)AssetStatus.InMaintenance)
            {
                var oldStatus = linkedInstance.Status;
                linkedInstance.Status = (int)AssetStatus.InUse;
                _db.AssetInstances.Update(linkedInstance);
                _db.AssetLifeCycles.Add(new AssetLifeCycle
                {
                    AssetInstanceId = linkedInstance.AssetInstanceId,
                    ActionType = (int)AssetLifeActionType.StatusChanged,
                    RelatedEntityType = 1,
                    RelatedEntityId = linkedInstance.AssetInstanceId,
                    ActorUserId = actorUserId,
                    ActorRoleId = actionRoleId.Value,
                    Description = $"Status changed from {(AssetStatus)oldStatus} to {(AssetStatus)AssetStatus.InUse} (maintenance completed)",
                    OccurredAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { recordId = mr.RecordId, taskId = task.TaskId });
    }
}
