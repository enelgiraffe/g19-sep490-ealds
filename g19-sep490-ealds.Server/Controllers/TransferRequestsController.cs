using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/transfer")]
[Authorize]
public class TransferRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _transferRequestTypeId;

    public TransferRequestsController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
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

        var query = _db.TransferRecords
            .AsNoTracking()
            .Include(tr => tr.Asset)
            .Include(tr => tr.AssetRequest)
            .Include(tr => tr.FromLocation).ThenInclude(fl => fl.Department)
            .Include(tr => tr.ToLocation).ThenInclude(tl => tl.Department)
            .Where(tr => isAccountant || tr.AssetRequest.CreatedBy == userId)
            .OrderByDescending(tr => tr.TransferDate);

        var list = await query
            .Select(tr => new TransferRequestListItemDTO
            {
                RecordId = tr.RecordId,
                AssetRequestId = tr.AssetRequestId,
                Code = "SBB" + tr.RecordId,
                TransferDate = tr.TransferDate,
                AssetCode = tr.Asset.Code,
                AssetName = tr.Asset.Name,
                FromDepartment = tr.FromLocation.Department.Name,
                ToDepartment = tr.ToLocation.Department.Name,
                Quantity = tr.Asset.Quantity,
                Status = tr.AssetRequest.Status,
                StatusName =
                    tr.AssetRequest.Status == 0 ? "Nháp" :
                    tr.AssetRequest.Status == 1 ? "Đã nộp" :
                    tr.AssetRequest.Status == 2 ? "Chờ phê duyệt" :
                    tr.AssetRequest.Status == 3 ? "Từ chối" :
                    tr.AssetRequest.Status == 4 ? "Phê duyệt" :
                    "Không xác định",
                Reason = tr.AssetRequest.Description
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransferRequest([FromBody] TransferRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        // NOTE: Frontend selects departments as "locations" (FromLocationId/ToLocationId are DepartmentId).
        if (dto.FromLocationId == dto.ToLocationId)
            return BadRequest("Vị trí nguồn và vị trí đích không được trùng nhau.");

        var fromDeptExists = await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == dto.FromLocationId);
        var toDeptExists = await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == dto.ToLocationId);
        if (!fromDeptExists)
            return BadRequest("Phòng ban nguồn (FromLocationId) không tồn tại trong hệ thống.");
        if (!toDeptExists)
            return BadRequest("Phòng ban đích (ToLocationId) không tồn tại trong hệ thống.");

        var now = dto.TransferDate ?? DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Find current location of this asset in the selected from-department.
        var fromLocation = await _db.AssetLocations
            .FirstOrDefaultAsync(al => al.AssetId == dto.AssetId && al.DepartmentId == dto.FromLocationId && al.IsCurrent);

        if (fromLocation == null)
            return BadRequest("Không tìm thấy vị trí hiện tại của tài sản tại phòng ban nguồn. Vui lòng kiểm tra lại 'Từ vị trí'.");

        // Close current locations for this asset
        var currentLocations = await _db.AssetLocations
            .Where(al => al.AssetId == dto.AssetId && al.IsCurrent)
            .ToListAsync();
        foreach (var loc in currentLocations)
        {
            loc.IsCurrent = false;
            loc.EndDate = today;
        }

        // Create new current location for destination department
        var toLocation = new AssetLocation
        {
            AssetId = dto.AssetId,
            DepartmentId = dto.ToLocationId,
            StartDate = today,
            EndDate = null,
            IsCurrent = true,
            Note = dto.Description
        };
        _db.AssetLocations.Add(toLocation);

        var title = dto.Title ?? $"Transfer asset {dto.AssetId} from dept {dto.FromLocationId} to dept {dto.ToLocationId}";

        var assetRequest = new AssetRequest
        {
            UserId = userId,
            RequestTypeId = _transferRequestTypeId,
            AssetId = dto.AssetId,
            Title = title,
            Description = dto.Description,
            ProposedData = null,
            // User is submitting a transfer request (not saving draft)
            Status = 1,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var transfer = new TransferRecord
        {
            AssetId = dto.AssetId,
            AssetRequestId = assetRequest.AssetRequestId,
            FromLocationId = fromLocation.LocationId,
            ToLocationId = toLocation.LocationId,
            FromUserId = dto.FromUserId ?? userId,
            ToUserId = dto.ToUserId,
            TransferDate = now,
            ExecuteBy = dto.ExecuteBy == 0 ? userId : dto.ExecuteBy
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

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, recordId = transfer.RecordId });
    }

    /// <summary>
    /// DELETE /api/Assets/Requests/transfer/{assetRequestId} - Xóa yêu cầu điều chuyển (chỉ khi chưa duyệt).
    /// Rollback AssetLocation về lại vị trí/phòng ban nguồn đã ghi nhận trong TransferRecord.
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
        // Allow delete only for Draft(0) or Submitted(1)
        if (status > 1)
            return BadRequest("Chỉ được xóa yêu cầu khi đang ở trạng thái Nháp hoặc Đã nộp.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Rollback current location to the FromLocation recorded in TransferRecord
            var assetId = transfer.AssetId;

            var currentLocations = await _db.AssetLocations
                .Where(al => al.AssetId == assetId && al.IsCurrent)
                .ToListAsync();
            foreach (var loc in currentLocations)
            {
                loc.IsCurrent = false;
                loc.EndDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
            }

            var fromLocation = await _db.AssetLocations.FirstOrDefaultAsync(al => al.LocationId == transfer.FromLocationId);
            if (fromLocation != null)
            {
                fromLocation.IsCurrent = true;
                fromLocation.EndDate = null;
            }

            // Remove created destination location record if it matches ToLocationId
            var toLocation = await _db.AssetLocations.FirstOrDefaultAsync(al => al.LocationId == transfer.ToLocationId);
            if (toLocation != null)
            {
                toLocation.IsCurrent = false;
            }

            // Delete request records and transfer record, then asset request
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
}
