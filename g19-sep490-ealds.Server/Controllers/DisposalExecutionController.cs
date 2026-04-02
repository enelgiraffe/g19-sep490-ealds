using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Authorize(Roles = "ACCOUNTANT")]
[Route("api/Assets/Requests/disposal/execution")]
public class DisposalExecutionController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _disposalRequestTypeId;

    public DisposalExecutionController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _disposalRequestTypeId = configuration.GetValue<int>("App:DisposalRequestTypeId", 5);
    }

    [HttpGet("by-request/{assetRequestId:int}")]
    public async Task<IActionResult> GetByAssetRequest(int assetRequestId)
    {
        var ar = await _db.AssetRequests.AsNoTracking().FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId);
        if (ar == null) return NotFound();
        if (ar.RequestTypeId != _disposalRequestTypeId)
            return BadRequest("Chỉ áp dụng cho yêu cầu thanh lý.");

        var exec = await _db.DisposalExecutions.AsNoTracking()
            .FirstOrDefaultAsync(e => e.AssetRequestId == assetRequestId);
        var appraisal = await _db.DisposalAppraisals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId);
        var (canFinalize, blockReason) = await EvaluateCanFinalizeAsync(ar, exec, appraisal);

        if (exec == null)
        {
            return Ok(new DisposalExecutionDto
            {
                DisposalExecutionId = null,
                AssetRequestId = assetRequestId,
                AppraisalId = appraisal?.AppraisalId,
                DisposalRecordId = await _db.DisposalRecords.AsNoTracking()
                    .Where(d => d.AssetRequestId == assetRequestId)
                    .Select(d => (int?)d.DiposalId)
                    .FirstOrDefaultAsync(),
                Status = 0,
                CanEdit = ar.Status == 2,
                CanFinalize = canFinalize,
                BlockFinalizeReason = blockReason
            });
        }

        return Ok(ToDto(exec, ar, canFinalize, blockReason));
    }

    [HttpPut("by-request/{assetRequestId:int}")]
    public async Task<IActionResult> SaveDraft(int assetRequestId, [FromBody] SaveDisposalExecutionDto dto)
    {
        var actorUserId = ResolveActorUserId(dto.UserId);
        if (actorUserId <= 0) return Unauthorized();

        var ar = await _db.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId);
        if (ar == null) return NotFound();
        if (ar.RequestTypeId != _disposalRequestTypeId)
            return BadRequest("Chỉ áp dụng cho yêu cầu thanh lý.");
        if (ar.Status != 2)
            return BadRequest("Chỉ lưu được khi yêu cầu đã được giám đốc phê duyệt (trạng thái 2).");

        var now = DateTime.UtcNow;
        var exec = await _db.DisposalExecutions.FirstOrDefaultAsync(e => e.AssetRequestId == assetRequestId);
        if (exec != null && exec.Status >= 2)
            return BadRequest("Đã hoàn tất thực hiện thanh lý, không thể sửa.");

        var dip = await _db.DisposalRecords.AsNoTracking()
            .FirstOrDefaultAsync(d => d.AssetRequestId == assetRequestId);
        var appraisal = await _db.DisposalAppraisals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId);

        if (exec == null)
        {
            exec = new DisposalExecution
            {
                AssetRequestId = assetRequestId,
                AppraisalId = appraisal?.AppraisalId,
                DisposalRecordId = dip?.DiposalId,
                Status = 0,
                CreatedBy = actorUserId,
                CreatedDate = now
            };
            _db.DisposalExecutions.Add(exec);
        }

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
        exec.UpdatedBy = actorUserId;
        exec.UpdatedDate = now;

        await _db.SaveChangesAsync();
        var appraisal2 = await _db.DisposalAppraisals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId);
        var (canFinalize, blockReason) = await EvaluateCanFinalizeAsync(ar, exec, appraisal2);
        return Ok(ToDto(exec, ar, canFinalize, blockReason));
    }

    [HttpPost("by-request/{assetRequestId:int}/finalize")]
    public async Task<IActionResult> Finalize(int assetRequestId, [FromBody] FinalizeDisposalExecutionDto dto)
    {
        var actorUserId = ResolveActorUserId(dto.UserId);
        if (actorUserId <= 0) return Unauthorized();

        var ar = await _db.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId);
        if (ar == null) return NotFound();
        if (ar.RequestTypeId != _disposalRequestTypeId)
            return BadRequest("Chỉ áp dụng cho yêu cầu thanh lý.");
        if (ar.Status != 2)
            return BadRequest("Chỉ hoàn tất được khi yêu cầu đã được giám đốc phê duyệt (trạng thái 2).");

        var appraisal = await _db.DisposalAppraisals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId);
        var exec = await _db.DisposalExecutions.FirstOrDefaultAsync(e => e.AssetRequestId == assetRequestId);
        var (canFinalize, blockReason) = await EvaluateCanFinalizeAsync(ar, exec, appraisal);
        if (!canFinalize)
            return BadRequest(blockReason ?? "Không thể hoàn tất thanh lý.");

        if (exec == null)
            return BadRequest("Vui lòng lưu thông tin thực hiện thanh lý trước khi hoàn tất.");
        if (exec.Status >= 2)
            return BadRequest("Đã hoàn tất thực hiện thanh lý.");

        if (!exec.ExecutedDate.HasValue)
            return BadRequest("Vui lòng nhập ngày thực hiện thanh lý.");
        if (!exec.ActualDisposalValue.HasValue)
            return BadRequest("Vui lòng nhập số tiền thu được từ thanh lý (có thể là 0).");

        var dip = await _db.DisposalRecords.FirstOrDefaultAsync(d => d.AssetRequestId == assetRequestId);
        if (dip == null) return BadRequest("Không tìm thấy bản ghi thanh lý gắn yêu cầu.");
        if (!ar.AssetInstanceId.HasValue || ar.AssetInstanceId.Value <= 0)
            return BadRequest("Yêu cầu không gắn cá thể tài sản.");

        var instance = await _db.AssetInstances.FirstOrDefaultAsync(i => i.AssetInstanceId == ar.AssetInstanceId.Value);
        if (instance == null) return NotFound("Không tìm thấy cá thể tài sản.");

        var now = DateTime.UtcNow;
        var fromStatus = ar.Status;

        instance.Status = (int)AssetStatus.Liquidated;
        instance.DepreciationPolicyId = null;

        var latestDep = await _db.DepreciationRecords
            .Where(r => r.AssetInstanceId == instance.AssetInstanceId)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();
        if (latestDep != null)
            latestDep.IsLocked = true;

        exec.Status = 2;
        exec.ApprovedBy = actorUserId;
        exec.ApprovedDate = now;
        exec.SubmittedBy = actorUserId;
        exec.SubmittedDate = now;
        exec.UpdatedBy = actorUserId;
        exec.UpdatedDate = now;

        ar.Status = 5;
        ar.ApproveDate ??= now;

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == actorUserId);
        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = ar.Status,
            Action = 1,
            ActionByUserId = actorUserId,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = "Accountant finalized disposal: depreciation stopped, asset liquidated.",
            OccurredAt = now
        };
        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = assetRequestId, assetRequestStatus = ar.Status, assetInstanceId = instance.AssetInstanceId });
    }

    private Task<(bool canFinalize, string? reason)> EvaluateCanFinalizeAsync(
        AssetRequest ar,
        DisposalExecution? exec,
        DisposalAppraisal? appraisal)
    {
        if (ar.Status != 2)
            return Task.FromResult((false, (string?)"Yêu cầu cần ở trạng thái đã phê duyệt giám đốc (2)."));
        if (appraisal != null && appraisal.Status < 4)
            return Task.FromResult((false, (string?)"Cần hội đồng thẩm định xác nhận biên bản trước khi hoàn tất thanh lý."));
        if (exec == null)
            return Task.FromResult((false, (string?)"Vui lòng lưu nháp thông tin thực hiện thanh lý trước."));
        if (exec.Status >= 2)
            return Task.FromResult((false, (string?)null));
        return Task.FromResult((true, (string?)null));
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
            AppraisalId = e.AppraisalId,
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
            CanEdit = ar.Status == 2 && e.Status < 2,
            CanFinalize = canFinalize && e.Status < 2,
            BlockFinalizeReason = blockFinalizeReason
        };

    private int ResolveActorUserId(int fallbackUserId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var parsedUserId) && parsedUserId > 0)
            return parsedUserId;
        return fallbackUserId;
    }
}
