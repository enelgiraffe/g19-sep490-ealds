using System;
using System.Linq;
using System.Security.Claims;
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
[Route("api/Assets/Requests/disposal")]
public class DisposalRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _disposalRequestTypeId;

    public DisposalRequestsController(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _disposalRequestTypeId = configuration.GetValue<int>("App:DisposalRequestTypeId", 5);
    }

    /// <summary>
    /// GET /api/Assets/Requests/disposal - Danh sách yêu cầu thanh lý.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransferRequestListItemDTO>>> GetList()
    {
        var list = await _db.DisposalRecords
            .AsNoTracking()
            .Include(d => d.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(d => d.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(d => d.AssetRequest)
            .Where(d => d.AssetRequest != null && d.AssetRequest.RequestTypeId == _disposalRequestTypeId)
            .OrderByDescending(d => d.AssetRequest!.CreateDate)
            .Select(d => new TransferRequestListItemDTO
            {
                RecordId = d.DiposalId,
                AssetRequestId = d.AssetRequestId,
                Code = "STL" + d.DiposalId,
                TransferDate = d.DiposalDate,
                AssetCode = d.AssetInstance.Asset != null ? d.AssetInstance.Asset.Code : string.Empty,
                AssetName = d.AssetInstance.Asset != null ? d.AssetInstance.Asset.Name : string.Empty,
                AssetInstanceId = d.AssetInstanceId,
                InstanceCode = d.AssetInstance.InstanceCode,
                FromDepartment = d.AssetInstance.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Department != null ? al.Department.Name : string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                ToDepartment = string.Empty,
                Quantity = 1,
                Status = d.AssetRequest!.Status,
                StatusName =
                    d.AssetRequest.Status == 0 ? "Nháp" :
                    d.AssetRequest.Status == 1 ? "Chờ phê duyệt" :
                    d.AssetRequest.Status == 2 ? "Đã duyệt" :
                    d.AssetRequest.Status == 3 ? "Từ chối" :
                    d.AssetRequest.Status == 4 ? "Đang thực hiện" :
                    d.AssetRequest.Status == 5 ? "Hoàn thành" :
                    "Không xác định",
                Reason = d.Reason,
                FromDepartmentId = d.AssetInstance.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.DepartmentId)
                    .FirstOrDefault(),
                ToDepartmentId = 0,
                CreatedBy = d.AssetRequest.CreatedBy,
                IsSenderConfirmed = false,
                IsReceiverConfirmed = false
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDisposalRequest([FromBody] AssetDisposalRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        if (!dto.AssetInstanceId.HasValue || dto.AssetInstanceId.Value <= 0)
            return BadRequest("AssetInstanceId is required.");

        var instance = await _db.AssetInstances
            .Include(ai => ai.Asset)
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == dto.AssetInstanceId.Value);
        if (instance == null)
            return NotFound($"Asset instance {dto.AssetInstanceId.Value} not found.");

        var catalogAssetId = instance.AssetId;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorUserId = int.TryParse(userIdClaim, out var parsedUserId) && parsedUserId > 0
            ? parsedUserId
            : dto.CreatedBy;

        // Fast path: allow based on JWT role claims first.
        var hasManagerRoleFromToken =
            User.IsInRole("DepartmentManager") ||
            User.IsInRole("DEPARTMENT_MANAGER") ||
            User.IsInRole("DEPT_MANAGER") ||
            User.IsInRole("DEPARTMENT_HEAD") ||
            User.IsInRole("HEAD_OF_DEPARTMENT") ||
            User.IsInRole("TRUONG_PHONG") ||
            User.IsInRole("TRUONGPHONG");

        // permission check: only department managers can submit disposal
        var userRoles = await _db.UserRoles
            .Include(ur => ur.Role)
            .AsNoTracking()
            .Where(ur => ur.UserId == actorUserId)
            .ToListAsync();

        // Auto-detect manager-like role IDs from DB (SQL-translatable predicates only).
        var managerRoleIds = await _db.Roles
            .AsNoTracking()
            .Where(r =>
                (r.Code != null &&
                    (
                        r.Code.ToUpper() == "DEPARTMENTMANAGER" ||
                        r.Code.ToUpper() == "DEPARTMENT_MANAGER" ||
                        r.Code.ToUpper() == "DEPT_MANAGER" ||
                        r.Code.ToUpper() == "DEPARTMENT_HEAD" ||
                        r.Code.ToUpper() == "HEAD_OF_DEPARTMENT" ||
                        r.Code.ToUpper() == "TRUONG_PHONG" ||
                        r.Code.ToUpper() == "TRUONGPHONG"
                    )) ||
                (r.Name != null &&
                    (
                        r.Name.ToUpper().Contains("MANAGER") ||
                        r.Name.ToUpper().Contains("HEAD") ||
                        r.Name.ToUpper().Contains("TRUONG PHONG") ||
                        r.Name.ToUpper().Contains("TRUONG BO PHAN")
                    ))
            )
            .Select(r => r.RoleId)
            .ToListAsync();
        var isDeptManager = hasManagerRoleFromToken || userRoles.Any(ur =>
            managerRoleIds.Contains(ur.RoleId) ||
            (ur.Role.Code != null &&
                (
                    ur.Role.Code.ToUpper() == "DEPARTMENTMANAGER" ||
                    ur.Role.Code.ToUpper() == "DEPARTMENT_MANAGER" ||
                    ur.Role.Code.ToUpper() == "DEPT_MANAGER" ||
                    ur.Role.Code.ToUpper() == "DEPARTMENT_HEAD" ||
                    ur.Role.Code.ToUpper() == "HEAD_OF_DEPARTMENT" ||
                    ur.Role.Code.ToUpper() == "TRUONG_PHONG" ||
                    ur.Role.Code.ToUpper() == "TRUONGPHONG"
                )) ||
            (ur.Role.Name != null &&
                (
                    ur.Role.Name.ToUpper().Contains("MANAGER") ||
                    ur.Role.Name.ToUpper().Contains("HEAD") ||
                    ur.Role.Name.ToUpper().Contains("TRUONG PHONG") ||
                    ur.Role.Name.ToUpper().Contains("TRUONG BO PHAN")
                ))
        );

        if (!isDeptManager)
        {
            return StatusCode(403, new
            {
                message = "Bạn không có quyền gửi yêu cầu thanh lý (chỉ trưởng phòng ban).",
                actorUserId,
                managerRoleIds,
                currentUserRoleIds = userRoles.Select(x => x.RoleId).Distinct().ToArray(),
                currentUserRoleCodes = userRoles.Select(x => x.Role?.Code).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray(),
                currentUserRoleNames = userRoles.Select(x => x.Role?.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray()
            });
        }

        var resolvedRequestTypeId = dto.RequestTypeId ?? _disposalRequestTypeId;
        var requestTypeExists = await _db.RequestTypes
            .AsNoTracking()
            .AnyAsync(rt => rt.RequestTypeId == resolvedRequestTypeId);
        if (!requestTypeExists)
        {
            return BadRequest($"RequestTypeId '{resolvedRequestTypeId}' không tồn tại trong bảng RequestType.");
        }
        var initialStepId = await _db.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == resolvedRequestTypeId)
            .SelectMany(rt => _db.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            return BadRequest($"No workflow step configured for RequestTypeId '{resolvedRequestTypeId}'.");

        var assetRequest = new AssetRequest
        {
            UserId = actorUserId,
            RequestTypeId = resolvedRequestTypeId,
            AssetId = catalogAssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = dto.Title,
            Description = dto.Description,
            ProposedData = null,
            Status = 0,
            CreatedBy = actorUserId,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var diposal = new DisposalRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            AssetInstanceId = dto.AssetInstanceId.Value,
            DiposalMethod = dto.DiposalMethod,
            DiposalValue = dto.DiposalValue,
            DiposalDate = DateTime.UtcNow,   // Ngày đề nghị do server gán, không nhận từ client
            Reason = dto.Reason,
            ExecutedBy = actorUserId
        };

        _db.DisposalRecords.Add(diposal);

        // NOTE: Asset status is NOT changed to Disposed here.
        // Status will be updated only after the disposal request is approved/finalized.

        await _db.SaveChangesAsync();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == actorUserId);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = 0,
            Action = 0,
            ActionByUserId = actorUserId,
            ActionRoleId = actionRoleId,
            Comment = "Submitted disposal request",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, diposalId = diposal.DiposalId });
    }
}
