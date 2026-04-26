using System.Text.Json;
using System.Text.Json.Nodes;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class RepairRequestService : IRepairRequestService
{
    private readonly EaldsDbContext _context;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _repairRequestTypeId;

    public RepairRequestService(
        EaldsDbContext context,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _context = context;
        _requestNotifications = requestNotifications;
        _repairRequestTypeId = configuration.GetValue<int>("App:RepairRequestTypeId", 4);
    }

    public async Task<IEnumerable<TransferRequestListItemDTO>> GetListAsync(int userId)
    {
        var privileged = await IsPrivilegedRepairViewerAsync(userId);
        var filterDeptId = privileged ? null : await GetEmployeeDepartmentIdAsync(userId);
        if (!privileged && !filterDeptId.HasValue)
            return Array.Empty<TransferRequestListItemDTO>();

        var query = _context.RepairTasks
            .AsNoTracking()
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(t => t.AssetRequest)
            .Where(t => t.AssetRequest != null && t.AssetRequest.RequestTypeId == _repairRequestTypeId);

        if (!privileged && filterDeptId.HasValue)
        {
            var deptId = filterDeptId.Value;
            query = query.Where(t =>
                t.AssetInstance.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == deptId));
        }

        return await query
            .OrderByDescending(t => t.AssetRequest!.CreateDate)
            .Select(t => new TransferRequestListItemDTO
            {
                RecordId = t.TaskId,
                AssetRequestId = t.AssetRequestId,
                Code = "SCC" + t.TaskId,
                TransferDate = t.AssetRequest!.CreateDate,
                AssetCode = t.AssetInstance.Asset != null ? t.AssetInstance.Asset.Code : string.Empty,
                AssetName = t.AssetInstance.Asset != null ? t.AssetInstance.Asset.Name : string.Empty,
                AssetInstanceId = t.AssetInstanceId,
                InstanceCode = t.AssetInstance.InstanceCode,
                FromDepartment = t.AssetInstance.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Department != null ? al.Department.Name : string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                ToDepartment = string.Empty,
                Quantity = 1,
                Status = t.AssetRequest.Status,
                StatusName =
                    t.AssetRequest.Status == 0 ? "Đã nộp" :
                    t.AssetRequest.Status == 1 ? "Chờ phê duyệt" :
                    t.AssetRequest.Status == 2 ? "Đã duyệt" :
                    t.AssetRequest.Status == 3 ? "Từ chối" :
                    t.AssetRequest.Status == 4 ? "Đang sửa chữa" :
                    t.AssetRequest.Status == 5 ? "Hoàn thành" :
                    "Không xác định",
                Reason = null,
                DamageCondition = t.Reason,
                RequestDescription = t.AssetRequest.Description,
                DirectorComment = t.AssetRequest.Approvals
                    .Where(a => a.ApprovedRole != null
                        && a.ApprovedRole.Code != null
                        && a.ApprovedRole.Code.ToUpper() == "DIRECTOR")
                    .OrderByDescending(a => a.DecisionDate)
                    .Select(a => a.Comment)
                    .FirstOrDefault(),
                DirectorDecisionDate = t.AssetRequest.Approvals
                    .Where(a => a.ApprovedRole != null
                        && a.ApprovedRole.Code != null
                        && a.ApprovedRole.Code.ToUpper() == "DIRECTOR")
                    .OrderByDescending(a => a.DecisionDate)
                    .Select(a => (DateTime?)a.DecisionDate)
                    .FirstOrDefault(),
                FromDepartmentId = t.AssetInstance.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.DepartmentId)
                    .FirstOrDefault(),
                ToDepartmentId = 0,
                CreatedBy = t.AssetRequest.CreatedBy,
                IsSenderConfirmed = false,
                IsReceiverConfirmed = false
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DamagedInstancePendingRepairDto>> GetDamagedPendingAsync(int userId)
    {
        var privileged = await IsPrivilegedRepairViewerAsync(userId);
        var filterDeptId = privileged ? null : await GetEmployeeDepartmentIdAsync(userId);
        if (!privileged && !filterDeptId.HasValue)
            return Array.Empty<DamagedInstancePendingRepairDto>();

        var blockingStatuses = new[] { 0, 1, 2, 4 };
        var blockingIds = await _context.RepairTasks
            .AsNoTracking()
            .Where(t => t.AssetRequest != null && t.AssetRequest.RequestTypeId == _repairRequestTypeId)
            .Where(t => blockingStatuses.Contains(t.AssetRequest!.Status))
            .Select(t => t.AssetInstanceId)
            .Distinct()
            .ToListAsync();
        var blocking = blockingIds.ToHashSet();

        var query = _context.AssetInstances
            .AsNoTracking()
            .Include(i => i.Asset)
            .Include(i => i.AssetLocations)
                .ThenInclude(al => al.Department)
            .Include(i => i.Warehouse)
            .Where(i => i.Status == (int)AssetStatus.Damaged)
            .Where(i => !blocking.Contains(i.AssetInstanceId));

        if (!privileged && filterDeptId.HasValue)
        {
            var deptId = filterDeptId.Value;
            query = query.Where(i => i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == deptId));
        }

        return await query
            .OrderByDescending(i => i.AssetInstanceId)
            .Select(i => new DamagedInstancePendingRepairDto
            {
                AssetInstanceId = i.AssetInstanceId,
                AssetId = i.AssetId,
                InstanceCode = i.InstanceCode,
                AssetCode = i.Asset != null ? i.Asset.Code : string.Empty,
                AssetName = i.Asset != null ? i.Asset.Name : string.Empty,
                DamageNote = i.Note,
                FromDepartment = i.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Department != null ? al.Department.Name : string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                FromDepartmentId = i.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.DepartmentId)
                    .FirstOrDefault(),
                Location = i.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Note)
                    .FirstOrDefault() ?? (i.Warehouse != null ? i.Warehouse.Name : string.Empty) ?? string.Empty
            })
            .ToListAsync();
    }

    public async Task<RepairRequestCreateResultDTO> CreateAsync(RepairRequestDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DamageCondition))
            throw new InvalidOperationException("Tình trạng hỏng hóc là bắt buộc.");

        if (string.IsNullOrWhiteSpace(dto.RepairKind))
            throw new InvalidOperationException("Phương án sửa chữa (repairKind) là bắt buộc.");

        if (dto.DamageDate.HasValue && dto.DamageDate.Value.Date > DateTime.UtcNow.Date)
            throw new InvalidOperationException("Ngày hỏng không được lớn hơn ngày hiện tại.");

        var instance = await _context.AssetInstances.AsNoTracking()
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == dto.AssetInstanceId);
        if (instance == null)
            throw new KeyNotFoundException($"AssetInstanceId {dto.AssetInstanceId} not found.");

        if (instance.Status != (int)AssetStatus.Damaged)
            throw new InvalidOperationException("Chỉ có thể tạo đơn sửa chữa khi tài sản đang ở trạng thái hỏng.");

        var blockingStatuses = new[] { 0, 1, 2, 4 };
        var hasBlocking = await _context.RepairTasks
            .AsNoTracking()
            .AnyAsync(t =>
                t.AssetInstanceId == dto.AssetInstanceId
                && t.AssetRequest != null
                && t.AssetRequest.RequestTypeId == _repairRequestTypeId
                && blockingStatuses.Contains(t.AssetRequest.Status));
        if (hasBlocking)
            throw new InvalidOperationException("Tài sản này đã có đơn sửa chữa đang trong luồng xử lý.");

        var title = dto.Title ?? $"Repair request for instance {dto.AssetInstanceId}";
        var initialStepId = await _context.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _repairRequestTypeId)
            .SelectMany(rt => _context.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            throw new InvalidOperationException($"No workflow step configured for RequestTypeId '{_repairRequestTypeId}'.");

        var assetRequest = new AssetRequest
        {
            UserId = dto.CreatedBy,
            RequestTypeId = _repairRequestTypeId,
            AssetId = instance.AssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = title,
            Description = dto.RepairKind!.Trim(),
            ProposedData = null,
            // Newly created repair requests start at status 1 so director can approve/reject.
            Status = 1,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value
        };

        _context.AssetRequests.Add(assetRequest);
        await _context.SaveChangesAsync();

        var repairTask = new RepairTask
        {
            AssetRequestId = assetRequest.AssetRequestId,
            AssetInstanceId = dto.AssetInstanceId,
            EstimatedCost = dto.EstimatedCost,
            Reason = dto.DamageCondition.Trim(),
            Status = 0
        };

        _context.RepairTasks.Add(repairTask);

        var userRole = await _context.UserRoles.AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = assetRequest.Status,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Repair requested",
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return new RepairRequestCreateResultDTO
        {
            AssetRequestId = assetRequest.AssetRequestId,
            TaskId = repairTask.TaskId
        };
    }

    public async Task<RepairStartResultDTO> StartRepairAsync(int assetRequestId, RepairStartDto dto)
    {
        if (dto.StartedBy <= 0)
            throw new InvalidOperationException("StartedBy is required.");

        var ar = await _context.AssetRequests.FindAsync(assetRequestId)
            ?? throw new KeyNotFoundException($"AssetRequest {assetRequestId} not found.");

        if (!await IsFinalApprovedByWorkflowAsync(ar))
            throw new InvalidOperationException("Only requests approved at final workflow step can be started.");

        var userRole = await _context.UserRoles
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

        var resolvedSupplierId = await ResolveRepairSupplierAsync(dto.NewSupplier, dto.SupplierId);

        var from = ar.Status;
        ar.Status = 4;

        if (!ar.AssetId.HasValue || ar.AssetId.Value <= 0)
            throw new InvalidOperationException("Repair request must be linked to a catalog asset.");

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == ar.AssetRequestId);
        if (task == null)
        {
            var fallbackInstance = await _context.AssetInstances
                .Where(ai => ai.AssetId == ar.AssetId.Value)
                .Select(ai => ai.AssetInstanceId)
                .FirstOrDefaultAsync();
            if (fallbackInstance == 0)
                throw new InvalidOperationException("No asset instance found for this request.");

            task = new RepairTask
            {
                AssetRequestId = ar.AssetRequestId,
                AssetInstanceId = fallbackInstance,
                EstimatedCost = dto.EstimatedCost ?? 0,
                Reason = dto.DamageCondition ?? string.Empty,
                RepairDate = dto.RepairDate,
                ExpectedCompletionDate = dto.ExpectedCompletionDate ?? dto.ExpectedCompletionTo,
                RepairProgressStatus = dto.RepairProgressStatus,
                Status = 1,
                SupplierId = resolvedSupplierId
            };
            _context.RepairTasks.Add(task);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(dto.DamageCondition))
                task.Reason = dto.DamageCondition;
            if (dto.EstimatedCost.HasValue)
                task.EstimatedCost = dto.EstimatedCost.Value;
            task.RepairDate = dto.RepairDate;
            task.ExpectedCompletionDate = dto.ExpectedCompletionDate ?? dto.ExpectedCompletionTo;
            task.RepairProgressStatus = dto.RepairProgressStatus;
            task.Status = 1;
            task.SupplierId = resolvedSupplierId;
            _context.RepairTasks.Update(task);
        }

        string? supplierNameForLog = null;
        if (resolvedSupplierId.HasValue)
        {
            supplierNameForLog = await _context.Suppliers.AsNoTracking()
                .Where(s => s.SupplierId == resolvedSupplierId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
        }

        var linkedInstanceId = task.AssetInstanceId;
        if (linkedInstanceId > 0)
        {
            var linkedInstance = await _context.AssetInstances.FindAsync(linkedInstanceId);
            if (linkedInstance != null && linkedInstance.Status != (int)AssetStatus.InRepair)
            {
                var oldStatus = linkedInstance.Status;
                linkedInstance.Status = (int)AssetStatus.InRepair;
                _context.AssetInstances.Update(linkedInstance);
                _context.AssetLifeCycles.Add(new AssetLifeCycle
                {
                    AssetInstanceId = linkedInstance.AssetInstanceId,
                    ActionType = (int)AssetLifeActionType.StatusChanged,
                    RelatedEntityType = 1,
                    RelatedEntityId = linkedInstance.AssetInstanceId,
                    ActorUserId = dto.StartedBy,
                    ActorRoleId = userRole?.RoleId ?? 0,
                    Description = $"Status changed from {(AssetStatus)oldStatus} to {(AssetStatus)AssetStatus.InRepair} (repair started)",
                    OccurredAt = DateTime.UtcNow
                });
            }
        }

        Dictionary<string, object?> startData = new()
        {
            ["flowType"] = "repair-start",
            ["reportNumber"] = dto.ReportNumber,
            ["damageDate"] = dto.DamageDate,
            ["damageCondition"] = dto.DamageCondition,
            ["attachmentDocumentIds"] = dto.AttachmentDocumentIds,
            ["attachmentUrls"] = dto.AttachmentUrls,
            ["repairDate"] = dto.RepairDate,
            ["expectedCompletionDate"] = dto.ExpectedCompletionDate,
            ["expectedCompletionFrom"] = dto.ExpectedCompletionFrom,
            ["expectedCompletionTo"] = dto.ExpectedCompletionTo,
            ["estimatedCost"] = dto.EstimatedCost,
            ["repairProgressStatus"] = dto.RepairProgressStatus,
            ["supplierId"] = resolvedSupplierId,
            ["supplierName"] = supplierNameForLog
        };
        if (!string.IsNullOrWhiteSpace(ar.ProposedData))
            startData["legacyProposedData"] = ar.ProposedData;
        ar.ProposedData = JsonSerializer.Serialize(startData);

        _context.AssetRequestRecords.Add(new AssetRequestRecord
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

        await _context.SaveChangesAsync();

        return new RepairStartResultDTO
        {
            AssetRequestId = ar.AssetRequestId,
            Status = ar.Status,
            TaskId = task.TaskId
        };
    }

    public async Task<RepairCompleteResultDTO> CompleteRepairAsync(int taskId, RepairCompleteDto dto)
    {
        if (dto.CompletedBy <= 0)
            throw new InvalidOperationException("CompletedBy is required.");

        var task = await _context.RepairTasks.FindAsync(taskId)
            ?? throw new KeyNotFoundException($"RepairTask {taskId} not found.");

        if (task.Status != 1)
            throw new InvalidOperationException("Repair can only be completed while the task is in progress.");

        var ar = await _context.AssetRequests.FindAsync(task.AssetRequestId);
        if (ar != null && ar.RequestTypeId == _repairRequestTypeId && ar.Status != 4)
            throw new InvalidOperationException("Repair can only be completed for requests in the in-progress repair state (status 4).");

        var repairDate = dto.CompletionDate ?? dto.RepairDate ?? DateTime.UtcNow;
        var resolvedSupplierId = await ResolveRepairSupplierAsync(dto.NewSupplier, dto.SupplierId);
        var supplierForRecord = resolvedSupplierId ?? task.SupplierId;

        var rr = new RepairRecord
        {
            TaskId = task.TaskId,
            ActualCost = dto.ActualCost,
            RepairDate = repairDate,
            Result = dto.Result?.Trim() ?? string.Empty,
            DetailedDescription = string.IsNullOrWhiteSpace(dto.DetailedDescription)
                ? null
                : dto.DetailedDescription.Trim(),
            ReturnToUseDate = dto.ReturnToUseDate,
            SupplierId = supplierForRecord,
            DamageDate = dto.DamageDate,
            DamageCondition = dto.DamageCondition,
            RepairWarrantyStartDate = dto.RepairWarrantyStartDate,
            RepairWarrantyEndDate = dto.RepairWarrantyEndDate,
            RepairWarrantyPeriodValue = dto.RepairWarrantyPeriodValue is > 0 ? dto.RepairWarrantyPeriodValue : null,
            RepairWarrantyPeriodUnit = string.IsNullOrWhiteSpace(dto.RepairWarrantyPeriodUnit)
                ? null
                : dto.RepairWarrantyPeriodUnit.Trim(),
            RepairWarrantyConditions = string.IsNullOrWhiteSpace(dto.RepairWarrantyConditions)
                ? null
                : dto.RepairWarrantyConditions.Trim(),
            RepairWarrantyNote = string.IsNullOrWhiteSpace(dto.RepairWarrantyNote)
                ? null
                : dto.RepairWarrantyNote.Trim()
        };

        _context.RepairRecords.Add(rr);

        task.Status = 2;
        _context.RepairTasks.Update(task);

        var completedByRoleId = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == dto.CompletedBy)
            .Join(
                _context.Roles.AsNoTracking(),
                ur => ur.RoleId,
                r => r.RoleId,
                (ur, r) => (int?)r.RoleId)
            .FirstOrDefaultAsync();

        if (ar != null)
        {
            var requestFromStatus = ar.Status;
            ar.Status = 5;

            var completionNode = new JsonObject
            {
                ["flowType"] = "repair-complete",
                ["reportNumber"] = dto.ReportNumber,
                ["completionDate"] = JsonSerializer.SerializeToNode(repairDate),
                ["returnToUseDate"] = dto.ReturnToUseDate.HasValue
                    ? JsonSerializer.SerializeToNode(dto.ReturnToUseDate.Value)
                    : null,
                ["actualCost"] = JsonSerializer.SerializeToNode(dto.ActualCost),
                ["attachmentDocumentIds"] = dto.AttachmentDocumentIds != null
                    ? JsonSerializer.SerializeToNode(dto.AttachmentDocumentIds)
                    : null,
                ["attachmentUrls"] = dto.AttachmentUrls != null
                    ? JsonSerializer.SerializeToNode(dto.AttachmentUrls)
                    : null,
                ["completedAt"] = JsonSerializer.SerializeToNode(DateTime.UtcNow),
                ["supplierId"] = supplierForRecord.HasValue
                    ? JsonSerializer.SerializeToNode(supplierForRecord.Value)
                    : null,
                ["repairWarrantyStartDate"] = dto.RepairWarrantyStartDate.HasValue
                    ? JsonSerializer.SerializeToNode(dto.RepairWarrantyStartDate.Value)
                    : null,
                ["repairWarrantyEndDate"] = dto.RepairWarrantyEndDate.HasValue
                    ? JsonSerializer.SerializeToNode(dto.RepairWarrantyEndDate.Value)
                    : null,
                ["repairWarrantyPeriodValue"] = dto.RepairWarrantyPeriodValue is > 0
                    ? JsonSerializer.SerializeToNode(dto.RepairWarrantyPeriodValue.Value)
                    : null,
                ["repairWarrantyPeriodUnit"] = string.IsNullOrWhiteSpace(dto.RepairWarrantyPeriodUnit)
                    ? null
                    : dto.RepairWarrantyPeriodUnit.Trim(),
                ["repairWarrantyConditions"] = string.IsNullOrWhiteSpace(dto.RepairWarrantyConditions)
                    ? null
                    : dto.RepairWarrantyConditions.Trim(),
                ["repairWarrantyNote"] = string.IsNullOrWhiteSpace(dto.RepairWarrantyNote)
                    ? null
                    : dto.RepairWarrantyNote.Trim()
            };

            JsonObject root;
            if (string.IsNullOrWhiteSpace(ar.ProposedData))
                root = new JsonObject();
            else
            {
                try
                {
                    var parsed = JsonNode.Parse(ar.ProposedData);
                    root = parsed as JsonObject ?? new JsonObject { ["legacy"] = parsed };
                }
                catch
                {
                    root = new JsonObject { ["legacyProposedDataRaw"] = ar.ProposedData };
                }
            }

            root["repairCompletion"] = completionNode;
            ar.ProposedData = root.ToJsonString();

            if (dto.ReturnToUseDate.HasValue && task.AssetInstanceId > 0)
            {
                var linkedInstance = await _context.AssetInstances.FindAsync(task.AssetInstanceId);
                if (linkedInstance != null && dto.ReturnToUseDate.Value.Date <= DateTime.UtcNow.Date)
                {
                    var oldStatus = linkedInstance.Status;
                    linkedInstance.Status = (int)AssetStatus.InUse;
                    linkedInstance.InUseDate = DateOnly.FromDateTime(dto.ReturnToUseDate.Value.Date);
                    _context.AssetInstances.Update(linkedInstance);
                    _context.AssetLifeCycles.Add(new AssetLifeCycle
                    {
                        AssetInstanceId = linkedInstance.AssetInstanceId,
                        ActionType = (int)AssetLifeActionType.StatusChanged,
                        RelatedEntityType = 1,
                        RelatedEntityId = linkedInstance.AssetInstanceId,
                        ActorUserId = dto.CompletedBy,
                        ActorRoleId = completedByRoleId ?? 0,
                        Description = $"Status changed from {(AssetStatus)oldStatus} to {(AssetStatus)AssetStatus.InUse} (repair completed)",
                        OccurredAt = DateTime.UtcNow
                    });
                }
            }

            if (completedByRoleId.HasValue)
            {
                _context.AssetRequestRecords.Add(new AssetRequestRecord
                {
                    AssetRequestId = ar.AssetRequestId,
                    FromStatus = requestFromStatus,
                    ToStatus = ar.Status,
                    Action = 3,
                    ActionByUserId = dto.CompletedBy,
                    ActionRoleId = completedByRoleId.Value,
                    Comment = "Repair completed",
                    OccurredAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return new RepairCompleteResultDTO
        {
            RecordId = rr.RepairId,
            TaskId = task.TaskId
        };
    }

    private async Task<bool> IsFinalApprovedByWorkflowAsync(AssetRequest ar)
    {
        if (ar.Status == 2 || ar.Status == 4)
            return true;

        var workflowId = await _context.RequestTypes.AsNoTracking()
            .Where(rt => rt.RequestTypeId == ar.RequestTypeId)
            .Select(rt => (int?)rt.WorkflowId)
            .FirstOrDefaultAsync();

        if (!workflowId.HasValue || workflowId.Value == 0)
            return false;

        var finalStepId = await _context.WorkflowSteps.AsNoTracking()
            .Where(ws => ws.WorkflowId == workflowId.Value)
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .LastOrDefaultAsync();

        if (!finalStepId.HasValue)
            return false;

        return await _context.Approvals.AsNoTracking().AnyAsync(a =>
            a.AssetRequestId == ar.AssetRequestId
            && a.StepId == finalStepId.Value
            && a.Decision == 1);
    }

    private async Task<int?> ResolveRepairSupplierAsync(RepairSupplierCreateDto? newSupplier, int? supplierId)
    {
        if (newSupplier != null
            && !string.IsNullOrWhiteSpace(newSupplier.Code)
            && !string.IsNullOrWhiteSpace(newSupplier.Name))
        {
            var code = newSupplier.Code.Trim();
            if (await _context.Suppliers.AnyAsync(s => s.Code == code))
                throw new InvalidOperationException("Mã đơn vị sửa chữa đã tồn tại.");
            var sup = new Supplier
            {
                Code = code,
                Name = newSupplier.Name.Trim(),
                Status = 1,
                CreateDate = DateTime.UtcNow
            };
            _context.Suppliers.Add(sup);
            await _context.SaveChangesAsync();
            return sup.SupplierId;
        }

        if (supplierId is > 0)
        {
            if (!await _context.Suppliers.AnyAsync(s => s.SupplierId == supplierId.Value))
                throw new InvalidOperationException("Đơn vị sửa chữa không hợp lệ.");
            return supplierId;
        }

        return null;
    }

    private async Task<bool> IsPrivilegedRepairViewerAsync(int userId)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles.AsNoTracking(), ur => ur.RoleId, r => r.RoleId, (ur, r) => r)
            .AnyAsync(r =>
                r.Code != null &&
                (r.Code.ToUpper() == "DIRECTOR" || r.Code.ToUpper() == "ACCOUNTANT"));
    }

    private async Task<int?> GetEmployeeDepartmentIdAsync(int userId)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();
    }
}
