using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.Allocation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/allocation")]
[Authorize]
public class AllocationRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _allocationRequestTypeId;
    private readonly int _departmentHeadRoleId;

    public AllocationRequestsController(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _allocationRequestTypeId = configuration.GetValue<int>("App:AllocationRequestTypeId", 6);
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
    }

    /// <summary>Instances in warehouse (no current department assignment) for a catalog asset.</summary>
    [HttpGet("warehouse-available")]
    public async Task<ActionResult<int>> GetWarehouseAvailable([FromQuery] int assetId)
    {
        if (assetId <= 0)
            return BadRequest(new { message = "assetId is required." });
        if (!await _db.Assets.AsNoTracking().AnyAsync(a => a.AssetId == assetId))
            return NotFound(new { message = "Asset not found." });
        var n = await AllocationOrderWorkflow.GetWarehouseAvailableCountAsync(_db, assetId);
        return Ok(n);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentAllocationRequestDto dto)
    {
        if (dto?.Lines == null || dto.Lines.Count == 0)
            return BadRequest(new { message = "Cần ít nhất một dòng tài sản." });

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Nhập tiêu đề yêu cầu." });

        for (var i = 0; i < dto.Lines.Count; i++)
        {
            var line = dto.Lines[i];
            if (line.AssetTypeId <= 0 || line.AssetId <= 0 || line.Quantity < 1)
                return BadRequest(new { message = $"Dòng {i + 1}: chọn loại, tài sản và số lượng hợp lệ." });
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var actorUserId) || actorUserId <= 0)
            return Unauthorized();

        var inHeadRole = await _db.UserRoles.AsNoTracking()
            .AnyAsync(ur => ur.UserId == actorUserId && ur.RoleId == _departmentHeadRoleId);
        if (!inHeadRole)
            return StatusCode(403, new { message = "Chỉ trưởng phòng ban được gửi yêu cầu cấp phát." });

        var departmentId = await _db.Employees.AsNoTracking()
            .Where(e => e.UserId == actorUserId)
            .OrderBy(e => e.EmployeeId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();
        if (!departmentId.HasValue)
            return BadRequest(new { message = "Tài khoản chưa gắn phòng ban (nhân viên)." });

        if (!await _db.RequestTypes.AsNoTracking().AnyAsync(rt => rt.RequestTypeId == _allocationRequestTypeId))
            return BadRequest(new
            {
                message =
                    $"RequestType {_allocationRequestTypeId} chưa có trong bảng RequestType. Chạy script SQL cấp phát (seed loại yêu cầu) hoặc thêm bản ghi RequestType với WorkflowId hợp lệ."
            });

        var workflowId = await _db.RequestTypes.AsNoTracking()
            .Where(rt => rt.RequestTypeId == _allocationRequestTypeId)
            .Select(rt => (int?)rt.WorkflowId)
            .FirstOrDefaultAsync();
        if (!workflowId.HasValue || workflowId.Value <= 0)
            return BadRequest(new { message = "Loại yêu cầu cấp phát chưa gắn WorkflowId hợp lệ." });

        var initialStepId = await _db.WorkflowSteps.AsNoTracking()
            .Where(ws => ws.WorkflowId == workflowId.Value)
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            return BadRequest(new { message = "Chưa cấu hình bước workflow (WorkflowStep) cho workflow của loại yêu cầu cấp phát." });

        var proposedJson = AllocationOrderWorkflow.BuildProposedDataJson(departmentId.Value, dto.Lines);
        if (!AllocationOrderWorkflow.TryParseProposedData(proposedJson, out var root, out var parseErr) || root == null)
            return BadRequest(new { message = parseErr });

        var validation = await AllocationOrderWorkflow.ValidateLinesAgainstDatabaseAsync(_db, root);
        if (validation != null)
            return BadRequest(new { message = validation });

        var assetRequest = new AssetRequest
        {
            UserId = actorUserId,
            RequestTypeId = _allocationRequestTypeId,
            AssetId = null,
            AssetInstanceId = null,
            Title = dto.Title!.Trim(),
            Description = "Yêu cầu cấp phát tài sản từ kho về phòng ban",
            ProposedData = proposedJson,
            Status = AllocationOrderWorkflow.RequestStatusPendingAccountant,
            CreatedBy = actorUserId,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value,
            AllocationTargetDepartmentId = departmentId.Value
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        // Must reference an existing Role (FK). Disposal uses ?? 1; here we know the caller passed the dept-head gate.
        _db.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = actorUserId,
            ActionRoleId = _departmentHeadRoleId,
            Comment = "Gửi yêu cầu cấp phát",
            OccurredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return Ok(new { assetRequestId = assetRequest.AssetRequestId });
    }

    /// <summary>List allocation requests (department head: own department; accountant: all).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AllocationRequestListItemDto>>> GetList()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var actorUserId) || actorUserId <= 0)
            return Unauthorized();

        var isAccountant = await _db.UserRoles.AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == actorUserId &&
                (ur.Role != null && ur.Role.Code != null && ur.Role.Code.ToUpper() == "ACCOUNTANT"));

        int? filterDeptId = null;
        if (!isAccountant)
        {
            filterDeptId = await _db.Employees.AsNoTracking()
                .Where(e => e.UserId == actorUserId)
                .Select(e => (int?)e.DepartmentId)
                .FirstOrDefaultAsync();
            if (!filterDeptId.HasValue)
                return Ok(Array.Empty<AllocationRequestListItemDto>());
        }

        var baseRequests = _db.AssetRequests.AsNoTracking()
            .Where(r => r.RequestTypeId == _allocationRequestTypeId);
        if (!isAccountant)
            baseRequests = baseRequests.Where(r => r.AllocationTargetDepartmentId == filterDeptId!.Value);

        var raw = await (
            from r in baseRequests
            join o in _db.AssetAllocationOrders.AsNoTracking() on r.AssetRequestId equals o.AssetRequestId into og
            from o in og.DefaultIfEmpty()
            orderby r.CreateDate descending
            select new
            {
                r.AssetRequestId,
                OrderId = o != null ? (int?)o.AssetAllocationOrderId : null,
                r.Title,
                r.Status,
                r.AllocationTargetDepartmentId,
                r.CreateDate,
                r.UserId,
                ReceiptConfirmedAt = o != null ? o.ConfirmedAt : null,
                ReceiptConfirmedByUserId = o != null ? o.ConfirmedByUserId : null
            }
        ).ToListAsync();

        var deptIds = raw
            .Select(x => x.AllocationTargetDepartmentId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var deptNames = await _db.Departments.AsNoTracking()
            .Where(d => deptIds.Contains(d.DepartmentId))
            .ToDictionaryAsync(d => d.DepartmentId, d => d.Name);

        var nameUserIds = raw
            .Select(x => x.UserId)
            .Concat(raw.Where(x => x.ReceiptConfirmedByUserId.HasValue).Select(x => x.ReceiptConfirmedByUserId!.Value))
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var displayNames = await AllocationReporting.ResolveUserDisplayNamesAsync(_db, nameUserIds);

        var rows = raw.Select(r => new AllocationRequestListItemDto
        {
            AssetRequestId = r.AssetRequestId,
            AssetAllocationOrderId = r.OrderId,
            Title = r.Title,
            Status = r.Status,
            DepartmentId = r.AllocationTargetDepartmentId ?? 0,
            DepartmentName = r.AllocationTargetDepartmentId.HasValue &&
                               deptNames.TryGetValue(r.AllocationTargetDepartmentId.Value, out var n)
                ? n
                : "",
            CreateDate = r.CreateDate,
            RequestedByUserId = r.UserId,
            RequestedByName = displayNames.TryGetValue(r.UserId, out var rn) ? rn : $"User #{r.UserId}",
            ReceiptConfirmedAt = r.ReceiptConfirmedAt,
            ReceiptConfirmedByUserId = r.ReceiptConfirmedByUserId,
            ReceiptConfirmedByName = r.ReceiptConfirmedByUserId.HasValue &&
                                     displayNames.TryGetValue(r.ReceiptConfirmedByUserId.Value, out var cn)
                ? cn
                : null
        }).ToList();

        return Ok(rows);
    }

    [HttpGet("orders/{orderId:int}")]
    public async Task<ActionResult<AllocationOrderDetailDto>> GetOrder(int orderId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var actorUserId) || actorUserId <= 0)
            return Unauthorized();

        var isAccountant = await _db.UserRoles.AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == actorUserId &&
                (ur.Role != null && ur.Role.Code != null && ur.Role.Code.ToUpper() == "ACCOUNTANT"));

        var order = await _db.AssetAllocationOrders
            .AsNoTracking()
            .Include(o => o.Lines)
            .ThenInclude(l => l.AssetType)
            .Include(o => o.Lines)
            .ThenInclude(l => l.Asset)
            .Include(o => o.AssetRequest)
            .Include(o => o.Department)
            .FirstOrDefaultAsync(o => o.AssetAllocationOrderId == orderId);

        if (order == null)
            return NotFound();

        if (order.AssetRequest?.RequestTypeId != _allocationRequestTypeId)
            return NotFound();

        if (!isAccountant)
        {
            var deptId = await _db.Employees.AsNoTracking()
                .Where(e => e.UserId == actorUserId)
                .Select(e => (int?)e.DepartmentId)
                .FirstOrDefaultAsync();
            if (deptId != order.DepartmentId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền xem đơn này." });
        }

        var detailNameIds = new List<int> { order.RequestedByUserId };
        if (order.ConfirmedByUserId.HasValue)
            detailNameIds.Add(order.ConfirmedByUserId.Value);
        var detailNames = await AllocationReporting.ResolveUserDisplayNamesAsync(_db, detailNameIds);

        var dto = new AllocationOrderDetailDto
        {
            AssetAllocationOrderId = order.AssetAllocationOrderId,
            AssetRequestId = order.AssetRequestId,
            OrderKind = order.Kind == AssetAllocationOrderKind.ReturnToWarehouse ? "return" : "allocation",
            Title = order.AssetRequest?.Title ?? "",
            DepartmentId = order.DepartmentId,
            DepartmentName = order.Department?.Name ?? "",
            OrderStatus = order.Status == AssetAllocationOrderStatus.Confirmed ? "confirmed" : "awaiting_confirm",
            RequestStatus = order.AssetRequest?.Status ?? 0,
            RequestedByUserId = order.RequestedByUserId,
            RequestedByName = detailNames.TryGetValue(order.RequestedByUserId, out var reqN) ? reqN : $"User #{order.RequestedByUserId}",
            RequestSubmittedAt = order.RequestSubmittedAt,
            CreatedAt = order.CreatedAt,
            ConfirmedAt = order.ConfirmedAt,
            ConfirmedByUserId = order.ConfirmedByUserId,
            ConfirmedByName = order.ConfirmedByUserId.HasValue &&
                                detailNames.TryGetValue(order.ConfirmedByUserId.Value, out var confN)
                ? confN
                : null,
            Lines = order.Lines
                .OrderBy(l => l.AssetAllocationOrderLineId)
                .Select(l => new AllocationOrderLineDetailDto
                {
                    AssetTypeId = l.AssetTypeId,
                    AssetTypeName = l.AssetType?.Name ?? "",
                    AssetId = l.AssetId,
                    AssetCode = l.Asset?.Code ?? "",
                    AssetName = l.Asset?.Name ?? "",
                    Quantity = l.Quantity,
                    Reason = l.Reason
                })
                .ToList()
        };

        return Ok(dto);
    }

    [HttpPost("orders/{orderId:int}/confirm")]
    public async Task<IActionResult> ConfirmOrder(int orderId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var actorUserId) || actorUserId <= 0)
            return Unauthorized();

        var kind = await _db.AssetAllocationOrders.AsNoTracking()
            .Where(o => o.AssetAllocationOrderId == orderId)
            .Select(o => (AssetAllocationOrderKind?)o.Kind)
            .FirstOrDefaultAsync();
        if (kind == null)
            return NotFound();
        if (kind != AssetAllocationOrderKind.Allocation)
            return BadRequest(new { message = "Đơn không phải cấp phát từ kho." });

        var err = await AllocationOrderWorkflow.ConfirmOrderAsync(_db, orderId, actorUserId, _departmentHeadRoleId);
        if (err != null)
            return BadRequest(new { message = err });

        await _db.SaveChangesAsync();
        return Ok(new { assetAllocationOrderId = orderId, status = "confirmed" });
    }

    /// <summary>Accountant: all cấp phát orders (đơn đã tạo sau duyệt).</summary>
    [HttpGet("orders-summary")]
    public async Task<ActionResult<IEnumerable<AllocationOrderSummaryDto>>> ListAllocationOrdersSummary()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var actorUserId) || actorUserId <= 0)
            return Unauthorized();

        var isAccountant = await _db.UserRoles.AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == actorUserId &&
                (ur.Role != null && ur.Role.Code != null && ur.Role.Code.ToUpper() == "ACCOUNTANT"));
        if (!isAccountant)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Chỉ kế toán xem danh sách này." });

        var rows = await (
            from o in _db.AssetAllocationOrders.AsNoTracking()
            join r in _db.AssetRequests.AsNoTracking() on o.AssetRequestId equals r.AssetRequestId
            join d in _db.Departments.AsNoTracking() on o.DepartmentId equals d.DepartmentId
            where o.Kind == AssetAllocationOrderKind.Allocation
            orderby o.CreatedAt descending
            select new AllocationOrderSummaryDto
            {
                AssetAllocationOrderId = o.AssetAllocationOrderId,
                AssetRequestId = o.AssetRequestId,
                Title = r.Title,
                DepartmentName = d.Name,
                OrderStatus = o.Status == AssetAllocationOrderStatus.Confirmed ? "confirmed" : "awaiting_confirm",
                RequestStatus = r.Status,
                CreatedAt = o.CreatedAt,
                ConfirmedAt = o.ConfirmedAt
            }
        ).ToListAsync();

        return Ok(rows);
    }
}
