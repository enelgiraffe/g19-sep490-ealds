using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class TransferRequestService : ITransferRequestService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<TransferRequestService> _logger;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _transferRequestTypeId;

    public TransferRequestService(
        EaldsDbContext context,
        ILogger<TransferRequestService> logger,
        IAssetRequestNotificationService requestNotifications,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _requestNotifications = requestNotifications;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
    }

    public async Task<IEnumerable<TransferRequestListItemDTO>> GetListAsync(int userId, bool isAccountant)
    {
        var userDeptId = await _context.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        var list = await _context.TransferRecords
            .AsNoTracking()
            .Include(tr => tr.AssetInstance).ThenInclude(ai => ai.Asset)
            .Include(tr => tr.AssetRequest)
            .Include(tr => tr.FromLocation).ThenInclude(fl => fl.Department)
            .Include(tr => tr.ToLocation).ThenInclude(tl => tl.Department)
            .Where(tr =>
                isAccountant
                || tr.AssetRequest.CreatedBy == userId
                || (userDeptId.HasValue && tr.FromLocation.DepartmentId == userDeptId.Value)
                || (userDeptId.HasValue && tr.ToLocation.DepartmentId == userDeptId.Value))
            .OrderByDescending(tr => tr.TransferDate)
            .Select(tr => new TransferRequestListItemDTO
            {
                RecordId = tr.TransferId,
                AssetRequestId = tr.AssetRequestId,
                Code = "SBB" + tr.TransferId,
                TransferDate = tr.TransferDate,
                AssetCode = tr.AssetInstance.Asset.Code,
                AssetName = tr.AssetInstance.Asset.Name,
                AssetInstanceId = tr.AssetInstanceId,
                InstanceCode = tr.AssetInstance.InstanceCode,
                FromDepartment = tr.FromLocation.Department.Name,
                ToDepartment = tr.ToLocation.Department.Name,
                FromDepartmentId = tr.FromLocation.DepartmentId,
                ToDepartmentId = tr.ToLocation.DepartmentId,
                CreatedBy = tr.AssetRequest.CreatedBy,
                CreatedByName = _context.Employees
                    .Where(e => e.UserId == tr.AssetRequest.CreatedBy)
                    .Select(e => e.Name)
                    .FirstOrDefault(),
                Quantity = 1,
                Status = tr.AssetRequest.Status,
                StatusName =
                    tr.AssetRequest.Status == 0 ? "Nháp" :
                    tr.AssetRequest.Status == 1 ? "Đã nộp" :
                    tr.AssetRequest.Status == 2 ? "Chờ phê duyệt" :
                    tr.AssetRequest.Status == 3 ? "Từ chối" :
                    tr.AssetRequest.Status == 4 ? "Phê duyệt" :
                    "Không xác định",
                Reason = tr.AssetRequest.Description,
                IsSenderConfirmed = tr.IsSenderConfirmed,
                IsReceiverConfirmed = tr.IsReceiverConfirmed,
                IsIncompleteProposedDraft = false,
                AccountantComment = _context.Approvals.AsNoTracking()
                    .Join(_context.Roles.AsNoTracking(), a => a.ApprovedRoleId, r => r.RoleId, (a, r) => new { a, r })
                    .Where(x => x.a.AssetRequestId == tr.AssetRequestId
                        && x.r.Code != null
                        && x.r.Code.ToUpper() == "ACCOUNTANT")
                    .OrderByDescending(x => x.a.DecisionDate)
                    .Select(x => x.a.Comment)
                    .FirstOrDefault(),
                DirectorComment = _context.Approvals.AsNoTracking()
                    .Join(_context.Roles.AsNoTracking(), a => a.ApprovedRoleId, r => r.RoleId, (a, r) => new { a, r })
                    .Where(x => x.a.AssetRequestId == tr.AssetRequestId
                        && x.r.Code != null
                        && x.r.Code.Trim().ToUpper() == "DIRECTOR")
                    .OrderByDescending(x => x.a.DecisionDate)
                    .Select(x => x.a.Comment)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var proposedDrafts = await _context.AssetRequests
            .AsNoTracking()
            .Where(ar => ar.RequestTypeId == _transferRequestTypeId
                && ar.Status == 0
                && (isAccountant || ar.CreatedBy == userId)
                && !_context.TransferRecords.Any(t => t.AssetRequestId == ar.AssetRequestId))
            .OrderByDescending(ar => ar.CreateDate)
            .Select(ar => new TransferRequestListItemDTO
            {
                RecordId = 0,
                AssetRequestId = ar.AssetRequestId,
                Code = "BN" + ar.AssetRequestId,
                TransferDate = ar.CreateDate,
                AssetCode = "—",
                AssetName = "Bản nháp chưa hoàn tất",
                AssetInstanceId = null,
                InstanceCode = null,
                FromDepartment = "—",
                ToDepartment = "—",
                FromDepartmentId = 0,
                ToDepartmentId = 0,
                CreatedBy = ar.CreatedBy,
                CreatedByName = _context.Employees
                    .Where(e => e.UserId == ar.CreatedBy)
                    .Select(e => e.Name)
                    .FirstOrDefault(),
                Quantity = 0,
                Status = 0,
                StatusName = "Nháp",
                Reason = ar.Description,
                IsSenderConfirmed = false,
                IsReceiverConfirmed = false,
                IsIncompleteProposedDraft = true,
                DraftFormJson = ar.ProposedData,
                AccountantComment = null,
                DirectorComment = null
            })
            .ToListAsync();

        return list.Concat(proposedDrafts).OrderByDescending(x => x.TransferDate).ToList();
    }

    public async Task<IEnumerable<TransferHandoverRecordItemDto>> GetHandoverRecordsAsync(int userId, bool isAccountant, int assetRequestId)
    {
        var userDeptId = await _context.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        var transfer = await _context.TransferRecords
            .AsNoTracking()
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.AssetRequest)
            .FirstOrDefaultAsync(t => t.AssetRequestId == assetRequestId);

        if (transfer == null)
            throw new KeyNotFoundException("Transfer request not found");

        var ar = transfer.AssetRequest;
        if (ar == null || ar.RequestTypeId != _transferRequestTypeId)
            throw new KeyNotFoundException("Transfer request not found");

        var canView = isAccountant
            || ar.CreatedBy == userId
            || (userDeptId.HasValue && transfer.FromLocation.DepartmentId == userDeptId.Value)
            || (userDeptId.HasValue && transfer.ToLocation.DepartmentId == userDeptId.Value);
        if (!canView)
            throw new UnauthorizedAccessException();

        var handovers = await _context.TransferHandoverRecords
            .AsNoTracking()
            .Where(h => h.TransferId == transfer.TransferId)
            .OrderBy(h => h.OccurredAt)
            .ToListAsync();

        var result = new List<TransferHandoverRecordItemDto>(handovers.Count);
        foreach (var h in handovers)
        {
            TransferHandoverDetailsDto details;
            try
            {
                details = JsonSerializer.Deserialize<TransferHandoverDetailsDto>(h.DetailsJson) ?? new TransferHandoverDetailsDto();
            }
            catch
            {
                details = new TransferHandoverDetailsDto();
            }

            var actorName = await _context.Employees.AsNoTracking()
                .Where(e => e.UserId == h.ActionByUserId)
                .Select(e => e.Name)
                .FirstOrDefaultAsync();

            result.Add(new TransferHandoverRecordItemDto
            {
                TransferHandoverRecordId = h.TransferHandoverRecordId,
                Side = h.Side,
                ActionByUserId = h.ActionByUserId,
                ActionByUserName = actorName,
                OccurredAt = h.OccurredAt,
                Details = details,
                UserNote = h.UserNote
            });
        }

        return result;
    }

    public async Task<CreateTransferResultDTO> CreateAsync(int userId, TransferRequestDTO dto)
    {
        if (dto.IncompleteDraft)
        {
            if (!dto.SaveAsDraft)
                throw new InvalidOperationException("Bản nháp chưa hoàn tất phải được lưu ở trạng thái nháp.");
            return await CreateProposedDataOnlyDraftAsync(dto, userId);
        }

        if (dto.AssetInstanceId <= 0)
            throw new InvalidOperationException("AssetInstanceId is required.");

        var instance = await _context.AssetInstances
            .Include(ai => ai.Asset)
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == dto.AssetInstanceId);
        if (instance == null)
            throw new KeyNotFoundException($"AssetInstanceId {dto.AssetInstanceId} not found.");

        if (instance.Status != (int)AssetStatus.InUse)
            throw new InvalidOperationException("Chỉ có thể điều chuyển cá thể đang ở trạng thái đang sử dụng.");

        if (dto.FromLocationId == dto.ToLocationId)
            throw new InvalidOperationException("Vị trí nguồn và vị trí đích không được trùng nhau.");

        var fromDeptExists = await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == dto.FromLocationId);
        var toDeptExists = await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == dto.ToLocationId);
        if (!fromDeptExists)
            throw new InvalidOperationException("Phòng ban nguồn (FromLocationId) không tồn tại trong hệ thống.");
        if (!toDeptExists)
            throw new InvalidOperationException("Phòng ban đích (ToLocationId) không tồn tại trong hệ thống.");

        if (await DepartmentAssetScope.AnyDepartmentsHaveInventoryInProgressAsync(
                _context, new[] { dto.FromLocationId, dto.ToLocationId }))
            throw new InvalidOperationException(DepartmentAssetScope.InventoryInProgressBlockingMessage);

        var now = dto.TransferDate ?? DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var fromLocation = await _context.AssetLocations
            .FirstOrDefaultAsync(al =>
                al.AssetInstanceId == dto.AssetInstanceId &&
                al.DepartmentId == dto.FromLocationId &&
                al.IsCurrent);

        if (fromLocation == null)
            throw new InvalidOperationException("Không tìm thấy vị trí hiện tại của tài sản tại phòng ban nguồn. Vui lòng kiểm tra lại 'Từ vị trí'.");

        // Keep old location current until approved; new location starts as pending (IsCurrent = false).
        var toLocation = new AssetLocation
        {
            AssetInstanceId = dto.AssetInstanceId,
            DepartmentId = dto.ToLocationId,
            StartDate = today,
            EndDate = null,
            IsCurrent = false,
            Note = dto.Description
        };
        _context.AssetLocations.Add(toLocation);

        var title = dto.Title ?? $"Transfer instance {dto.AssetInstanceId} from dept {dto.FromLocationId} to dept {dto.ToLocationId}";
        var initialStepId = await _context.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _transferRequestTypeId)
            .SelectMany(rt => _context.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            throw new InvalidOperationException($"No workflow step configured for RequestTypeId '{_transferRequestTypeId}'.");

        var saveAsDraft = dto.SaveAsDraft;
        var requestStatus = saveAsDraft ? 0 : 1;

        var assetRequest = new AssetRequest
        {
            UserId = userId,
            RequestTypeId = _transferRequestTypeId,
            AssetId = instance.AssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = title,
            Description = dto.Description,
            ProposedData = null,
            Status = requestStatus,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value
        };

        _context.AssetRequests.Add(assetRequest);
        await _context.SaveChangesAsync();

        var transfer = new TransferRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            AssetInstanceId = dto.AssetInstanceId,
            FromLocationId = fromLocation.LocationId,
            ToLocationId = toLocation.LocationId,
            FromUserId = dto.FromUserId ?? userId,
            ToUserId = dto.ToUserId,
            TransferDate = now,
            ExecutedBy = dto.ExecuteBy == 0 ? userId : dto.ExecuteBy,
            IsSenderConfirmed = false,
            IsReceiverConfirmed = false
        };

        _context.TransferRecords.Add(transfer);

        var userRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        var actionRoleId = userRole?.RoleId ?? 1;

        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = requestStatus,
            Action = 0,
            ActionByUserId = userId,
            ActionRoleId = actionRoleId,
            Comment = saveAsDraft ? "Lưu nháp điều chuyển" : "Transfer requested",
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        if (dto.ReplaceIncompleteAssetRequestId is int rId && rId > 0)
            await TryDeleteReplacedIncompleteTransferDraftAsync(rId, userId);

        if (!saveAsDraft)
            await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return new CreateTransferResultDTO
        {
            AssetRequestId = assetRequest.AssetRequestId,
            RecordId = transfer.TransferId,
            IncompleteDraft = false
        };
    }

    public async Task<int> UpdateDraftAsync(int userId, int assetRequestId, UpdateTransferDraftBody body)
    {
        var json = string.IsNullOrWhiteSpace(body.DraftFormJson) ? "{}" : body.DraftFormJson.Trim();
        if (json.Length > 1_000_000)
            throw new InvalidOperationException("Bản nháp quá lớn.");

        var ar = await _context.AssetRequests
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId && a.RequestTypeId == _transferRequestTypeId);
        if (ar == null)
            throw new KeyNotFoundException($"Transfer request {assetRequestId} not found.");
        if (ar.CreatedBy != userId)
            throw new UnauthorizedAccessException();
        if (ar.Status != 0)
            throw new InvalidOperationException("Chỉ được sửa bản nháp (Nháp) chưa gửi.");
        if (await _context.TransferRecords.AnyAsync(t => t.AssetRequestId == assetRequestId))
            throw new InvalidOperationException("Yêu cầu đã có bản ghi điều chuyển, không cập nhật theo bản nháp JSON.");

        ar.ProposedData = json;
        await _context.SaveChangesAsync();
        return ar.AssetRequestId;
    }

    public async Task DeleteAsync(int userId, int assetRequestId)
    {
        var ar = await _context.AssetRequests
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId && a.RequestTypeId == _transferRequestTypeId);
        if (ar == null)
            throw new KeyNotFoundException($"Transfer request {assetRequestId} not found.");
        if (ar.CreatedBy != userId)
            throw new UnauthorizedAccessException();
        if (ar.Status != 0)
            throw new InvalidOperationException("Chỉ được xóa yêu cầu ở trạng thái Nháp.");

        var transfer = await _context.TransferRecords
            .Include(tr => tr.FromLocation)
            .Include(tr => tr.ToLocation)
            .FirstOrDefaultAsync(tr => tr.AssetRequestId == assetRequestId);

        if (transfer == null)
        {
            await using var txDraft = await _context.Database.BeginTransactionAsync();
            try
            {
                var draftRecords = await _context.AssetRequestRecords
                    .Where(r => r.AssetRequestId == assetRequestId)
                    .ToListAsync();
                if (draftRecords.Count > 0) _context.AssetRequestRecords.RemoveRange(draftRecords);
                _context.AssetRequests.Remove(ar);
                await _context.SaveChangesAsync();
                await txDraft.CommitAsync();
                return;
            }
            catch
            {
                await txDraft.RollbackAsync();
                throw;
            }
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var toLocation = await _context.AssetLocations.FirstOrDefaultAsync(al => al.LocationId == transfer.ToLocationId);
            if (toLocation != null)
                _context.AssetLocations.Remove(toLocation);

            var records = await _context.AssetRequestRecords
                .Where(r => r.AssetRequestId == assetRequestId)
                .ToListAsync();
            if (records.Count > 0) _context.AssetRequestRecords.RemoveRange(records);

            _context.TransferRecords.Remove(transfer);

            var arReload = await _context.AssetRequests.FirstOrDefaultAsync(r => r.AssetRequestId == assetRequestId);
            if (arReload != null) _context.AssetRequests.Remove(arReload);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ConfirmSendAsync(int userId, bool isAccountant, int assetRequestId, TransferHandoverConfirmBody? body)
    {
        var ar = await _context.AssetRequests.FindAsync(assetRequestId);
        if (ar == null || ar.RequestTypeId != _transferRequestTypeId)
            throw new KeyNotFoundException("Transfer request not found");
        if (ar.Status != 4)
            throw new InvalidOperationException("Chỉ được xác nhận gửi khi yêu cầu đã được Giám đốc phê duyệt (Status = 4).");

        var transfer = await _context.TransferRecords
            .Include(t => t.FromLocation).ThenInclude(fl => fl.Department)
            .Include(t => t.ToLocation).ThenInclude(tl => tl.Department)
            .Include(t => t.AssetInstance).ThenInclude(ai => ai.Asset)
            .FirstOrDefaultAsync(t => t.AssetRequestId == assetRequestId);
        if (transfer == null)
            throw new KeyNotFoundException("Transfer record not found");

        if (transfer.IsSenderConfirmed)
            throw new InvalidOperationException("Bên gửi đã xác nhận rồi.");

        var userDeptId = await _context.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        var canSend = isAccountant
            || ar.CreatedBy == userId
            || transfer.FromUserId == userId
            || (userDeptId.HasValue && transfer.FromLocation.DepartmentId == userDeptId.Value);
        if (!canSend)
            throw new UnauthorizedAccessException();

        if (await DepartmentAssetScope.DepartmentHasInventoryInProgressAsync(_context, transfer.FromLocation.DepartmentId))
            throw new InvalidOperationException(DepartmentAssetScope.InventoryInProgressBlockingMessage);

        var note = string.IsNullOrWhiteSpace(body?.Note) ? null : body!.Note!.Trim();
        if (note != null && note.Length > 2000)
            throw new InvalidOperationException("Ghi chú không quá 2000 ký tự.");

        var occurred = DateTime.UtcNow;
        _context.TransferHandoverRecords.Add(new TransferHandoverRecord
        {
            TransferId = transfer.TransferId,
            Side = "Sender",
            ActionByUserId = userId,
            OccurredAt = occurred,
            DetailsJson = BuildHandoverDetailsJson(transfer, "Sender"),
            UserNote = note
        });

        transfer.IsSenderConfirmed = true;
        transfer.SenderConfirmedAt = occurred;

        var senderRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequestId,
            FromStatus = 4, ToStatus = 4,
            Action = 1,
            ActionByUserId = userId,
            ActionRoleId = senderRole?.RoleId ?? 1,
            Comment = "Bên gửi đã xác nhận chuyển",
            OccurredAt = DateTime.UtcNow
        });

        await TryExecutePhysicalTransfer(transfer);
        await _context.SaveChangesAsync();
        return transfer.IsSenderConfirmed && transfer.IsReceiverConfirmed;
    }

    public async Task<bool> ConfirmReceiveAsync(int userId, bool isAccountant, int assetRequestId, TransferHandoverConfirmBody? body)
    {
        var ar = await _context.AssetRequests.FindAsync(assetRequestId);
        if (ar == null || ar.RequestTypeId != _transferRequestTypeId)
            throw new KeyNotFoundException("Transfer request not found");
        if (ar.Status != 4)
            throw new InvalidOperationException("Chỉ được xác nhận nhận khi yêu cầu đã được Giám đốc phê duyệt (Status = 4).");

        var transfer = await _context.TransferRecords
            .Include(t => t.FromLocation).ThenInclude(fl => fl.Department)
            .Include(t => t.ToLocation).ThenInclude(tl => tl.Department)
            .Include(t => t.AssetInstance).ThenInclude(ai => ai.Asset)
            .FirstOrDefaultAsync(t => t.AssetRequestId == assetRequestId);
        if (transfer == null)
            throw new KeyNotFoundException("Transfer record not found");

        if (transfer.IsReceiverConfirmed)
            throw new InvalidOperationException("Bên nhận đã xác nhận rồi.");
        if (!transfer.IsSenderConfirmed)
            throw new InvalidOperationException("Bên gửi chưa xác nhận bàn giao. Bên nhận chưa thể xác nhận đã nhận.");

        var userDeptId = await _context.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        var canReceive = isAccountant
            || transfer.ToUserId == userId
            || (userDeptId.HasValue && transfer.ToLocation.DepartmentId == userDeptId.Value);
        if (!canReceive)
            throw new UnauthorizedAccessException();

        if (await DepartmentAssetScope.DepartmentHasInventoryInProgressAsync(_context, transfer.ToLocation.DepartmentId))
            throw new InvalidOperationException(DepartmentAssetScope.InventoryInProgressBlockingMessage);

        var note = string.IsNullOrWhiteSpace(body?.Note) ? null : body!.Note!.Trim();
        if (note != null && note.Length > 2000)
            throw new InvalidOperationException("Ghi chú không quá 2000 ký tự.");

        var occurred = DateTime.UtcNow;
        _context.TransferHandoverRecords.Add(new TransferHandoverRecord
        {
            TransferId = transfer.TransferId,
            Side = "Receiver",
            ActionByUserId = userId,
            OccurredAt = occurred,
            DetailsJson = BuildHandoverDetailsJson(transfer, "Receiver"),
            UserNote = note
        });

        transfer.IsReceiverConfirmed = true;
        transfer.ReceiverConfirmedAt = occurred;

        var receiverRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequestId,
            FromStatus = 4, ToStatus = 4,
            Action = 1,
            ActionByUserId = userId,
            ActionRoleId = receiverRole?.RoleId ?? 1,
            Comment = "Bên nhận đã xác nhận nhận",
            OccurredAt = DateTime.UtcNow
        });

        await TryExecutePhysicalTransfer(transfer);
        await _context.SaveChangesAsync();
        return transfer.IsSenderConfirmed && transfer.IsReceiverConfirmed;
    }

    private async Task<CreateTransferResultDTO> CreateProposedDataOnlyDraftAsync(TransferRequestDTO dto, int userId)
    {
        var json = string.IsNullOrWhiteSpace(dto.DraftFormJson) ? "{}" : dto.DraftFormJson.Trim();
        if (json.Length > 1_000_000)
            throw new InvalidOperationException("Bản nháp quá lớn.");

        var initialStepId = await _context.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _transferRequestTypeId)
            .SelectMany(rt => _context.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            throw new InvalidOperationException($"No workflow step configured for RequestTypeId '{_transferRequestTypeId}'.");

        var assetRequest = new AssetRequest
        {
            UserId = userId,
            RequestTypeId = _transferRequestTypeId,
            AssetId = null,
            AssetInstanceId = null,
            Title = "Bản nháp điều chuyển",
            Description = null,
            ProposedData = json,
            Status = 0,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value
        };

        _context.AssetRequests.Add(assetRequest);
        await _context.SaveChangesAsync();

        var userRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = 0,
            Action = 0,
            ActionByUserId = userId,
            ActionRoleId = userRole?.RoleId ?? 1,
            Comment = "Lưu nháp (chưa hoàn tất thông tin)",
            OccurredAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new CreateTransferResultDTO
        {
            AssetRequestId = assetRequest.AssetRequestId,
            RecordId = 0,
            IncompleteDraft = true
        };
    }

    private async Task TryDeleteReplacedIncompleteTransferDraftAsync(int assetRequestId, int userId)
    {
        var ar = await _context.AssetRequests
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId && a.RequestTypeId == _transferRequestTypeId);
        if (ar == null) return;
        if (ar.CreatedBy != userId) return;
        if (ar.Status != 0) return;
        if (await _context.TransferRecords.AnyAsync(t => t.AssetRequestId == assetRequestId)) return;
        var records = await _context.AssetRequestRecords
            .Where(r => r.AssetRequestId == assetRequestId)
            .ToListAsync();
        if (records.Count > 0) _context.AssetRequestRecords.RemoveRange(records);
        _context.AssetRequests.Remove(ar);
        await _context.SaveChangesAsync();
    }

    private async Task TryExecutePhysicalTransfer(TransferRecord transfer)
    {
        if (!transfer.IsSenderConfirmed || !transfer.IsReceiverConfirmed) return;

        var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var fromLocation = await _context.AssetLocations.FirstOrDefaultAsync(al => al.LocationId == transfer.FromLocationId);
        if (fromLocation != null && fromLocation.IsCurrent)
        {
            fromLocation.IsCurrent = false;
            fromLocation.EndDate = todayDate;
        }

        var otherLocations = await _context.AssetLocations
            .Where(al => al.AssetInstanceId == transfer.AssetInstanceId && al.IsCurrent && al.LocationId != transfer.FromLocationId)
            .ToListAsync();
        foreach (var loc in otherLocations)
        {
            loc.IsCurrent = false;
            loc.EndDate = todayDate;
        }

        var toLocation = await _context.AssetLocations.FirstOrDefaultAsync(al => al.LocationId == transfer.ToLocationId);
        if (toLocation != null)
        {
            toLocation.IsCurrent = true;
            toLocation.StartDate = todayDate;
        }
    }

    private static string BuildHandoverDetailsJson(TransferRecord transfer, string side)
    {
        var fromDept = transfer.FromLocation.Department?.Name ?? "";
        var toDept = transfer.ToLocation.Department?.Name ?? "";
        var instanceCode = transfer.AssetInstance?.InstanceCode ?? "";
        var assetCode = transfer.AssetInstance?.Asset?.Code ?? "";
        var assetName = transfer.AssetInstance?.Asset?.Name ?? "";

        string summary = string.Equals(side, "Sender", StringComparison.OrdinalIgnoreCase)
            ? $"Bên gửi bàn giao tài sản {instanceCode} — {assetName} từ {fromDept} đến {toDept}."
            : $"Bên nhận tiếp nhận tài sản {instanceCode} — {assetName} tại {toDept} (xuất từ {fromDept}).";

        return JsonSerializer.Serialize(new TransferHandoverDetailsDto
        {
            Side = side,
            ProtocolCode = "SBB" + transfer.TransferId,
            AssetRequestId = transfer.AssetRequestId,
            FromDepartment = fromDept,
            ToDepartment = toDept,
            InstanceCode = instanceCode,
            AssetCode = assetCode,
            AssetName = assetName,
            Summary = summary
        });
    }
}
