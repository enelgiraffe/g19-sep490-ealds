using System;
using System.Linq;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class DisposalExecutionService : IDisposalExecutionService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<DisposalExecutionService> _logger;
    private readonly int _disposalRequestTypeId;

    public DisposalExecutionService(
        EaldsDbContext context,
        ILogger<DisposalExecutionService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _disposalRequestTypeId = configuration.GetValue<int>("App:DisposalRequestTypeId", 5);
    }

    public async Task<DisposalExecutionDto> GetByAssetRequestAsync(int assetRequestId)
    {
        var ar = await _context.AssetRequests.AsNoTracking().FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId)
            ?? throw new KeyNotFoundException("Asset request not found.");

        if (ar.RequestTypeId != _disposalRequestTypeId)
            throw new InvalidOperationException("Chỉ áp dụng cho yêu cầu thanh lý.");

        var exec = await _context.DisposalExecutions.AsNoTracking()
            .FirstOrDefaultAsync(e => e.AssetRequestId == assetRequestId);

        var (canFinalize, blockReason) = EvaluateCanFinalize(ar, exec);

        if (exec == null)
        {
            return new DisposalExecutionDto
            {
                DisposalExecutionId = null,
                AssetRequestId = assetRequestId,
                DisposalRecordId = await _context.DisposalRecords.AsNoTracking()
                    .Where(d => d.AssetRequestId == assetRequestId)
                    .Select(d => (int?)d.DiposalId)
                    .FirstOrDefaultAsync(),
                Status = 0,
                AssetRequestStatus = ar.Status,
                CanEdit = ar.Status == 2 || ar.Status == 4,
                CanFinalize = canFinalize,
                BlockFinalizeReason = blockReason
            };
        }

        return ToDto(exec, ar, canFinalize, blockReason);
    }

    public async Task<DisposalExecutionDto> SaveDraftAsync(int userId, int assetRequestId, SaveDisposalExecutionDto dto)
    {
        var ar = await _context.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId)
            ?? throw new KeyNotFoundException("Asset request not found.");

        if (ar.RequestTypeId != _disposalRequestTypeId)
            throw new InvalidOperationException("Chỉ áp dụng cho yêu cầu thanh lý.");

        if (ar.Status != 2 && ar.Status != 4)
            throw new InvalidOperationException("Chỉ lưu được ở bước đã duyệt (2) hoặc đã ghi nhận thẩm định (4).");

        var now = DateTime.UtcNow;
        var exec = await _context.DisposalExecutions.FirstOrDefaultAsync(e => e.AssetRequestId == assetRequestId);

        if (exec != null && exec.Status >= 2)
            throw new InvalidOperationException("Đã hoàn tất thực hiện thanh lý, không thể sửa.");

        var dip = await _context.DisposalRecords.AsNoTracking()
            .FirstOrDefaultAsync(d => d.AssetRequestId == assetRequestId);

        if (exec == null)
        {
            exec = new DisposalExecution
            {
                AssetRequestId = assetRequestId,
                DisposalRecordId = dip?.DiposalId,
                Status = 0,
                CreatedBy = userId,
                CreatedDate = now
            };
            _context.DisposalExecutions.Add(exec);
        }

        if (dto.PlannedExecutionDate.HasValue)
            exec.PlannedExecutionDate = dto.PlannedExecutionDate;
        exec.ExecutedDate = dto.ExecutedDate;
        exec.ExecutionMethod = dto.ExecutionMethod;
        exec.BuyerName = string.IsNullOrWhiteSpace(dto.BuyerName) ? null : dto.BuyerName.Trim();
        exec.BuyerContact = string.IsNullOrWhiteSpace(dto.BuyerContact) ? null : dto.BuyerContact.Trim();
        exec.ContractNo = string.IsNullOrWhiteSpace(dto.ContractNo) ? null : dto.ContractNo.Trim();
        exec.InvoiceNo = string.IsNullOrWhiteSpace(dto.InvoiceNo) ? null : dto.InvoiceNo.Trim();
        exec.MinutesNo = string.IsNullOrWhiteSpace(dto.MinutesNo) ? null : dto.MinutesNo.Trim();
        exec.ActualDisposalValue = dto.ActualDisposalValue;
        exec.ExpenseValue = dto.ExpenseValue;
        exec.AttachmentUrls = string.IsNullOrWhiteSpace(dto.AttachmentUrls) ? null : dto.AttachmentUrls.Trim();
        exec.ExecutionNote = string.IsNullOrWhiteSpace(dto.ExecutionNote) ? null : dto.ExecutionNote.Trim();
        exec.UpdatedBy = userId;
        exec.UpdatedDate = now;

        await _context.SaveChangesAsync();

        var (canFinalize, blockReason) = EvaluateCanFinalize(ar, exec);
        return ToDto(exec, ar, canFinalize, blockReason);
    }

    public async Task<DisposalFinalizeResultDTO> FinalizeAsync(int userId, int assetRequestId)
    {
        var ar = await _context.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId)
            ?? throw new KeyNotFoundException("Asset request not found.");

        if (ar.RequestTypeId != _disposalRequestTypeId)
            throw new InvalidOperationException("Chỉ áp dụng cho yêu cầu thanh lý.");

        if (ar.Status != 4)
            throw new InvalidOperationException("Chỉ hoàn tất được sau khi kế toán đã ghi nhận biên bản thẩm định (trạng thái 4).");

        var exec = await _context.DisposalExecutions.FirstOrDefaultAsync(e => e.AssetRequestId == assetRequestId);
        var (canFinalize, blockReason) = EvaluateCanFinalize(ar, exec);

        if (!canFinalize)
            throw new InvalidOperationException(blockReason ?? "Không thể hoàn tất thanh lý.");

        if (exec == null)
            throw new InvalidOperationException("Vui lòng lưu thông tin thực hiện thanh lý trước khi hoàn tất.");

        if (exec.Status >= 2)
            throw new InvalidOperationException("Đã hoàn tất thực hiện thanh lý.");

        if (!exec.ExecutedDate.HasValue)
            throw new InvalidOperationException("Vui lòng nhập ngày thực hiện thanh lý.");

        if (!exec.ActualDisposalValue.HasValue)
            throw new InvalidOperationException("Vui lòng nhập số tiền thu được từ thanh lý (có thể là 0).");

        var dip = await _context.DisposalRecords.FirstOrDefaultAsync(d => d.AssetRequestId == assetRequestId)
            ?? throw new InvalidOperationException("Không tìm thấy bản ghi thanh lý gắn yêu cầu.");

        if (!ar.AssetInstanceId.HasValue || ar.AssetInstanceId.Value <= 0)
            throw new InvalidOperationException("Yêu cầu không gắn cá thể tài sản.");

        var instance = await _context.AssetInstances.FirstOrDefaultAsync(i => i.AssetInstanceId == ar.AssetInstanceId.Value)
            ?? throw new KeyNotFoundException("Không tìm thấy cá thể tài sản.");

        var now = DateTime.UtcNow;
        var fromStatus = ar.Status;

        instance.Status = (int)AssetStatus.Liquidated;
        instance.DepreciationPolicyId = null;

        var latestDep = await _context.DepreciationRecords
            .Where(r => r.AssetInstanceId == instance.AssetInstanceId)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();
        if (latestDep != null)
            latestDep.IsLocked = true;

        exec.Status = 2;
        exec.ApprovedBy = userId;
        exec.ApprovedDate = now;
        exec.SubmittedBy = userId;
        exec.SubmittedDate = now;
        exec.UpdatedBy = userId;
        exec.UpdatedDate = now;

        ar.Status = 5;
        ar.ApproveDate ??= now;

        var userRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = ar.Status,
            Action = 1,
            ActionByUserId = userId,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = "Accountant finalized disposal: depreciation stopped, asset liquidated.",
            OccurredAt = now
        });

        await _context.SaveChangesAsync();

        return new DisposalFinalizeResultDTO
        {
            AssetRequestId = assetRequestId,
            AssetRequestStatus = ar.Status,
            AssetInstanceId = instance.AssetInstanceId
        };
    }

    public async Task<DisposalExecutionDto> RecordAppraisalAsync(int userId, int assetRequestId, RecordDisposalAppraisalDto dto)
    {
        var ar = await _context.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId)
            ?? throw new KeyNotFoundException("Asset request not found.");

        if (ar.RequestTypeId != _disposalRequestTypeId)
            throw new InvalidOperationException("Chỉ áp dụng cho yêu cầu thanh lý.");

        if (ar.Status != 2)
            throw new InvalidOperationException("Chỉ ghi nhận thẩm định khi yêu cầu đã được giám đốc phê duyệt (trạng thái 2).");

        if (!dto.AppraisalDate.HasValue)
            throw new InvalidOperationException("Vui lòng nhập ngày thẩm định.");

        var minutesNo = string.IsNullOrWhiteSpace(dto.AppraisalMinutesNo) ? null : dto.AppraisalMinutesNo.Trim();
        var conclusion = string.IsNullOrWhiteSpace(dto.AppraisalConclusion) ? null : dto.AppraisalConclusion.Trim();

        if (string.IsNullOrWhiteSpace(minutesNo) && string.IsNullOrWhiteSpace(conclusion))
            throw new InvalidOperationException("Vui lòng nhập số biên bản hoặc kết luận thẩm định.");

        var now = DateTime.UtcNow;
        var exec = await _context.DisposalExecutions.FirstOrDefaultAsync(e => e.AssetRequestId == assetRequestId);

        if (exec == null)
        {
            var dip = await _context.DisposalRecords.AsNoTracking()
                .FirstOrDefaultAsync(d => d.AssetRequestId == assetRequestId);
            exec = new DisposalExecution
            {
                AssetRequestId = assetRequestId,
                DisposalRecordId = dip?.DiposalId,
                Status = 0,
                CreatedBy = userId,
                CreatedDate = now
            };
            _context.DisposalExecutions.Add(exec);
        }

        exec.PlannedExecutionDate = dto.AppraisalDate;
        exec.MinutesNo = minutesNo ?? exec.MinutesNo;
        exec.ExecutionNote = conclusion ?? exec.ExecutionNote;
        exec.UpdatedBy = userId;
        exec.UpdatedDate = now;

        var fromStatus = ar.Status;
        ar.Status = 4;

        var userRole = await _context.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == userId);
        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = ar.Status,
            Action = 1,
            ActionByUserId = userId,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = "Accountant recorded appraisal minutes.",
            OccurredAt = now
        });

        await _context.SaveChangesAsync();

        var (canFinalize, blockReason) = EvaluateCanFinalize(ar, exec);
        return ToDto(exec, ar, canFinalize, blockReason);
    }

    private static (bool canFinalize, string? reason) EvaluateCanFinalize(AssetRequest ar, DisposalExecution? exec)
    {
        if (ar.Status != 4)
            return (false, "Yêu cầu cần ở trạng thái đã ghi nhận biên bản thẩm định (4).");
        if (exec == null)
            return (false, "Vui lòng lưu nháp thông tin thực hiện thanh lý trước.");
        if (exec.Status >= 2)
            return (false, null);
        return (true, null);
    }

    private static DisposalExecutionDto ToDto(
        DisposalExecution e,
        AssetRequest ar,
        bool canFinalize,
        string? blockFinalizeReason) =>
        new()
        {
            DisposalExecutionId = e.DisposalExecutionId,
            AssetRequestId = e.AssetRequestId,
            DisposalRecordId = e.DisposalRecordId,
            PlannedExecutionDate = e.PlannedExecutionDate,
            ExecutedDate = e.ExecutedDate,
            ExecutionMethod = e.ExecutionMethod,
            BuyerName = e.BuyerName,
            BuyerContact = e.BuyerContact,
            ContractNo = e.ContractNo,
            InvoiceNo = e.InvoiceNo,
            MinutesNo = e.MinutesNo,
            ActualDisposalValue = e.ActualDisposalValue,
            ExpenseValue = e.ExpenseValue,
            AttachmentUrls = e.AttachmentUrls,
            ExecutionNote = e.ExecutionNote,
            Status = e.Status,
            AssetRequestStatus = ar.Status,
            CanEdit = (ar.Status == 2 || ar.Status == 4) && e.Status < 2,
            CanFinalize = canFinalize && e.Status < 2,
            BlockFinalizeReason = blockFinalizeReason
        };
}
