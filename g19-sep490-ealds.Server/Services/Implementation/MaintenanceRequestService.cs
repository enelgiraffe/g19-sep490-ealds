using System.Text.Json;
using System.Text.Json.Nodes;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class MaintenanceRequestService : IMaintenanceRequestService
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _maintenanceRequestTypeId;

    public MaintenanceRequestService(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _maintenanceRequestTypeId = configuration.GetValue<int>("App:MaintenanceRequestTypeId", 2);
    }

    public async Task<IEnumerable<TransferRequestListItemDTO>> GetListAsync(int userId)
    {
        var (allowAll, viewerDeptId) = await ResolveMaintenanceListVisibilityAsync(userId);
        var query = _db.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(t => t.AssetRequest)
            .Where(t => t.AssetRequest != null && t.AssetRequest.RequestTypeId == _maintenanceRequestTypeId);

        if (!allowAll)
        {
            if (!viewerDeptId.HasValue || viewerDeptId.Value <= 0)
                return Array.Empty<TransferRequestListItemDTO>();
            query = query.Where(t => t.AssetInstance.AssetLocations.Any(al =>
                al.IsCurrent && al.DepartmentId == viewerDeptId.Value));
        }

        return await query
            .OrderByDescending(t => t.PlannedDate)
            .Select(t => new TransferRequestListItemDTO
            {
                RecordId = t.TaskId,
                AssetRequestId = t.AssetRequestId ?? 0,
                Code = "SBD" + t.TaskId,
                TransferDate = t.PlannedDate,
                AssetCode = t.AssetInstance.InstanceCode,
                AssetName = t.AssetInstance.Asset != null ? t.AssetInstance.Asset.Name : string.Empty,
                AssetTypeName = t.AssetInstance.Asset != null && t.AssetInstance.Asset.AssetType != null
                    ? t.AssetInstance.Asset.AssetType.Name
                    : null,
                AssetInstanceId = t.AssetInstanceId,
                InstanceCode = t.AssetInstance.InstanceCode,
                FromDepartment = t.AssetInstance.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Department != null ? al.Department.Name : string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                ToDepartment = t.AssetInstance.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Department != null ? al.Department.Name : string.Empty)
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
                    t.AssetRequest.Status == 5 ? "Đã bảo dưỡng" :
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
    }

    public async Task<MaintenanceRequestCreateResultDTO> CreateAsync(MaintenanceRequestDTO dto)
    {
        var assetInstance = await _db.AssetInstances
            .Include(ai => ai.Asset)
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == dto.AssetInstanceId);
        if (assetInstance == null)
            throw new KeyNotFoundException($"AssetInstanceId {dto.AssetInstanceId} not found.");

        var schedule = dto.ScheduleId.HasValue && dto.ScheduleId.Value > 0
            ? await _db.MaintenanceSchedules.FindAsync(dto.ScheduleId)
            : null;

        var title = string.IsNullOrWhiteSpace(dto.Title)
            ? $"Yêu cầu bảo dưỡng tài sản #{dto.AssetInstanceId}"
            : dto.Title.Trim();
        var initialStepId = await _db.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _maintenanceRequestTypeId)
            .SelectMany(rt => _db.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            throw new InvalidOperationException($"No workflow step configured for RequestTypeId '{_maintenanceRequestTypeId}'.");

        var assetRequest = new AssetRequest
        {
            UserId = dto.CreatedBy,
            RequestTypeId = _maintenanceRequestTypeId,
            AssetId = assetInstance.AssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = title,
            Description = dto.Description,
            ProposedData = null,
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

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        _db.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = assetRequest.Status,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Tạo yêu cầu bảo dưỡng",
            OccurredAt = DateTime.UtcNow
        });

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

        return new MaintenanceRequestCreateResultDTO
        {
            AssetRequestId = assetRequest.AssetRequestId,
            TaskId = task.TaskId
        };
    }

    public async Task DeleteMaintenanceRequestAsync(int assetRequestId)
    {
        var task = await _db.MaintenanceTasks
            .Include(t => t.AssetRequest)
            .FirstOrDefaultAsync(t => t.AssetRequestId == assetRequestId);

        var ar = task?.AssetRequest ?? await _db.AssetRequests.FirstOrDefaultAsync(r => r.AssetRequestId == assetRequestId);
        if (ar == null)
            throw new KeyNotFoundException($"Maintenance request {assetRequestId} not found.");

        if (ar.Status > 1)
            throw new InvalidOperationException("Chỉ được xóa đề xuất bảo dưỡng khi đang ở trạng thái Nháp hoặc Đã nộp.");

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
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<MaintenanceStartResultDTO> StartMaintenanceAsync(int assetRequestId, MaintenanceStartDto dto)
    {
        if (dto.StartedBy <= 0)
            throw new InvalidOperationException("StartedBy is required.");

        var ar = await _db.AssetRequests.FindAsync(assetRequestId)
            ?? throw new KeyNotFoundException($"AssetRequest {assetRequestId} not found.");

        if (!await IsFinalApprovedByWorkflowAsync(ar))
            throw new InvalidOperationException("Only requests approved at final workflow step can be started.");

        var userRole = await _db.UserRoles
            .Include(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.UserId == dto.StartedBy);
        var role = userRole?.Role;
        var normalizedCode = role?.Code?.Trim().ToUpperInvariant();
        var codeAllowed =
            normalizedCode == "DEPARTMENTMANAGER"
            || normalizedCode == "DEPARTMENT_MANAGER"
            || normalizedCode == "DEPT_MANAGER"
            || normalizedCode == "DEPARTMENT_HEAD"
            || normalizedCode == "HEAD_OF_DEPARTMENT"
            || normalizedCode == "TRUONG_BAN"
            || normalizedCode == "TRUONGBAN"
            || normalizedCode == "TRUONG_PHONG"
            || normalizedCode == "TRUONGPHONG"
            || normalizedCode == "ACCOUNTANT"
            || normalizedCode == "DIRECTOR";

        var roleName = role?.Name ?? string.Empty;
        var nameAllowed =
            roleName.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0
            || roleName.IndexOf("Director", StringComparison.OrdinalIgnoreCase) >= 0
            || roleName.IndexOf("Accountant", StringComparison.OrdinalIgnoreCase) >= 0
            || roleName.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0
            || roleName.IndexOf("Trưởng ban", StringComparison.OrdinalIgnoreCase) >= 0
            || roleName.IndexOf("Truong ban", StringComparison.OrdinalIgnoreCase) >= 0
            || roleName.IndexOf("Trưởng phòng", StringComparison.OrdinalIgnoreCase) >= 0
            || roleName.IndexOf("Truong phong", StringComparison.OrdinalIgnoreCase) >= 0;

        if (role == null || (!codeAllowed && !nameAllowed))
            throw new UnauthorizedAccessException();

        var task = await _db.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == ar.AssetRequestId);

        var from = ar.Status;
        ar.Status = 4;

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

        _db.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = from,
            ToStatus = ar.Status,
            Action = 2,
            ActionByUserId = dto.StartedBy,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return new MaintenanceStartResultDTO
        {
            AssetRequestId = ar.AssetRequestId,
            Status = ar.Status,
            TaskId = task?.TaskId
        };
    }

    public async Task<MaintenanceCompleteResultDTO> CompleteMaintenanceAsync(int taskId, MaintenanceCompleteDto dto)
    {
        if (dto.CompletedBy <= 0)
            throw new InvalidOperationException("CompletedBy is required.");

        var task = await _db.MaintenanceTasks.FindAsync(taskId)
            ?? throw new KeyNotFoundException($"MaintenanceTask {taskId} not found.");

        if (task.Status != 1)
            throw new InvalidOperationException("Maintenance can only be completed while the task is in progress.");

        AssetRequest? linkedRequest = null;
        if (task.AssetRequestId.HasValue)
        {
            linkedRequest = await _db.AssetRequests.FindAsync(task.AssetRequestId.Value);
            if (linkedRequest != null
                && linkedRequest.RequestTypeId == _maintenanceRequestTypeId
                && linkedRequest.Status != 4)
                throw new InvalidOperationException("Maintenance can only be completed for requests in the in-progress maintenance state (status 4).");
        }

        var completedByRoleId = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == dto.CompletedBy)
            .Join(
                _db.Roles.AsNoTracking(),
                ur => ur.RoleId,
                r => r.RoleId,
                (ur, r) => (int?)r.RoleId)
            .FirstOrDefaultAsync();

        var executionDate = dto.CompletionDate ?? dto.ExecutionDate ?? DateTime.UtcNow;
        var totalCost = dto.ActualCost ?? dto.TotalCost;
        var workPerformed = dto.MaintenanceContent ?? dto.WorkPerformed ?? string.Empty;
        // Tình trạng / mô tả trước bảo dưỡng: ưu tiên mô tả trên yêu cầu (AssetRequest.Description),
        // sau đó payload từ client, cuối cùng tình trạng ghi trên cá thể (AssetInstance.Condition).
        var fromRequestDescription =
            linkedRequest != null && !string.IsNullOrWhiteSpace(linkedRequest.Description)
                ? linkedRequest.Description.Trim()
                : null;
        var fromDto = string.IsNullOrWhiteSpace(dto.ConditionBefore) ? null : dto.ConditionBefore.Trim();
        string? fromInstanceCondition = null;
        if (fromRequestDescription == null && fromDto == null && task.AssetInstanceId > 0)
        {
            var instRow = await _db.AssetInstances.AsNoTracking()
                .FirstOrDefaultAsync(i => i.AssetInstanceId == task.AssetInstanceId);
            if (!string.IsNullOrWhiteSpace(instRow?.Condition))
                fromInstanceCondition = instRow!.Condition!.Trim();
        }

        var conditionBefore = fromRequestDescription ?? fromDto ?? fromInstanceCondition ?? string.Empty;
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

        task.Status = 2;
        _db.MaintenanceTasks.Update(task);

        if (linkedRequest != null)
        {
            var fromRequestStatus = linkedRequest.Status;
            linkedRequest.Status = 5;

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

            if (completedByRoleId.HasValue)
            {
                _db.AssetRequestRecords.Add(new AssetRequestRecord
                {
                    AssetRequestId = linkedRequest.AssetRequestId,
                    FromStatus = fromRequestStatus,
                    ToStatus = linkedRequest.Status,
                    Action = 3,
                    ActionByUserId = dto.CompletedBy,
                    ActionRoleId = completedByRoleId.Value,
                    Comment = "Maintenance completed",
                    OccurredAt = DateTime.UtcNow
                });
            }
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
                    ActorUserId = dto.CompletedBy,
                    ActorRoleId = completedByRoleId ?? 0,
                    Description = $"Status changed from {(AssetStatus)oldStatus} to {(AssetStatus)AssetStatus.InUse} (maintenance completed)",
                    OccurredAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();

        return new MaintenanceCompleteResultDTO
        {
            RecordId = mr.RecordId,
            TaskId = task.TaskId
        };
    }

    private async Task<bool> IsFinalApprovedByWorkflowAsync(AssetRequest ar)
    {
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

    private async Task<(bool AllowAll, int? DepartmentId)> ResolveMaintenanceListVisibilityAsync(int userId)
    {
        if (userId <= 0)
            return (false, null);

        var roleIds = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (roleIds.Any(r => r is 1 or 2 or 3))
            return (true, null);

        var privilegedByCode = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.RoleId, (ur, r) => r)
            .AnyAsync(r =>
                r.Code != null &&
                (r.Code.ToUpper() == "ADMIN"
                 || r.Code.ToUpper() == "DIRECTOR"
                 || r.Code.ToUpper() == "ACCOUNTANT"));

        if (privilegedByCode)
            return (true, null);

        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee?.DepartmentId is int deptId && deptId > 0)
            return (false, deptId);

        return (false, null);
    }
}
