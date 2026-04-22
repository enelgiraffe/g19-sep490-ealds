using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/transfer")]
[Authorize]
public class TransferRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _transferRequestTypeId;

    public TransferRequestsController(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
    }

    /// <summary>
    /// GET /api/Assets/Requests/transfer - Danh sách yêu cầu điều chuyển.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransferRequestListItemDTO>>> GetList()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var isAccountant = User.IsInRole("ACCOUNTANT");
        var userDeptId = await _db.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        var query = _db.TransferRecords
            .AsNoTracking()
            .Include(tr => tr.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(tr => tr.AssetRequest)
            .Include(tr => tr.FromLocation).ThenInclude(fl => fl.Department)
            .Include(tr => tr.ToLocation).ThenInclude(tl => tl.Department)
            .Where(tr =>
                isAccountant
                || tr.AssetRequest.CreatedBy == userId
                || (userDeptId.HasValue && tr.FromLocation.DepartmentId == userDeptId.Value)
                || (userDeptId.HasValue && tr.ToLocation.DepartmentId == userDeptId.Value))
            .OrderByDescending(tr => tr.TransferDate);

        var list = await query
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
                CreatedByName = _db.Employees
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
                AccountantComment = _db.Approvals.AsNoTracking()
                    .Join(_db.Roles.AsNoTracking(), a => a.ApprovedRoleId, r => r.RoleId, (a, r) => new { a, r })
                    .Where(x => x.a.AssetRequestId == tr.AssetRequestId
                        && x.r.Code != null
                        && x.r.Code.ToUpper() == "ACCOUNTANT")
                    .OrderByDescending(x => x.a.DecisionDate)
                    .Select(x => x.a.Comment)
                    .FirstOrDefault(),
                DirectorComment = _db.Approvals.AsNoTracking()
                    .Join(_db.Roles.AsNoTracking(), a => a.ApprovedRoleId, r => r.RoleId, (a, r) => new { a, r })
                    .Where(x => x.a.AssetRequestId == tr.AssetRequestId
                        && x.r.Code != null
                        && x.r.Code.Trim().ToUpper() == "DIRECTOR")
                    .OrderByDescending(x => x.a.DecisionDate)
                    .Select(x => x.a.Comment)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var proposedDrafts = await _db.AssetRequests
            .AsNoTracking()
            .Where(ar => ar.RequestTypeId == _transferRequestTypeId
                && ar.Status == 0
                && (isAccountant || ar.CreatedBy == userId)
                && !_db.TransferRecords.Any(t => t.AssetRequestId == ar.AssetRequestId))
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
                CreatedByName = _db.Employees
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

        return Ok(list.Concat(proposedDrafts).OrderByDescending(x => x.TransferDate).ToList());
    }

    /// <summary>
    /// GET /api/Assets/Requests/transfer/{assetRequestId}/handover-records — biên bản bàn giao theo từng thao tác gửi/nhận.
    /// </summary>
    [HttpGet("{id:int}/handover-records")]
    public async Task<ActionResult<IEnumerable<TransferHandoverRecordItemDto>>> GetHandoverRecords(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var isAccountant = User.IsInRole("ACCOUNTANT");
        var userDeptId = await _db.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        var transfer = await _db.TransferRecords
            .AsNoTracking()
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.AssetRequest)
            .FirstOrDefaultAsync(t => t.AssetRequestId == id);

        if (transfer == null)
            return NotFound("Transfer request not found");

        var ar = transfer.AssetRequest;
        if (ar == null || ar.RequestTypeId != _transferRequestTypeId)
            return NotFound("Transfer request not found");

        var canView = isAccountant
            || ar.CreatedBy == userId
            || (userDeptId.HasValue && transfer.FromLocation.DepartmentId == userDeptId.Value)
            || (userDeptId.HasValue && transfer.ToLocation.DepartmentId == userDeptId.Value);
        if (!canView)
            return Forbid();

        var handovers = await _db.TransferHandoverRecords
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

            var actorName = await _db.Employees.AsNoTracking()
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

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransferRequest([FromBody] TransferRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (dto.IncompleteDraft)
        {
            if (!dto.SaveAsDraft)
                return BadRequest("Bản nháp chưa hoàn tất phải được lưu ở trạng thái nháp.");
            return await CreateProposedDataOnlyTransferDraft(dto, userId);
        }

        if (dto.AssetInstanceId <= 0)
            return BadRequest("AssetInstanceId is required.");

        var instance = await _db.AssetInstances
            .Include(ai => ai.Asset)
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == dto.AssetInstanceId);
        if (instance == null)
            return NotFound($"AssetInstanceId {dto.AssetInstanceId} not found.");

        if (instance.Status != (int)AssetStatus.InUse)
            return BadRequest("Chỉ có thể điều chuyển cá thể đang ở trạng thái đang sử dụng.");

        // NOTE: Frontend selects departments as "locations" (FromLocationId/ToLocationId are DepartmentId).
        if (dto.FromLocationId == dto.ToLocationId)
            return BadRequest("Vị trí nguồn và vị trí đích không được trùng nhau.");

        var fromDeptExists = await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == dto.FromLocationId);
        var toDeptExists = await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == dto.ToLocationId);
        if (!fromDeptExists)
            return BadRequest("Phòng ban nguồn (FromLocationId) không tồn tại trong hệ thống.");
        if (!toDeptExists)
            return BadRequest("Phòng ban đích (ToLocationId) không tồn tại trong hệ thống.");

        if (await DepartmentAssetScope.AnyDepartmentsHaveInventoryInProgressAsync(
                _db,
                new[] { dto.FromLocationId, dto.ToLocationId }))
            return BadRequest(new { message = DepartmentAssetScope.InventoryInProgressBlockingMessage });

        var now = dto.TransferDate ?? DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var fromLocation = await _db.AssetLocations
            .FirstOrDefaultAsync(al =>
                al.AssetInstanceId == dto.AssetInstanceId &&
                al.DepartmentId == dto.FromLocationId &&
                al.IsCurrent);

        if (fromLocation == null)
            return BadRequest("Không tìm thấy vị trí hiện tại của tài sản tại phòng ban nguồn. Vui lòng kiểm tra lại 'Từ vị trí'.");

        // Keep the old location current until it's approved.
        // We still create the new location string to store ToLocationId, but it stays IsCurrent = false.
        var toLocation = new AssetLocation
        {
            AssetInstanceId = dto.AssetInstanceId,
            DepartmentId = dto.ToLocationId,
            StartDate = today,
            EndDate = null,
            IsCurrent = false, // Pending approval
            Note = dto.Description
        };
        _db.AssetLocations.Add(toLocation);

        var title = dto.Title ?? $"Transfer instance {dto.AssetInstanceId} from dept {dto.FromLocationId} to dept {dto.ToLocationId}";
        var initialStepId = await _db.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _transferRequestTypeId)
            .SelectMany(rt => _db.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            return BadRequest($"No workflow step configured for RequestTypeId '{_transferRequestTypeId}'.");

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

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

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

        _db.TransferRecords.Add(transfer);

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = requestStatus,
            Action = 0,
            ActionByUserId = userId,
            ActionRoleId = actionRoleId,
            Comment = saveAsDraft ? "Lưu nháp điều chuyển" : "Transfer requested",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        if (dto.ReplaceIncompleteAssetRequestId is int rId && rId > 0)
            await TryDeleteReplacedIncompleteTransferDraftAsync(rId, userId);

        if (!saveAsDraft)
            await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, recordId = transfer.TransferId });
    }

    private async Task TryDeleteReplacedIncompleteTransferDraftAsync(int assetRequestId, int userId)
    {
        var ar = await _db.AssetRequests
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId && a.RequestTypeId == _transferRequestTypeId);
        if (ar == null) return;
        if (ar.CreatedBy != userId) return;
        if (ar.Status != 0) return;
        if (await _db.TransferRecords.AnyAsync(t => t.AssetRequestId == assetRequestId)) return;
        var records = await _db.AssetRequestRecords
            .Where(r => r.AssetRequestId == assetRequestId)
            .ToListAsync();
        if (records.Count > 0) _db.AssetRequestRecords.RemoveRange(records);
        _db.AssetRequests.Remove(ar);
        await _db.SaveChangesAsync();
    }

    private async Task<IActionResult> CreateProposedDataOnlyTransferDraft(TransferRequestDTO dto, int userId)
    {
        var json = string.IsNullOrWhiteSpace(dto.DraftFormJson) ? "{}" : dto.DraftFormJson.Trim();
        if (json.Length > 1_000_000)
            return BadRequest("Bản nháp quá lớn.");

        var initialStepId = await _db.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _transferRequestTypeId)
            .SelectMany(rt => _db.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            return BadRequest($"No workflow step configured for RequestTypeId '{_transferRequestTypeId}'.");

        const string title = "Bản nháp điều chuyển";
        var assetRequest = new AssetRequest
        {
            UserId = userId,
            RequestTypeId = _transferRequestTypeId,
            AssetId = null,
            AssetInstanceId = null,
            Title = title,
            Description = null,
            ProposedData = json,
            Status = 0,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        var actionRoleId = userRole?.RoleId ?? 1;
        _db.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = 0,
            Action = 0,
            ActionByUserId = userId,
            ActionRoleId = actionRoleId,
            Comment = "Lưu nháp (chưa hoàn tất thông tin)",
            OccurredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, recordId = 0, incompleteDraft = true });
    }

    /// <summary>
    /// PUT /api/Assets/Requests/transfer/{assetRequestId}/draft — cập nhật bản nháp chưa hoàn tất (chỉ <c>ProposedData</c>).
    /// </summary>
    [HttpPut("{assetRequestId:int}/draft")]
    public async Task<IActionResult> UpdateIncompleteTransferDraft(int assetRequestId, [FromBody] UpdateTransferDraftBody? body)
    {
        if (body == null)
            return BadRequest("Request body is required.");
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var json = string.IsNullOrWhiteSpace(body.DraftFormJson) ? "{}" : body.DraftFormJson.Trim();
        if (json.Length > 1_000_000)
            return BadRequest("Bản nháp quá lớn.");

        var ar = await _db.AssetRequests
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId && a.RequestTypeId == _transferRequestTypeId);
        if (ar == null)
            return NotFound(new { message = $"Transfer request {assetRequestId} not found." });
        if (ar.CreatedBy != userId)
            return Forbid();
        if (ar.Status != 0)
            return BadRequest("Chỉ được sửa bản nháp (Nháp) chưa gửi.");
        if (await _db.TransferRecords.AnyAsync(t => t.AssetRequestId == assetRequestId))
            return BadRequest("Yêu cầu đã có bản ghi điều chuyển, không cập nhật theo bản nháp JSON.");

        ar.ProposedData = json;
        await _db.SaveChangesAsync();
        return Ok(new { assetRequestId = ar.AssetRequestId });
    }

    /// <summary>
    /// DELETE /api/Assets/Requests/transfer/{assetRequestId} - Xóa yêu cầu điều chuyển (chỉ khi chưa duyệt).
    /// </summary>
    [HttpDelete("{assetRequestId:int}")]
    public async Task<IActionResult> DeleteTransferRequest(int assetRequestId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var ar = await _db.AssetRequests
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId && a.RequestTypeId == _transferRequestTypeId);
        if (ar == null)
            return NotFound(new { message = $"Transfer request {assetRequestId} not found." });

        if (ar.CreatedBy != userId)
            return Forbid();

        if (ar.Status > 1)
            return BadRequest("Chỉ được xóa yêu cầu khi đang ở trạng thái Nháp hoặc Đã nộp.");

        var transfer = await _db.TransferRecords
            .Include(tr => tr.FromLocation)
            .Include(tr => tr.ToLocation)
            .FirstOrDefaultAsync(tr => tr.AssetRequestId == assetRequestId);

        if (transfer == null)
        {
            await using var txDraft = await _db.Database.BeginTransactionAsync();
            try
            {
                if (ar.Status != 0)
                {
                    await txDraft.RollbackAsync();
                    return BadRequest("Chỉ được xóa bản nháp chưa gửi.");
                }
                var records = await _db.AssetRequestRecords
                    .Where(r => r.AssetRequestId == assetRequestId)
                    .ToListAsync();
                if (records.Count > 0) _db.AssetRequestRecords.RemoveRange(records);
                _db.AssetRequests.Remove(ar);
                await _db.SaveChangesAsync();
                await txDraft.CommitAsync();
                return NoContent();
            }
            catch
            {
                await txDraft.RollbackAsync();
                throw;
            }
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Trả lại trạng thái IsCurrent cho vị trí đích nếu nó chưa được cấp quyền
            var toLocation = await _db.AssetLocations.FirstOrDefaultAsync(al => al.LocationId == transfer.ToLocationId);
            if (toLocation != null)
            {
                _db.AssetLocations.Remove(toLocation); // Bỏ đi vị trí nháp
            }

            var records = await _db.AssetRequestRecords
                .Where(r => r.AssetRequestId == assetRequestId)
                .ToListAsync();
            if (records.Count > 0) _db.AssetRequestRecords.RemoveRange(records);

            _db.TransferRecords.Remove(transfer);

            var arReload = await _db.AssetRequests.FirstOrDefaultAsync(r => r.AssetRequestId == assetRequestId);
            if (arReload != null) _db.AssetRequests.Remove(arReload);

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

    [HttpPost("{id:int}/confirm-send")]
    public async Task<IActionResult> ConfirmSend(int id, [FromBody] TransferHandoverConfirmBody? body)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar == null || ar.RequestTypeId != _transferRequestTypeId) return NotFound("Transfer request not found");
        if (ar.Status != 4) return BadRequest("Chỉ được xác nhận gửi khi yêu cầu đã được Giám đốc phê duyệt (Status = 4).");

        var transfer = await _db.TransferRecords
            .Include(t => t.FromLocation).ThenInclude(fl => fl.Department)
            .Include(t => t.ToLocation).ThenInclude(tl => tl.Department)
            .Include(t => t.AssetInstance).ThenInclude(ai => ai.Asset)
            .FirstOrDefaultAsync(t => t.AssetRequestId == id);
        if (transfer == null) return NotFound("Transfer record not found");

        if (transfer.IsSenderConfirmed) return BadRequest("Bên gửi đã xác nhận rồi.");

        var userDeptId = await _db.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();
        var isAccountant = User.IsInRole("ACCOUNTANT");
        var canSend = isAccountant
            || ar.CreatedBy == userId
            || transfer.FromUserId == userId
            || (userDeptId.HasValue && transfer.FromLocation.DepartmentId == userDeptId.Value);
        if (!canSend) return Forbid();

        if (await DepartmentAssetScope.DepartmentHasInventoryInProgressAsync(
                _db,
                transfer.FromLocation.DepartmentId))
            return BadRequest(new { message = DepartmentAssetScope.InventoryInProgressBlockingMessage });

        var note = string.IsNullOrWhiteSpace(body?.Note) ? null : body!.Note!.Trim();
        if (note != null && note.Length > 2000)
            return BadRequest("Ghi chú không quá 2000 ký tự.");

        var occurred = DateTime.UtcNow;
        _db.TransferHandoverRecords.Add(new TransferHandoverRecord
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
        var senderUserRole = await _db.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.UserId == userId);
        var senderActionRoleId = senderUserRole?.RoleId ?? 1;

        var rec = new AssetRequestRecord
        {
            AssetRequestId = id,
            FromStatus = 4, ToStatus = 4,
            Action = 1,
            ActionByUserId = userId,
            ActionRoleId = senderActionRoleId,
            Comment = "Bên gửi đã xác nhận chuyển",
            OccurredAt = DateTime.UtcNow
        };
        _db.AssetRequestRecords.Add(rec);

        await TryExecutePhysicalTransfer(transfer);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Xác nhận gửi thành công.", isReady = transfer.IsSenderConfirmed && transfer.IsReceiverConfirmed });
    }

    [HttpPost("{id:int}/confirm-receive")]
    public async Task<IActionResult> ConfirmReceive(int id, [FromBody] TransferHandoverConfirmBody? body)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar == null || ar.RequestTypeId != _transferRequestTypeId) return NotFound("Transfer request not found");
        if (ar.Status != 4) return BadRequest("Chỉ được xác nhận nhận khi yêu cầu đã được Giám đốc phê duyệt (Status = 4).");

        var transfer = await _db.TransferRecords
            .Include(t => t.FromLocation).ThenInclude(fl => fl.Department)
            .Include(t => t.ToLocation).ThenInclude(tl => tl.Department)
            .Include(t => t.AssetInstance).ThenInclude(ai => ai.Asset)
            .FirstOrDefaultAsync(t => t.AssetRequestId == id);
        if (transfer == null) return NotFound("Transfer record not found");

        if (transfer.IsReceiverConfirmed) return BadRequest("Bên nhận đã xác nhận rồi.");
        if (!transfer.IsSenderConfirmed)
            return BadRequest("Bên gửi chưa xác nhận bàn giao. Bên nhận chưa thể xác nhận đã nhận.");

        var userDeptId = await _db.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();
        var isAccountant = User.IsInRole("ACCOUNTANT");
        var canReceive = isAccountant
            || transfer.ToUserId == userId
            || (userDeptId.HasValue && transfer.ToLocation.DepartmentId == userDeptId.Value);
        if (!canReceive) return Forbid();

        if (await DepartmentAssetScope.DepartmentHasInventoryInProgressAsync(
                _db,
                transfer.ToLocation.DepartmentId))
            return BadRequest(new { message = DepartmentAssetScope.InventoryInProgressBlockingMessage });

        var note = string.IsNullOrWhiteSpace(body?.Note) ? null : body!.Note!.Trim();
        if (note != null && note.Length > 2000)
            return BadRequest("Ghi chú không quá 2000 ký tự.");

        var occurred = DateTime.UtcNow;
        _db.TransferHandoverRecords.Add(new TransferHandoverRecord
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
        var receiverUserRole = await _db.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(ur => ur.UserId == userId);
        var receiverActionRoleId = receiverUserRole?.RoleId ?? 1;

        var rec = new AssetRequestRecord
        {
            AssetRequestId = id,
            FromStatus = 4, ToStatus = 4,
            Action = 1,
            ActionByUserId = userId,
            ActionRoleId = receiverActionRoleId,
            Comment = "Bên nhận đã xác nhận nhận",
            OccurredAt = DateTime.UtcNow
        };
        _db.AssetRequestRecords.Add(rec);

        await TryExecutePhysicalTransfer(transfer);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Xác nhận nhận thành công.", isReady = transfer.IsSenderConfirmed && transfer.IsReceiverConfirmed });
    }

    private async Task TryExecutePhysicalTransfer(TransferRecord transfer)
    {
        if (transfer.IsSenderConfirmed && transfer.IsReceiverConfirmed)
        {
            var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var fromLocation = await _db.AssetLocations.FirstOrDefaultAsync(al => al.LocationId == transfer.FromLocationId);
            if (fromLocation != null && fromLocation.IsCurrent)
            {
                fromLocation.IsCurrent = false;
                fromLocation.EndDate = todayDate;
            }

            var otherLocations = await _db.AssetLocations
                .Where(al => al.AssetInstanceId == transfer.AssetInstanceId && al.IsCurrent && al.LocationId != transfer.FromLocationId)
                .ToListAsync();
            foreach (var loc in otherLocations)
            {
                loc.IsCurrent = false;
                loc.EndDate = todayDate;
            }

            var toLocation = await _db.AssetLocations.FirstOrDefaultAsync(al => al.LocationId == transfer.ToLocationId);
            if (toLocation != null)
            {
                toLocation.IsCurrent = true;
                toLocation.StartDate = todayDate;
            }
        }
    }

    private static string BuildHandoverDetailsJson(TransferRecord transfer, string side)
    {
        var fromDept = transfer.FromLocation.Department?.Name ?? "";
        var toDept = transfer.ToLocation.Department?.Name ?? "";
        var instanceCode = transfer.AssetInstance?.InstanceCode ?? "";
        var assetCode = transfer.AssetInstance?.Asset?.Code ?? "";
        var assetName = transfer.AssetInstance?.Asset?.Name ?? "";
        string summary;
        if (string.Equals(side, "Sender", StringComparison.OrdinalIgnoreCase))
            summary = $"Bên gửi bàn giao tài sản {instanceCode} — {assetName} từ {fromDept} đến {toDept}.";
        else
            summary = $"Bên nhận tiếp nhận tài sản {instanceCode} — {assetName} tại {toDept} (xuất từ {fromDept}).";

        var dto = new TransferHandoverDetailsDto
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
        };

        return JsonSerializer.Serialize(dto);
    }
}
