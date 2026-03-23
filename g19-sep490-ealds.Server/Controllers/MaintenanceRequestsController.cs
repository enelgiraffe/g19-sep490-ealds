using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
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
            // Vừa tạo là "Đã nộp" để tránh hiển thị "Chưa gửi"
            Status = 1,
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

        // Allow delete only when request is Draft(0) or Submitted(1)
        if (ar.Status > 1)
            return BadRequest("Chỉ được xóa đề xuất bảo dưỡng khi đang ở trạng thái Nháp hoặc Đã nộp.");

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
    public async Task<IActionResult> StartMaintenance(int id, [FromBody] MaintenanceStartDto dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");
        if (dto.StartedBy <= 0)
            return BadRequest("StartedBy is required.");

        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar == null) return NotFound();

        var isFinalApproved = await IsFinalApprovedByWorkflowAsync(ar);
        if (!isFinalApproved)
            return BadRequest("Only requests approved at final workflow step can be started.");

        var userRole = await _db.UserRoles.Include(ur => ur.Role).AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.StartedBy);
        var role = userRole?.Role;
        var allowed = role != null && (
            (role.Code != null && (role.Code.Equals("DepartmentManager", StringComparison.OrdinalIgnoreCase) || role.Code.Equals("Accountant", StringComparison.OrdinalIgnoreCase)))
            || (role.Name != null && (role.Name.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0 || role.Name.IndexOf("Director", StringComparison.OrdinalIgnoreCase) >= 0 || role.Name.IndexOf("Accountant", StringComparison.OrdinalIgnoreCase) >= 0))
        );

        if (!allowed) return Forbid();

        var from = ar.Status;
        ar.Status = 4;

        // mark related maintenance task as in-progress and persist start fields
        var task = await _db.MaintenaceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == ar.AssetRequestId);
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
            task.EstimatedCost = dto.EstimatedCost;
            task.ExpectedCompletionDate = dto.ExpectedCompletionDate ?? dto.ExpectedCompletionTo;
            task.MaintenanceContent = dto.MaintenanceContent;
            task.LocationType = dto.LocationType;
            task.Status = 1; // in-progress
            _db.MaintenaceTasks.Update(task);
        }

        if (!string.IsNullOrWhiteSpace(dto.DetailedDescription))
            ar.Description = dto.DetailedDescription;

        // Set asset status to InMaintenance only now (when maintenance actually starts)
        var linkedAssetId = task?.AssetId ?? 0;
        if (linkedAssetId > 0)
        {
            var linkedAsset = await _db.Assets.FindAsync(linkedAssetId);
            if (linkedAsset != null && linkedAsset.Status != (int)AssetStatus.InMaintenance)
            {
                var oldStatus = linkedAsset.Status;
                linkedAsset.Status = (int)AssetStatus.InMaintenance;
                _db.Assets.Update(linkedAsset);
                _db.AssetLifeCycles.Add(new AssetLifeCycle
                {
                    AssetId = linkedAsset.AssetId,
                    ActionType = (int)AssetLifeActionType.StatusChanged,
                    RelatedEntityType = 1,
                    RelatedEntityId = linkedAsset.AssetId,
                    ActorUserId = dto.StartedBy,
                    ActorRoleId = userRole?.RoleId ?? 0,
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
            ActionByUserId = dto.StartedBy,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status, taskId = task?.TaskId });
    }

    private async Task<bool> IsFinalApprovedByWorkflowAsync(AssetRequest ar)
    {
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

        var task = await _db.MaintenaceTasks.FindAsync(taskId);
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

        var noteParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(dto.ReportNumber))
            noteParts.Add($"ReportNumber: {dto.ReportNumber}");
        if (dto.ReturnToUseDate.HasValue)
            noteParts.Add($"ReturnToUseDate: {dto.ReturnToUseDate.Value:O}");
        if (!string.IsNullOrWhiteSpace(dto.TechnicalNote))
            noteParts.Add(dto.TechnicalNote);
        var technicalNote = noteParts.Count > 0 ? string.Join("\n", noteParts) : null;

        var mr = new MaintenanceRecord
        {
            TaskId = task.TaskId,
            ExecutionDate = executionDate,
            TotalCost = totalCost,
            WorkPerformed = workPerformed,
            ConditionBefore = conditionBefore,
            ConditionAfter = conditionAfter,
            TechnicalNote = technicalNote,
            Status = 1
        };

        _db.MaintenanceRecords.Add(mr);

        task.Status = 2; // completed
        _db.MaintenaceTasks.Update(task);

        if (linkedRequest != null)
        {
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
                FromStatus = linkedRequest.Status,
                ToStatus = linkedRequest.Status,
                Action = 3,
                ActionByUserId = dto.CompletedBy,
                ActionRoleId = 0,
                Comment = "Maintenance completed",
                OccurredAt = DateTime.UtcNow
            };

            _db.AssetRequestRecords.Add(rec);
        }

        await _db.SaveChangesAsync();

        return Ok(new { recordId = mr.RecordId, taskId = task.TaskId });
    }
}
