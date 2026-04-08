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

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/transfer")]
[Authorize]
public class TransferRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _transferRequestTypeId;
    /// <summary>RoleId trưởng phòng ban (seed: 4, giống DisposalRequestsController).</summary>
    private readonly int _departmentHeadRoleId;

    public TransferRequestsController(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
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
                IsReceiverConfirmed = tr.IsReceiverConfirmed
            })
            .ToListAsync();

        return Ok(list);
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

        if (dto.AssetInstanceId <= 0)
            return BadRequest("AssetInstanceId is required.");

        var instance = await _db.AssetInstances
            .Include(ai => ai.Asset)
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == dto.AssetInstanceId);
        if (instance == null)
            return NotFound($"AssetInstanceId {dto.AssetInstanceId} not found.");

        // NOTE: Frontend selects departments as "locations" (FromLocationId/ToLocationId are DepartmentId).
        if (dto.FromLocationId == dto.ToLocationId)
            return BadRequest("Vị trí nguồn và vị trí đích không được trùng nhau.");

        var fromDeptExists = await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == dto.FromLocationId);
        var toDeptExists = await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == dto.ToLocationId);
        if (!fromDeptExists)
            return BadRequest("Phòng ban nguồn (FromLocationId) không tồn tại trong hệ thống.");
        if (!toDeptExists)
            return BadRequest("Phòng ban đích (ToLocationId) không tồn tại trong hệ thống.");

        var isAccountant = User.IsInRole("ACCOUNTANT");
        if (!isAccountant)
        {
            var isDepartmentHead = await _db.UserRoles.AsNoTracking()
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == _departmentHeadRoleId);
            if (isDepartmentHead)
            {
                var userDeptId = await _db.Employees.AsNoTracking()
                    .Where(e => e.UserId == userId)
                    .Select(e => (int?)e.DepartmentId)
                    .FirstOrDefaultAsync();
                if (!userDeptId.HasValue)
                    return BadRequest("Trưởng phòng ban cần được gán phòng ban trong hồ sơ nhân viên để tạo yêu cầu điều chuyển.");
                if (dto.FromLocationId != userDeptId.Value)
                    return BadRequest("Phòng ban nguồn phải là phòng ban của bạn (đơn vị đang quản lý tài sản).");
            }
        }

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

        var assetRequest = new AssetRequest
        {
            UserId = userId,
            RequestTypeId = _transferRequestTypeId,
            AssetId = instance.AssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = title,
            Description = dto.Description,
            ProposedData = null,
            Status = 1,
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
            ToStatus = 1,
            Action = 0,
            ActionByUserId = userId,
            ActionRoleId = actionRoleId,
            Comment = "Transfer requested",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, recordId = transfer.TransferId });
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

        var transfer = await _db.TransferRecords
            .Include(tr => tr.AssetRequest)
            .Include(tr => tr.FromLocation)
            .Include(tr => tr.ToLocation)
            .FirstOrDefaultAsync(tr => tr.AssetRequestId == assetRequestId);

        if (transfer == null)
            return NotFound(new { message = $"Transfer request {assetRequestId} not found." });

        if (transfer.AssetRequest?.CreatedBy != userId)
            return Forbid();

        var status = transfer.AssetRequest?.Status ?? 0;
        if (status > 1)
            return BadRequest("Chỉ được xóa yêu cầu khi đang ở trạng thái Nháp hoặc Đã nộp.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var assetInstanceId = transfer.AssetInstanceId;

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

            var assetRequest = await _db.AssetRequests.FirstOrDefaultAsync(r => r.AssetRequestId == assetRequestId);
            if (assetRequest != null) _db.AssetRequests.Remove(assetRequest);

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
