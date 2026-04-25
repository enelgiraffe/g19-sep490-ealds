using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.Allocation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class HandoverRequestService : IHandoverRequestService
{
    private readonly EaldsDbContext _context;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly ILogger<HandoverRequestService> _logger;
    private readonly int _handoverRequestTypeId;
    private readonly int _departmentHeadRoleId;

    public HandoverRequestService(
        EaldsDbContext context,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications,
        ILogger<HandoverRequestService> logger)
    {
        _context = context;
        _requestNotifications = requestNotifications;
        _logger = logger;
        _handoverRequestTypeId = configuration.GetValue<int>("App:HandoverRequestTypeId", 7);
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
    }

    public async Task<int> GetDepartmentAssignedAsync(int userId, int assetId)
    {
        if (assetId <= 0)
            throw new InvalidOperationException("assetId is required.");
        if (!await _context.Assets.AsNoTracking().AnyAsync(a => a.AssetId == assetId))
            throw new KeyNotFoundException("Asset not found.");

        var departmentId = await _context.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.EmployeeId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();
        if (!departmentId.HasValue)
            throw new InvalidOperationException("Tài khoản chưa gắn phòng ban.");

        return await AllocationOrderWorkflow.GetDepartmentAssignedCountAsync(_context, assetId, departmentId.Value);
    }

    public async Task<int> CreateAsync(int userId, CreateDepartmentAllocationRequestDto dto)
    {
        if (dto?.Lines == null || dto.Lines.Count == 0)
            throw new InvalidOperationException("Cần ít nhất một dòng tài sản.");
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Nhập tiêu đề yêu cầu.");

        for (var i = 0; i < dto.Lines.Count; i++)
        {
            var line = dto.Lines[i];
            if (line.AssetTypeId <= 0 || line.AssetId <= 0 || line.Quantity < 1)
                throw new InvalidOperationException($"Dòng {i + 1}: chọn loại, tài sản và số lượng hợp lệ.");
        }

        var inHeadRole = await _context.UserRoles.AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == _departmentHeadRoleId);
        if (!inHeadRole)
            throw new UnauthorizedAccessException("Chỉ trưởng phòng ban được gửi yêu cầu hoàn trả.");

        var departmentId = await _context.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.EmployeeId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();
        if (!departmentId.HasValue)
            throw new InvalidOperationException("Tài khoản chưa gắn phòng ban (nhân viên).");

        if (await DepartmentAssetScope.DepartmentHasInventoryInProgressAsync(_context, departmentId.Value))
            throw new InvalidOperationException(DepartmentAssetScope.InventoryInProgressBlockingMessage);

        if (!await _context.RequestTypes.AsNoTracking().AnyAsync(rt => rt.RequestTypeId == _handoverRequestTypeId))
            throw new InvalidOperationException(
                $"RequestType {_handoverRequestTypeId} chưa có trong bảng RequestType.");

        var workflowId = await _context.RequestTypes.AsNoTracking()
            .Where(rt => rt.RequestTypeId == _handoverRequestTypeId)
            .Select(rt => (int?)rt.WorkflowId)
            .FirstOrDefaultAsync();
        if (!workflowId.HasValue || workflowId.Value <= 0)
            throw new InvalidOperationException("Loại yêu cầu hoàn trả chưa gắn WorkflowId hợp lệ.");

        var initialStepId = await _context.WorkflowSteps.AsNoTracking()
            .Where(ws => ws.WorkflowId == workflowId.Value)
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            throw new InvalidOperationException("Chưa cấu hình bước workflow cho loại yêu cầu hoàn trả.");

        var proposedJson = AllocationOrderWorkflow.BuildProposedDataJson(departmentId.Value, dto.Lines);
        if (!AllocationOrderWorkflow.TryParseProposedData(proposedJson, out var root, out var parseErr) || root == null)
            throw new InvalidOperationException(parseErr);

        var validation = await AllocationOrderWorkflow.ValidateReturnLinesAgainstDatabaseAsync(_context, root);
        if (validation != null)
            throw new InvalidOperationException(validation);

        var assetRequest = new AssetRequest
        {
            UserId = userId,
            RequestTypeId = _handoverRequestTypeId,
            AssetId = null,
            AssetInstanceId = null,
            Title = dto.Title!.Trim(),
            Description = "Yêu cầu hoàn trả tài sản từ phòng ban về kho",
            ProposedData = proposedJson,
            Status = AllocationOrderWorkflow.RequestStatusPendingAccountant,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value,
            AllocationTargetDepartmentId = departmentId.Value
        };

        _context.AssetRequests.Add(assetRequest);
        await _context.SaveChangesAsync();

        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = userId,
            ActionRoleId = _departmentHeadRoleId,
            Comment = "Gửi yêu cầu hoàn trả về kho",
            OccurredAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return assetRequest.AssetRequestId;
    }

    public async Task<IEnumerable<AllocationRequestListItemDto>> GetListAsync(int userId)
    {
        var isAccountant = await _context.UserRoles.AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == userId &&
                ur.Role != null && ur.Role.Code != null && ur.Role.Code.ToUpper() == "ACCOUNTANT");

        int? filterDeptId = null;
        if (!isAccountant)
        {
            filterDeptId = await _context.Employees.AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => (int?)e.DepartmentId)
                .FirstOrDefaultAsync();
            if (!filterDeptId.HasValue)
                return Array.Empty<AllocationRequestListItemDto>();
        }

        var baseRequests = _context.AssetRequests.AsNoTracking()
            .Where(r => r.RequestTypeId == _handoverRequestTypeId);
        if (!isAccountant)
            baseRequests = baseRequests.Where(r => r.AllocationTargetDepartmentId == filterDeptId!.Value);

        var raw = await (
            from r in baseRequests
            join o in _context.AssetAllocationOrders.AsNoTracking() on r.AssetRequestId equals o.AssetRequestId into og
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
        var deptNames = await _context.Departments.AsNoTracking()
            .Where(d => deptIds.Contains(d.DepartmentId))
            .ToDictionaryAsync(d => d.DepartmentId, d => d.Name);

        var nameUserIds = raw
            .Select(x => x.UserId)
            .Concat(raw.Where(x => x.ReceiptConfirmedByUserId.HasValue).Select(x => x.ReceiptConfirmedByUserId!.Value))
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var displayNames = await AllocationReporting.ResolveUserDisplayNamesAsync(_context, nameUserIds);

        return raw.Select(r => new AllocationRequestListItemDto
        {
            AssetRequestId = r.AssetRequestId,
            AssetAllocationOrderId = r.OrderId,
            Title = r.Title,
            Status = r.Status,
            DepartmentId = r.AllocationTargetDepartmentId ?? 0,
            DepartmentName = r.AllocationTargetDepartmentId.HasValue &&
                             deptNames.TryGetValue(r.AllocationTargetDepartmentId.Value, out var n) ? n : "",
            CreateDate = r.CreateDate,
            RequestedByUserId = r.UserId,
            RequestedByName = displayNames.TryGetValue(r.UserId, out var rn) ? rn : $"User #{r.UserId}",
            ReceiptConfirmedAt = r.ReceiptConfirmedAt,
            ReceiptConfirmedByUserId = r.ReceiptConfirmedByUserId,
            ReceiptConfirmedByName = r.ReceiptConfirmedByUserId.HasValue &&
                                     displayNames.TryGetValue(r.ReceiptConfirmedByUserId.Value, out var cn) ? cn : null
        }).ToList();
    }

    public async Task<AllocationOrderDetailDto> GetOrderAsync(int userId, int orderId)
    {
        var isAccountant = await _context.UserRoles.AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == userId &&
                ur.Role != null && ur.Role.Code != null && ur.Role.Code.ToUpper() == "ACCOUNTANT");

        var order = await _context.AssetAllocationOrders
            .AsNoTracking()
            .Include(o => o.Lines).ThenInclude(l => l.AssetType)
            .Include(o => o.Lines).ThenInclude(l => l.Asset)
            .Include(o => o.AssetRequest)
            .Include(o => o.Department)
            .FirstOrDefaultAsync(o => o.AssetAllocationOrderId == orderId);

        if (order == null)
            throw new KeyNotFoundException("Order not found.");
        if (order.AssetRequest?.RequestTypeId != _handoverRequestTypeId)
            throw new KeyNotFoundException("Order not found.");

        if (!isAccountant)
        {
            var deptId = await _context.Employees.AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => (int?)e.DepartmentId)
                .FirstOrDefaultAsync();
            if (deptId != order.DepartmentId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem đơn này.");
        }

        var detailNameIds = new List<int> { order.RequestedByUserId };
        if (order.ConfirmedByUserId.HasValue) detailNameIds.Add(order.ConfirmedByUserId.Value);
        var detailNames = await AllocationReporting.ResolveUserDisplayNamesAsync(_context, detailNameIds);
        var accountantComment = await AllocationReporting.GetAccountantApprovalCommentAsync(_context, order.AssetRequestId);

        return new AllocationOrderDetailDto
        {
            AssetAllocationOrderId = order.AssetAllocationOrderId,
            AssetRequestId = order.AssetRequestId,
            OrderKind = "return",
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
                              detailNames.TryGetValue(order.ConfirmedByUserId.Value, out var confN) ? confN : null,
            AccountantComment = string.IsNullOrWhiteSpace(accountantComment) ? null : accountantComment.Trim(),
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
    }

    public async Task ConfirmOrderAsync(int userId, int orderId)
    {
        var kind = await _context.AssetAllocationOrders.AsNoTracking()
            .Where(o => o.AssetAllocationOrderId == orderId)
            .Select(o => (AssetAllocationOrderKind?)o.Kind)
            .FirstOrDefaultAsync();
        if (kind == null)
            throw new KeyNotFoundException("Order not found.");
        if (kind != AssetAllocationOrderKind.ReturnToWarehouse)
            throw new InvalidOperationException("Đơn không phải hoàn trả về kho.");

        var err = await AllocationOrderWorkflow.ConfirmOrderAsync(_context, orderId, userId, _departmentHeadRoleId);
        if (err != null)
            throw new InvalidOperationException(err);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AllocationOrderSummaryDto>> GetOrdersSummaryAsync(int userId)
    {
        var isAccountant = await _context.UserRoles.AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == userId &&
                ur.Role != null && ur.Role.Code != null && ur.Role.Code.ToUpper() == "ACCOUNTANT");
        if (!isAccountant)
            throw new UnauthorizedAccessException("Chỉ kế toán xem danh sách này.");

        return await (
            from o in _context.AssetAllocationOrders.AsNoTracking()
            join r in _context.AssetRequests.AsNoTracking() on o.AssetRequestId equals r.AssetRequestId
            join d in _context.Departments.AsNoTracking() on o.DepartmentId equals d.DepartmentId
            where o.Kind == AssetAllocationOrderKind.ReturnToWarehouse
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
    }
}
