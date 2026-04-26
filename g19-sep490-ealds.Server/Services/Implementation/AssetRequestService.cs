using g19_sep490_ealds.Server.DTOs.AssetRequests;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetRequestService : IAssetRequestService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<AssetRequestService> _logger;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _purchaseRequestTypeId;

    public AssetRequestService(
        EaldsDbContext context,
        ILogger<AssetRequestService> logger,
        IAssetRequestNotificationService requestNotifications,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _requestNotifications = requestNotifications;
        _purchaseRequestTypeId = configuration.GetValue<int>("App:PurchaseRequestTypeId", 1);
    }

    public async Task<List<AssetRequestListItemDTO>> GetPurchaseListAsync(int requestTypeId)
    {
        return await _context.AssetRequests
            .AsNoTracking()
            .Include(r => r.Asset)
            .Include(r => r.User)
            .Where(r => r.RequestTypeId == requestTypeId)
            .OrderByDescending(r => r.CreateDate)
            .Select(r => new AssetRequestListItemDTO
            {
                AssetRequestId = r.AssetRequestId,
                AssetId = r.AssetId,
                Title = r.Title,
                Description = r.Description,
                ProposedData = r.ProposedData,
                Status = r.Status,
                CreateDate = r.CreateDate,
                UserId = r.UserId,
                CreatedBy = r.CreatedBy,
                CreatorName = r.User != null
                    ? r.User.EmployeeUsers
                        .Select(e => e.Name)
                        .FirstOrDefault() ?? r.User.Email
                    : null,
                CreatorDepartmentName = r.User != null
                    ? r.User.EmployeeUsers.Select(e => e.Department != null ? e.Department.Name : null).FirstOrDefault()
                    : null,
                AssetCode = r.Asset != null ? r.Asset.Code : null,
                AssetName = r.Asset != null ? r.Asset.Name : null,
                AssetQuantity = r.Asset != null ? (int?)r.Asset.Quantity : null,
            })
            .ToListAsync();
    }

    public async Task<AssetRequestDetailDTO> GetPurchaseByIdAsync(int id)
    {
        var request = await _context.AssetRequests
            .AsNoTracking()
            .Where(r => r.AssetRequestId == id && r.RequestTypeId == _purchaseRequestTypeId)
            .Select(r => new AssetRequestDetailDTO
            {
                AssetRequestId = r.AssetRequestId,
                AssetId = r.AssetId,
                Title = r.Title,
                Description = r.Description,
                ProposedData = r.ProposedData,
                Status = r.Status,
                CreateDate = r.CreateDate,
                UserId = r.UserId,
                CreatedBy = r.CreatedBy,
                CreatorName = r.User != null
                    ? r.User.EmployeeUsers
                        .Select(e => e.Name)
                        .FirstOrDefault() ?? r.User.Email
                    : null,
                CreatorDepartmentName = r.User != null
                    ? r.User.EmployeeUsers.Select(e => e.Department != null ? e.Department.Name : null).FirstOrDefault()
                    : null,
                AssetCode = r.Asset != null ? r.Asset.Code : null,
                AssetName = r.Asset != null ? r.Asset.Name : null,
                AccountantComment = r.Approvals
                    .Where(a => a.ApprovedRole != null && a.ApprovedRole.Code == "ACCOUNTANT")
                    .OrderByDescending(a => a.DecisionDate)
                    .Select(a => a.Comment)
                    .FirstOrDefault(),
                DirectorComment = r.Approvals
                    .Where(a => a.ApprovedRole != null && a.ApprovedRole.Code == "DIRECTOR")
                    .OrderByDescending(a => a.DecisionDate)
                    .Select(a => a.Comment)
                    .FirstOrDefault(),
                Approvals = r.Approvals
                    .OrderByDescending(a => a.DecisionDate)
                    .Select(a => new AssetRequestApprovalItemDTO
                    {
                        ApprovalId = a.ApprovalId,
                        DecisionDate = a.DecisionDate,
                        Comment = a.Comment,
                        RoleCode = a.ApprovedRole != null ? a.ApprovedRole.Code : null,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync();

        if (request == null)
            throw new KeyNotFoundException($"Purchase request {id} not found.");

        return request;
    }

    public async Task<List<AssetRequestPurchaseLineResponseDTO>> GetPurchaseLinesAsync(int id)
    {
        var ar = await _context.AssetRequests
            .FirstOrDefaultAsync(r => r.AssetRequestId == id && r.RequestTypeId == _purchaseRequestTypeId);
        if (ar == null)
            throw new KeyNotFoundException($"Purchase request {id} not found.");

        await PurchaseRequestLineHelper.EnsureLinesAsync(_context, ar);

        return await _context.AssetRequestPurchaseLines
            .AsNoTracking()
            .Where(l => l.AssetRequestId == id)
            .OrderBy(l => l.LineIndex)
            .Select(l => new AssetRequestPurchaseLineResponseDTO
            {
                LineId = l.LineId,
                LineIndex = l.LineIndex,
                ItemName = l.ItemName,
                Quantity = l.Quantity,
                Unit = l.Unit,
                ModelCode = l.ModelCode,
                EstimatedPrice = l.EstimatedPrice,
                AssetId = l.AssetId,
                CapitalizedAt = l.CapitalizedAt,
                AssetCode = l.Asset != null ? l.Asset.Code : null,
                AssetName = l.Asset != null ? l.Asset.Name : null,
            })
            .ToListAsync();
    }

    public async Task<int> CreateAsync(AssetRequestDTO dto)
    {
        if (dto == null)
            throw new InvalidOperationException("Request body is required.");
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Title is required.");

        var desiredStatus = dto.Status ?? 0;
        if (desiredStatus != 0 && desiredStatus != -1)
            throw new InvalidOperationException("Invalid status. Allowed: -1 (Draft), 0 (Submitted).");

        var requestTypeExists = await _context.RequestTypes
            .AsNoTracking()
            .AnyAsync(rt => rt.RequestTypeId == _purchaseRequestTypeId);
        if (!requestTypeExists)
            throw new InvalidOperationException($"Configured purchase RequestTypeId '{_purchaseRequestTypeId}' does not exist in RequestType table.");

        var initialStepId = await _context.RequestTypes
            .AsNoTracking()
            .Where(rt => rt.RequestTypeId == _purchaseRequestTypeId)
            .SelectMany(rt => _context.WorkflowSteps.Where(ws => ws.WorkflowId == rt.WorkflowId))
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .FirstOrDefaultAsync();
        if (!initialStepId.HasValue)
            throw new InvalidOperationException($"No workflow step configured for RequestTypeId '{_purchaseRequestTypeId}'.");

        var assetRequest = new AssetRequest
        {
            UserId = dto.UserId,
            RequestTypeId = _purchaseRequestTypeId,
            AssetId = dto.AssetId,
            AssetInstanceId = dto.AssetInstanceId,
            Title = dto.Title,
            Description = dto.Description,
            ProposedData = dto.ProposedData,
            Status = desiredStatus,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = initialStepId.Value,
        };

        _context.AssetRequests.Add(assetRequest);
        await _context.SaveChangesAsync();

        var userRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = assetRequest.Status,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = assetRequest.Status == -1 ? "Created draft request" : "Created request",
            OccurredAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        if (desiredStatus == 0)
            await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);

        return assetRequest.AssetRequestId;
    }

    public async Task<int> UpdateAsync(int id, AssetRequestDTO dto)
    {
        if (dto == null)
            throw new InvalidOperationException("Request body is required.");
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Title is required.");

        var desiredStatus = dto.Status ?? -1;

        var ar = await _context.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == id && x.RequestTypeId == _purchaseRequestTypeId);
        if (ar == null)
            throw new KeyNotFoundException($"Purchase request {id} not found.");

        if (ar.Status != -1)
            throw new InvalidOperationException("Only draft purchase requests (status=-1) can be edited.");
        if (desiredStatus != -1 && desiredStatus != 0)
            throw new InvalidOperationException("Invalid status. Allowed: -1 (Draft), 0 (Sent).");

        var fromStatus = ar.Status;

        ar.Title = dto.Title;
        ar.Description = dto.Description;
        ar.ProposedData = dto.ProposedData;
        ar.AssetId = dto.AssetId;
        ar.UserId = dto.UserId;
        ar.CreatedBy = dto.CreatedBy;
        ar.Status = desiredStatus;

        var userRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = ar.Status,
            Action = (fromStatus == -1 && desiredStatus == 0) ? 1 : 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = (fromStatus == -1 && desiredStatus == 0) ? "Submitted request" : "Updated draft request",
            OccurredAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();

        if (fromStatus == -1 && desiredStatus == 0)
            await _requestNotifications.NotifyFirstApproversAsync(ar.AssetRequestId);

        return ar.AssetRequestId;
    }

    public async Task<AssetRequestFullDetailDTO> GetDetailsAsync(int id)
    {
        var ar = await _context.AssetRequests
            .Include(x => x.Asset)
            .Include(x => x.User)
            .Include(x => x.RequestType)
            .Include(x => x.Approvals)
            .Include(x => x.AssetRequestRecords)
            .Include(x => x.MaintenanceTasks)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(x => x.RepairTasks)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(x => x.RepairTasks)
                .ThenInclude(t => t.Supplier)
            .Include(x => x.TransferRecords)
                .ThenInclude(tr => tr.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(x => x.Procurements)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetRequestId == id);

        if (ar == null)
            throw new KeyNotFoundException($"Asset request {id} not found.");

        return new AssetRequestFullDetailDTO
        {
            Id = ar.AssetRequestId,
            Title = ar.Title,
            Description = ar.Description,
            ProposedData = ar.ProposedData,
            Status = ar.Status,
            CreateDate = ar.CreateDate,
            ApproveDate = ar.ApproveDate,
            StepId = ar.StepId,
            RequestTypeId = ar.RequestTypeId,
            User = ar.User == null ? null : new AssetRequestUserDTO { UserId = ar.User.UserId, Email = ar.User.Email },
            RequestType = ar.RequestType == null ? null : new AssetRequestTypeRefDTO { RequestTypeId = ar.RequestType.RequestTypeId, WorkflowId = ar.RequestType.WorkflowId },
            Asset = ar.Asset == null ? null : new AssetRequestAssetRefDTO { AssetId = ar.Asset.AssetId, Name = ar.Asset.Name, Code = ar.Asset.Code, Quantity = ar.Asset.Quantity },
            Approvals = ar.Approvals.Select(a => new AssetRequestFullApprovalDTO
            {
                ApprovalId = a.ApprovalId,
                DecisionDate = a.DecisionDate,
                ApprovedUserId = a.ApprovedUserId,
                ApprovedRoleId = a.ApprovedRoleId,
                StepId = a.StepId,
                Decision = a.Decision,
                Comment = a.Comment,
                RoleCode = a.ApprovedRole != null ? a.ApprovedRole.Code : null,
                RoleName = a.ApprovedRole != null ? a.ApprovedRole.Name : null,
            }).ToList(),
            Records = ar.AssetRequestRecords.Select(r => new AssetRequestRecordDTO
            {
                RecordId = r.RecordId,
                FromStatus = r.FromStatus,
                ToStatus = r.ToStatus,
                Action = r.Action,
                ActionByUserId = r.ActionByUserId,
                ActionRoleId = r.ActionRoleId,
                Comment = r.Comment,
                OccurredAt = r.OccurredAt,
            }).ToList(),
            MaintenanceTasks = ar.MaintenanceTasks.Select(t => new AssetRequestMaintenanceTaskDTO
            {
                TaskId = t.TaskId,
                PlannedDate = t.PlannedDate,
                Status = t.Status,
                AssignTo = t.AssignTo,
                AssetInstanceId = t.AssetInstanceId,
                InstanceCode = t.AssetInstance != null ? t.AssetInstance.InstanceCode : null,
                AssetName = t.AssetInstance != null && t.AssetInstance.Asset != null ? t.AssetInstance.Asset.Name : null,
            }).ToList(),
            RepairTasks = ar.RepairTasks.Select(t => new AssetRequestRepairTaskDTO
            {
                TaskId = t.TaskId,
                EstimatedCost = t.EstimatedCost,
                DamageCondition = t.Reason,
                Status = t.Status,
                RepairDate = t.RepairDate,
                AssetInstanceId = t.AssetInstanceId,
                SupplierId = t.SupplierId,
                SupplierName = t.Supplier != null ? t.Supplier.Name : null,
                InstanceCode = t.AssetInstance != null ? t.AssetInstance.InstanceCode : null,
                AssetName = t.AssetInstance != null && t.AssetInstance.Asset != null ? t.AssetInstance.Asset.Name : null,
            }).ToList(),
            TransferRecords = ar.TransferRecords.Select(tr => new AssetRequestTransferRecordDTO
            {
                TransferId = tr.TransferId,
                AssetRequestId = tr.AssetRequestId,
                AssetInstanceId = tr.AssetInstanceId,
                FromLocationId = tr.FromLocationId,
                ToLocationId = tr.ToLocationId,
                TransferDate = tr.TransferDate,
                InstanceCode = tr.AssetInstance != null ? tr.AssetInstance.InstanceCode : null,
                AssetName = tr.AssetInstance != null && tr.AssetInstance.Asset != null ? tr.AssetInstance.Asset.Name : null,
            }).ToList(),
            Procurements = ar.Procurements.Select(p => new AssetRequestProcurementRefDTO { ProcurementId = p.ProcurementId }).ToList(),
        };
    }

    public async Task<AssetRequestPagedResultDTO> ListAsync(int? status, int? requestTypeId, int? userId, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _context.AssetRequests
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (requestTypeId.HasValue)
            query = query.Where(x => x.RequestTypeId == requestTypeId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreateDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ar => new AssetRequestListResultItemDTO
            {
                Id = ar.AssetRequestId,
                Title = ar.Title,
                Description = ar.Description,
                Status = ar.Status,
                CreateDate = ar.CreateDate,
                UserId = ar.UserId,
                UserEmail = ar.User != null ? ar.User.Email : null,
                AssetId = ar.AssetId,
                AssetInstanceId = ar.AssetInstanceId,
                AssetCode = ar.Asset != null ? ar.Asset.Code : null,
                AssetInstanceCode = ar.AssetInstance != null ? ar.AssetInstance.InstanceCode : null,
                AssetName = ar.Asset != null ? ar.Asset.Name : null,
                AssetQuantity = ar.Asset != null ? (int?)ar.Asset.Quantity : null,
                CurrentDepartmentName = ar.AssetInstance != null
                    ? ar.AssetInstance.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Department != null ? al.Department.Name : null)
                        .FirstOrDefault()
                    : null,
                CurrentLocation = ar.AssetInstance != null
                    ? ar.AssetInstance.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Note)
                        .FirstOrDefault() ?? (ar.AssetInstance.Warehouse != null ? ar.AssetInstance.Warehouse.Name : null)
                    : null,
                RequestTypeId = ar.RequestTypeId,
            })
            .ToListAsync();

        return new AssetRequestPagedResultDTO
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        };
    }

    public async Task RevertToDraftAsync(int id, int userId)
    {
        if (userId <= 0)
            throw new InvalidOperationException("UserId is required.");

        var request = await _context.AssetRequests
            .Where(r => r.AssetRequestId == id && r.RequestTypeId == _purchaseRequestTypeId)
            .FirstOrDefaultAsync();

        if (request == null)
            throw new KeyNotFoundException($"Purchase request with id {id} not found.");

        if (request.Status != 0)
            throw new InvalidOperationException("Only submitted requests (status=0) can be reverted to draft.");

        if (request.CreatedBy != userId)
            throw new UnauthorizedAccessException();

        request.Status = -1;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDraftAsync(int id, int userId)
    {
        if (userId <= 0)
            throw new InvalidOperationException("UserId is required.");

        var ar = await _context.AssetRequests
            .FirstOrDefaultAsync(r => r.AssetRequestId == id && r.RequestTypeId == _purchaseRequestTypeId);
        if (ar == null)
            throw new KeyNotFoundException($"Purchase request with id {id} not found.");

        if (ar.Status != -1)
            throw new InvalidOperationException("Chỉ được xóa yêu cầu ở trạng thái Nháp.");

        if (ar.CreatedBy != userId)
            throw new UnauthorizedAccessException();

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var approvals = await _context.Approvals.Where(x => x.AssetRequestId == id).ToListAsync();
            var records = await _context.AssetRequestRecords.Where(x => x.AssetRequestId == id).ToListAsync();
            var purchaseLines = await _context.AssetRequestPurchaseLines.Where(x => x.AssetRequestId == id).ToListAsync();

            var procurements = await _context.Procurements.Where(p => p.AssetRequestId == id).ToListAsync();
            foreach (var proc in procurements)
            {
                var pLines = await _context.ProcurementLines.Where(l => l.ProcurementId == proc.ProcurementId).ToListAsync();
                if (pLines.Count > 0)
                    _context.ProcurementLines.RemoveRange(pLines);
            }
            if (procurements.Count > 0)
                _context.Procurements.RemoveRange(procurements);

            if (approvals.Count > 0)
                _context.Approvals.RemoveRange(approvals);
            if (records.Count > 0)
                _context.AssetRequestRecords.RemoveRange(records);
            if (purchaseLines.Count > 0)
                _context.AssetRequestPurchaseLines.RemoveRange(purchaseLines);

            _context.AssetRequests.Remove(ar);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
