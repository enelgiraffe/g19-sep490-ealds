using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class DisposalRequestService : IDisposalRequestService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<DisposalRequestService> _logger;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _disposalRequestTypeId;
    private readonly int _departmentHeadRoleId;

    public DisposalRequestService(
        EaldsDbContext context,
        ILogger<DisposalRequestService> logger,
        IAssetRequestNotificationService requestNotifications,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _requestNotifications = requestNotifications;
        _disposalRequestTypeId = configuration.GetValue<int>("App:DisposalRequestTypeId", 5);
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
    }

    public async Task<IEnumerable<TransferRequestListItemDTO>> GetListAsync()
    {
        return await _context.DisposalRecords
            .AsNoTracking()
            .Include(d => d.AssetInstance)
                .ThenInclude(ai => ai.Asset)
                .ThenInclude(a => a.AssetType)
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
                AssetTypeName = d.AssetInstance.Asset != null && d.AssetInstance.Asset.AssetType != null
                    ? d.AssetInstance.Asset.AssetType.Name
                    : null,
                AssetInstanceId = d.AssetInstanceId,
                InstanceCode = d.AssetInstance.InstanceCode,
                OriginalPrice = d.AssetInstance.OriginalPrice,
                CurrentValue = d.AssetInstance.CurrentValue,
                DisposalDeclaredValue = d.DiposalValue,
                FromDepartment = d.AssetInstance.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Department != null ? al.Department.Name : string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                ToDepartment = string.Empty,
                Quantity = 1,
                Status = d.AssetRequest!.Status,
                StatusName =
                    d.AssetRequest.Status == 0 ? "Đã gửi" :
                    d.AssetRequest.Status == 1 ? "Chờ duyệt giám đốc" :
                    d.AssetRequest.Status == 2 ? "Đã duyệt" :
                    d.AssetRequest.Status == 3 ? "Từ chối" :
                    d.AssetRequest.Status == 4 ? "Đã thẩm định" :
                    d.AssetRequest.Status == 5 ? "Đã thanh lý" :
                    "Không xác định",
                Reason = d.Reason,
                FromDepartmentId = d.AssetInstance.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.DepartmentId)
                    .FirstOrDefault(),
                ToDepartmentId = 0,
                CreatedBy = d.AssetRequest.CreatedBy,
                CreatedByName = _context.Employees
                    .Where(e => e.UserId == d.AssetRequest.CreatedBy)
                    .Select(e => e.Name)
                    .FirstOrDefault(),
                IsSenderConfirmed = false,
                IsReceiverConfirmed = false
            })
            .ToListAsync();
    }

    public async Task<(int assetRequestId, int diposalId)> CreateAsync(int userId, AssetDisposalRequestDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Title is required.");

        if (!dto.AssetInstanceId.HasValue || dto.AssetInstanceId.Value <= 0)
            throw new InvalidOperationException("AssetInstanceId is required.");

        var instance = await _context.AssetInstances
            .Include(ai => ai.Asset)
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == dto.AssetInstanceId.Value)
            ?? throw new KeyNotFoundException($"Asset instance {dto.AssetInstanceId.Value} not found.");

        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        if (!userRoles.Any(ur => ur.RoleId == _departmentHeadRoleId))
            throw new UnauthorizedAccessException("Bạn không có quyền gửi yêu cầu thanh lý (chỉ trưởng phòng ban).");

        var resolvedRequestTypeId = dto.RequestTypeId ?? _disposalRequestTypeId;

        if (!await _context.RequestTypes.AsNoTracking().AnyAsync(rt => rt.RequestTypeId == resolvedRequestTypeId))
            throw new InvalidOperationException($"RequestTypeId '{resolvedRequestTypeId}' không tồn tại trong bảng RequestType.");

        var initialStepId = await _context.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == resolvedRequestTypeId)
            .SelectMany(rt => _context.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"No workflow step configured for RequestTypeId '{resolvedRequestTypeId}'.");

        var assetRequest = new AssetRequest
        {
            UserId = userId,
            RequestTypeId = resolvedRequestTypeId,
            AssetId = instance.AssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = dto.Title,
            Description = dto.Description,
            ProposedData = null,
            Status = 0,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId
        };

        _context.AssetRequests.Add(assetRequest);
        await _context.SaveChangesAsync();

        var diposal = new DisposalRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            AssetInstanceId = dto.AssetInstanceId.Value,
            DiposalMethod = dto.DiposalMethod,
            DiposalValue = dto.DiposalValue,
            DiposalDate = DateTime.UtcNow,
            Reason = dto.Reason,
            ExecutedBy = userId
        };

        _context.DisposalRecords.Add(diposal);

        var userRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = 0,
            Action = 0,
            ActionByUserId = userId,
            ActionRoleId = userRole?.RoleId ?? 1,
            Comment = "Submitted disposal request",
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return (assetRequest.AssetRequestId, diposal.DiposalId);
    }
}
