using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/disposal/appraisals")]
public class DisposalAppraisalsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _disposalRequestTypeId;

    public DisposalAppraisalsController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _disposalRequestTypeId = configuration.GetValue<int>("App:DisposalRequestTypeId", 5);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyAppraisals([FromQuery] int? userId = null)
    {
        var actorUserId = ResolveActorUserId(userId);
        if (actorUserId <= 0) return Unauthorized();

        var relatedAppraisalIds = await _db.DisposalAppraisalMembers
            .AsNoTracking()
            .Where(x => x.UserId == actorUserId)
            .Select(x => x.AppraisalId)
            .Distinct()
            .ToListAsync();

        var list = await _db.DisposalAppraisals
            .AsNoTracking()
            .Include(a => a.AssetRequest)
            .Include(a => a.DisposalAppraisalReports)
            .Include(a => a.MeetingDepartment)
            .Where(a => a.ReporterUserId == actorUserId || relatedAppraisalIds.Contains(a.AppraisalId))
            .OrderByDescending(a => a.CreatedDate)
            .Select(a => new DisposalAppraisalListItemDto
            {
                AppraisalId = a.AppraisalId,
                AssetRequestId = a.AssetRequestId,
                RequestTitle = a.AssetRequest.Title,
                RequestStatus = a.AssetRequest.Status,
                RequestCreateDate = a.AssetRequest.CreateDate,
                ScheduledAt = a.ScheduledAt,
                MeetingLocation = a.MeetingLocation,
                MeetingDepartmentId = a.MeetingDepartmentId,
                MeetingDepartmentName = a.MeetingDepartment != null ? a.MeetingDepartment.Name : null,
                Status = a.Status,
                IsReporter = a.ReporterUserId == actorUserId,
                IsRelatedMember = a.DisposalAppraisalMembers.Any(m => m.UserId == actorUserId),
                HasReport = a.DisposalAppraisalReports.Any()
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Danh sách tất cả đợt thẩm định thanh lý (chỉ giám đốc).</summary>
    [HttpGet("director")]
    public async Task<IActionResult> GetDirectorAppraisalList([FromQuery] int? userId = null)
    {
        var actorUserId = ResolveActorUserId(userId);
        if (actorUserId <= 0) return Unauthorized();
        if (!await IsDirectorAsync(actorUserId)) return Forbid();

        var list = await _db.DisposalAppraisals
            .AsNoTracking()
            .Include(a => a.AssetRequest)
            .Include(a => a.DisposalAppraisalReports)
            .Include(a => a.MeetingDepartment)
            .Include(a => a.DisposalAppraisalMembers)
            .OrderByDescending(a => a.CreatedDate)
            .Select(a => new DisposalAppraisalListItemDto
            {
                AppraisalId = a.AppraisalId,
                AssetRequestId = a.AssetRequestId,
                RequestTitle = a.AssetRequest.Title,
                RequestStatus = a.AssetRequest.Status,
                RequestCreateDate = a.AssetRequest.CreateDate,
                ScheduledAt = a.ScheduledAt,
                MeetingLocation = a.MeetingLocation,
                MeetingDepartmentId = a.MeetingDepartmentId,
                MeetingDepartmentName = a.MeetingDepartment != null ? a.MeetingDepartment.Name : null,
                Status = a.Status,
                IsReporter = a.ReporterUserId == actorUserId,
                IsRelatedMember = a.DisposalAppraisalMembers.Any(m => m.UserId == actorUserId),
                HasReport = a.DisposalAppraisalReports.Any()
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Chi tiết đợt thẩm định theo mã yêu cầu thanh lý (giám đốc / thành viên).</summary>
    [HttpGet("by-request/{assetRequestId:int}")]
    public async Task<IActionResult> GetByAssetRequest(int assetRequestId, [FromQuery] int? userId = null)
    {
        var actorUserId = ResolveActorUserId(userId);
        if (actorUserId <= 0) return Unauthorized();

        var appraisal = await _db.DisposalAppraisals
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetRequestId == assetRequestId);
        if (appraisal == null) return NotFound();

        if (!await CanAccessAppraisalAsync(appraisal.AppraisalId, actorUserId)) return Forbid();

        var dto = await BuildDetailDtoAsync(appraisal.AppraisalId, actorUserId);
        return Ok(dto);
    }

    [HttpGet("{appraisalId:int}")]
    public async Task<IActionResult> GetDetail(int appraisalId, [FromQuery] int? userId = null)
    {
        var actorUserId = ResolveActorUserId(userId);
        if (actorUserId <= 0) return Unauthorized();

        if (!await _db.DisposalAppraisals.AsNoTracking().AnyAsync(a => a.AppraisalId == appraisalId))
            return NotFound();

        if (!await CanAccessAppraisalAsync(appraisalId, actorUserId)) return Forbid();

        var dto = await BuildDetailDtoAsync(appraisalId, actorUserId);
        return Ok(dto);
    }

    /// <summary>Giám đốc tạo đợt thẩm định cho một yêu cầu thanh lý (một yêu cầu chỉ một đợt).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDisposalAppraisalDto dto)
    {
        var actorUserId = ResolveActorUserId(dto.UserId);
        if (actorUserId <= 0) return Unauthorized();
        if (!await IsDirectorAsync(actorUserId)) return Forbid();

        var ar = await _db.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == dto.AssetRequestId);
        if (ar == null) return NotFound("Không tìm thấy yêu cầu.");
        if (ar.RequestTypeId != _disposalRequestTypeId)
            return BadRequest("Chỉ áp dụng cho yêu cầu thanh lý.");
        if (ar.Status < 1)
            return BadRequest("Yêu cầu cần được kế toán xử lý (trạng thái >= 1) trước khi lập hội đồng thẩm định.");

        if (await _db.DisposalAppraisals.AnyAsync(a => a.AssetRequestId == dto.AssetRequestId))
            return Conflict("Đã có đợt thẩm định cho yêu cầu này.");

        if (dto.ReporterUserId.HasValue && dto.ReporterUserId.Value > 0)
        {
            var reporterExists = await _db.Users.AnyAsync(u => u.UserId == dto.ReporterUserId.Value);
            if (!reporterExists) return BadRequest("ReporterUserId không hợp lệ.");
        }

        if (dto.MeetingDepartmentId.HasValue && dto.MeetingDepartmentId.Value > 0)
        {
            var deptOk = await _db.Departments.AsNoTracking()
                .AnyAsync(d => d.DepartmentId == dto.MeetingDepartmentId.Value);
            if (!deptOk) return BadRequest("MeetingDepartmentId không hợp lệ.");
        }

        var now = DateTime.UtcNow;
        var appraisal = new DisposalAppraisal
        {
            AssetRequestId = dto.AssetRequestId,
            ScheduledAt = dto.ScheduledAt,
            MeetingLocation = string.IsNullOrWhiteSpace(dto.MeetingLocation) ? null : dto.MeetingLocation.Trim(),
            MeetingDepartmentId = dto.MeetingDepartmentId is > 0 ? dto.MeetingDepartmentId : null,
            ReporterUserId = dto.ReporterUserId is > 0 ? dto.ReporterUserId : null,
            Status = dto.ScheduledAt.HasValue ? 1 : 0,
            CreatedBy = actorUserId,
            CreatedDate = now
        };
        _db.DisposalAppraisals.Add(appraisal);
        await _db.SaveChangesAsync();

        if (appraisal.ReporterUserId.HasValue)
        {
            var ru = appraisal.ReporterUserId.Value;
            var member = new DisposalAppraisalMember
            {
                AppraisalId = appraisal.AppraisalId,
                UserId = ru,
                IsReporter = true,
                MemberRole = null,
                AddedBy = actorUserId,
                AddedDate = now
            };
            _db.DisposalAppraisalMembers.Add(member);
            await _db.SaveChangesAsync();
            await EnsureMemberDecisionRowAsync(appraisal.AppraisalId, member.AppraisalMemberId, ru, now);
            await _db.SaveChangesAsync();
        }

        var detail = await BuildDetailDtoAsync(appraisal.AppraisalId, actorUserId);
        return Ok(detail);
    }

    [HttpPut("{appraisalId:int}")]
    public async Task<IActionResult> Update(int appraisalId, [FromBody] UpdateDisposalAppraisalDto dto)
    {
        var actorUserId = ResolveActorUserId(dto.UserId);
        if (actorUserId <= 0) return Unauthorized();
        if (!await IsDirectorAsync(actorUserId)) return Forbid();

        var appraisal = await _db.DisposalAppraisals.FirstOrDefaultAsync(a => a.AppraisalId == appraisalId);
        if (appraisal == null) return NotFound();

        if (dto.ReporterUserId.HasValue && dto.ReporterUserId.Value > 0)
        {
            var ok = await _db.Users.AnyAsync(u => u.UserId == dto.ReporterUserId.Value);
            if (!ok) return BadRequest("ReporterUserId không hợp lệ.");
        }

        if (dto.MeetingDepartmentId.HasValue && dto.MeetingDepartmentId.Value > 0)
        {
            var deptOk = await _db.Departments.AsNoTracking()
                .AnyAsync(d => d.DepartmentId == dto.MeetingDepartmentId.Value);
            if (!deptOk) return BadRequest("MeetingDepartmentId không hợp lệ.");
        }

        var now = DateTime.UtcNow;
        appraisal.ScheduledAt = dto.ScheduledAt;
        appraisal.MeetingLocation = string.IsNullOrWhiteSpace(dto.MeetingLocation) ? null : dto.MeetingLocation.Trim();
        appraisal.MeetingDepartmentId = dto.MeetingDepartmentId is > 0 ? dto.MeetingDepartmentId : null;
        appraisal.ReporterUserId = dto.ReporterUserId is > 0 ? dto.ReporterUserId : null;
        appraisal.Status = dto.ScheduledAt.HasValue ? Math.Max(appraisal.Status, 1) : appraisal.Status;
        appraisal.UpdatedBy = actorUserId;
        appraisal.UpdatedDate = now;

        await SyncReporterFlagsOnAppraisalAsync(appraisal);
        await _db.SaveChangesAsync();

        var detail = await BuildDetailDtoAsync(appraisalId, actorUserId);
        return Ok(detail);
    }

    [HttpPost("{appraisalId:int}/members")]
    public async Task<IActionResult> AddMember(int appraisalId, [FromBody] AddDisposalAppraisalMemberDto dto)
    {
        var actorUserId = ResolveActorUserId(dto.UserId);
        if (actorUserId <= 0) return Unauthorized();
        if (!await IsDirectorAsync(actorUserId)) return Forbid();

        var appraisal = await _db.DisposalAppraisals.FirstOrDefaultAsync(a => a.AppraisalId == appraisalId);
        if (appraisal == null) return NotFound();

        if (dto.MemberUserId <= 0) return BadRequest("MemberUserId không hợp lệ.");
        var userExists = await _db.Users.AnyAsync(u => u.UserId == dto.MemberUserId);
        if (!userExists) return BadRequest("Người dùng không tồn tại.");

        if (await _db.DisposalAppraisalMembers.AnyAsync(m => m.AppraisalId == appraisalId && m.UserId == dto.MemberUserId))
            return Conflict("Thành viên đã có trong hội đồng.");

        var now = DateTime.UtcNow;
        var member = new DisposalAppraisalMember
        {
            AppraisalId = appraisalId,
            UserId = dto.MemberUserId,
            IsReporter = false,
            MemberRole = string.IsNullOrWhiteSpace(dto.MemberRole) ? null : dto.MemberRole.Trim(),
            AddedBy = actorUserId,
            AddedDate = now
        };
        _db.DisposalAppraisalMembers.Add(member);
        await _db.SaveChangesAsync();

        await EnsureMemberDecisionRowAsync(appraisalId, member.AppraisalMemberId, dto.MemberUserId, now);
        await _db.SaveChangesAsync();

        if (dto.SetAsReporter)
        {
            appraisal.ReporterUserId = dto.MemberUserId;
            appraisal.UpdatedBy = actorUserId;
            appraisal.UpdatedDate = now;
            await SyncReporterFlagsOnAppraisalAsync(appraisal);
            await _db.SaveChangesAsync();
        }

        var detail = await BuildDetailDtoAsync(appraisalId, actorUserId);
        return Ok(detail);
    }

    [HttpDelete("{appraisalId:int}/members/{appraisalMemberId:int}")]
    public async Task<IActionResult> RemoveMember(int appraisalId, int appraisalMemberId, [FromQuery] int userId)
    {
        var actorUserId = ResolveActorUserId(userId);
        if (actorUserId <= 0) return Unauthorized();
        if (!await IsDirectorAsync(actorUserId)) return Forbid();

        var appraisal = await _db.DisposalAppraisals.FirstOrDefaultAsync(a => a.AppraisalId == appraisalId);
        if (appraisal == null) return NotFound();

        var member = await _db.DisposalAppraisalMembers
            .FirstOrDefaultAsync(m => m.AppraisalId == appraisalId && m.AppraisalMemberId == appraisalMemberId);
        if (member == null) return NotFound();

        var decisions = await _db.DisposalAppraisalMemberDecisions
            .Where(d => d.AppraisalMemberId == appraisalMemberId)
            .ToListAsync();
        if (decisions.Count > 0)
            _db.DisposalAppraisalMemberDecisions.RemoveRange(decisions);

        _db.DisposalAppraisalMembers.Remove(member);

        if (appraisal.ReporterUserId == member.UserId)
        {
            appraisal.ReporterUserId = null;
            appraisal.UpdatedBy = actorUserId;
            appraisal.UpdatedDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await SyncReporterFlagsOnAppraisalAsync(appraisal);
        await _db.SaveChangesAsync();

        var detail = await BuildDetailDtoAsync(appraisalId, actorUserId);
        return Ok(detail);
    }

    [HttpPost("{appraisalId:int}/report")]
    public async Task<IActionResult> SaveReport(int appraisalId, [FromBody] SaveDisposalAppraisalReportDto dto)
    {
        var actorUserId = ResolveActorUserId(dto.UserId);
        if (actorUserId <= 0) return Unauthorized();

        var appraisal = await _db.DisposalAppraisals
            .FirstOrDefaultAsync(x => x.AppraisalId == appraisalId);
        if (appraisal == null) return NotFound();
        if (appraisal.ReporterUserId != actorUserId) return Forbid();
        // Hội đồng đã xác nhận đồng loạt (status 4) — không cho sửa biên bản nữa.
        if (appraisal.Status >= 4)
            return BadRequest("Biên bản đã được hội đồng xác nhận, không thể chỉnh sửa.");

        var now = DateTime.UtcNow;
        var report = await _db.DisposalAppraisalReports
            .FirstOrDefaultAsync(x => x.AppraisalId == appraisalId);
        if (report == null)
        {
            report = new DisposalAppraisalReport
            {
                AppraisalId = appraisalId,
                SubmittedBy = actorUserId,
                SubmittedDate = now
            };
            _db.DisposalAppraisalReports.Add(report);
        }
        else
        {
            report.UpdatedBy = actorUserId;
            report.UpdatedDate = now;
        }

        report.MinutesNo = string.IsNullOrWhiteSpace(dto.MinutesNo) ? null : dto.MinutesNo.Trim();
        report.MeetingDate = dto.MeetingDate;
        report.AppraisedValue = dto.AppraisedValue;
        report.MarketReferenceValue = dto.MarketReferenceValue;
        report.AppraisalMethod = string.IsNullOrWhiteSpace(dto.AppraisalMethod) ? null : dto.AppraisalMethod.Trim();
        report.AppraisedValueInWords = string.IsNullOrWhiteSpace(dto.AppraisedValueInWords) ? null : dto.AppraisedValueInWords.Trim();
        report.AppraisalOutcome = string.IsNullOrWhiteSpace(dto.AppraisalOutcome) ? null : dto.AppraisalOutcome.Trim();
        report.Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary.Trim();
        report.Recommendation = string.IsNullOrWhiteSpace(dto.Recommendation) ? null : dto.Recommendation.Trim();
        report.AttachmentUrls = string.IsNullOrWhiteSpace(dto.AttachmentUrls) ? null : dto.AttachmentUrls.Trim();

        appraisal.Status = 2; // ReportSubmitted
        appraisal.UpdatedBy = actorUserId;
        appraisal.UpdatedDate = now;

        await _db.SaveChangesAsync();
        return Ok(new { appraisalId, status = appraisal.Status });
    }

    [HttpPost("{appraisalId:int}/decision")]
    public async Task<IActionResult> SaveMemberDecision(int appraisalId, [FromBody] DecideDisposalAppraisalDto dto)
    {
        var actorUserId = ResolveActorUserId(dto.UserId);
        if (actorUserId <= 0) return Unauthorized();
        if (dto.Decision != 1 && dto.Decision != 2) return BadRequest("Decision must be 1 (confirm) or 2 (reject).");
        if (dto.Decision == 2 && string.IsNullOrWhiteSpace(dto.RejectReason))
            return BadRequest("Reject reason is required.");

        var member = await _db.DisposalAppraisalMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.AppraisalId == appraisalId && m.UserId == actorUserId);
        if (member == null) return Forbid();

        var now = DateTime.UtcNow;
        var decision = await _db.DisposalAppraisalMemberDecisions
            .FirstOrDefaultAsync(x => x.AppraisalId == appraisalId && x.AppraisalMemberId == member.AppraisalMemberId);
        if (decision == null)
        {
            decision = new DisposalAppraisalMemberDecision
            {
                AppraisalId = appraisalId,
                AppraisalMemberId = member.AppraisalMemberId,
                UserId = actorUserId,
                CreatedDate = now
            };
            _db.DisposalAppraisalMemberDecisions.Add(decision);
        }

        decision.Decision = dto.Decision;
        decision.RejectReason = dto.Decision == 2 ? dto.RejectReason?.Trim() : null;
        decision.DecisionDate = now;
        decision.UpdatedDate = now;

        await _db.SaveChangesAsync();
        var councilStatus = await RefreshAppraisalCouncilStatusAsync(appraisalId);
        return Ok(new { appraisalId, decision = decision.Decision, status = councilStatus });
    }

    /// <summary>
    /// Khi đã có biên bản (status &gt;= 2): nếu mọi thành viên hội đồng (không tính người nhập biên bản) đều xác nhận (1) thì chuyển status sang 4 (đã xác nhận hội đồng).
    /// </summary>
    private async Task<int> RefreshAppraisalCouncilStatusAsync(int appraisalId)
    {
        var appraisal = await _db.DisposalAppraisals
            .FirstOrDefaultAsync(a => a.AppraisalId == appraisalId);
        if (appraisal == null) return 0;
        if (appraisal.Status < 2) return appraisal.Status;
        if (appraisal.Status >= 4) return appraisal.Status;

        var voterMemberIds = await _db.DisposalAppraisalMembers
            .AsNoTracking()
            .Where(m => m.AppraisalId == appraisalId && !m.IsReporter)
            .Select(m => m.AppraisalMemberId)
            .ToListAsync();

        if (voterMemberIds.Count == 0)
            return appraisal.Status;

        var decisions = await _db.DisposalAppraisalMemberDecisions
            .AsNoTracking()
            .Where(d => d.AppraisalId == appraisalId && voterMemberIds.Contains(d.AppraisalMemberId))
            .ToListAsync();

        foreach (var memberId in voterMemberIds)
        {
            var d = decisions.FirstOrDefault(x => x.AppraisalMemberId == memberId);
            if (d == null || d.Decision == 0)
                return appraisal.Status;
            if (d.Decision == 2)
                return appraisal.Status;
        }

        appraisal.Status = 4;
        appraisal.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return appraisal.Status;
    }

    private async Task<bool> IsDirectorAsync(int userId)
    {
        if (User.IsInRole("DIRECTOR")) return true;
        if (User.Claims.Any(c =>
                c.Type == ClaimTypes.Role &&
                !string.IsNullOrWhiteSpace(c.Value) &&
                c.Value.Contains("DIRECTOR", StringComparison.OrdinalIgnoreCase)))
            return true;

        return await _db.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .AnyAsync(ur =>
                ur.UserId == userId &&
                ur.Role != null &&
                (
                    (ur.Role.Code != null && ur.Role.Code.ToUpper() == "DIRECTOR") ||
                    (ur.Role.Name != null && ur.Role.Name.ToUpper().Contains("DIRECTOR")) ||
                    (ur.Role.Name != null && ur.Role.Name.ToUpper().Contains("GIÁM ĐỐC"))
                ));
    }

    private async Task<bool> CanAccessAppraisalAsync(int appraisalId, int actorUserId)
    {
        if (await IsDirectorAsync(actorUserId)) return true;

        var appraisal = await _db.DisposalAppraisals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AppraisalId == appraisalId);
        if (appraisal == null) return false;
        if (appraisal.ReporterUserId == actorUserId) return true;
        return await _db.DisposalAppraisalMembers
            .AsNoTracking()
            .AnyAsync(m => m.AppraisalId == appraisalId && m.UserId == actorUserId);
    }

    private async Task<DisposalAppraisalDetailDto> BuildDetailDtoAsync(int appraisalId, int actorUserId)
    {
        var appraisal = await _db.DisposalAppraisals
            .AsNoTracking()
            .Include(a => a.AssetRequest)
            .Include(a => a.DisposalAppraisalReports)
            .Include(a => a.DisposalAppraisalMembers)
            .Include(a => a.MeetingDepartment)
            .FirstAsync(a => a.AppraisalId == appraisalId);

        var isDirector = await IsDirectorAsync(actorUserId);
        var isMember = appraisal.DisposalAppraisalMembers.Any(m => m.UserId == actorUserId);
        var isReporter = appraisal.ReporterUserId == actorUserId;

        var memberRows = await (
            from m in _db.DisposalAppraisalMembers.AsNoTracking()
            join u in _db.Users.AsNoTracking() on m.UserId equals u.UserId
            join e in _db.Employees.AsNoTracking() on u.UserId equals e.UserId into emp
            from e in emp.DefaultIfEmpty()
            join d in _db.DisposalAppraisalMemberDecisions.AsNoTracking()
                on new { m.AppraisalId, m.AppraisalMemberId } equals new { d.AppraisalId, d.AppraisalMemberId } into decisionRows
            from d in decisionRows.DefaultIfEmpty()
            where m.AppraisalId == appraisalId
            orderby m.AppraisalMemberId
            select new DisposalAppraisalMemberDto
            {
                AppraisalMemberId = m.AppraisalMemberId,
                UserId = m.UserId,
                MemberName = !string.IsNullOrWhiteSpace(e.Name) ? e.Name : (u.Email ?? $"User #{u.UserId}"),
                MemberRole = m.MemberRole,
                IsReporter = m.IsReporter,
                Decision = d != null ? d.Decision : 0,
                RejectReason = d != null ? d.RejectReason : null,
                DecisionDate = d != null ? d.DecisionDate : null
            })
            .ToListAsync();

        var report = appraisal.DisposalAppraisalReports
            .OrderByDescending(r => r.SubmittedDate)
            .FirstOrDefault();

        return new DisposalAppraisalDetailDto
        {
            AppraisalId = appraisal.AppraisalId,
            AssetRequestId = appraisal.AssetRequestId,
            RequestTitle = appraisal.AssetRequest.Title,
            RequestStatus = appraisal.AssetRequest.Status,
            RequestCreateDate = appraisal.AssetRequest.CreateDate,
            ScheduledAt = appraisal.ScheduledAt,
            MeetingLocation = appraisal.MeetingLocation,
            MeetingDepartmentId = appraisal.MeetingDepartmentId,
            MeetingDepartmentName = appraisal.MeetingDepartment?.Name,
            Status = appraisal.Status,
            ReporterUserId = appraisal.ReporterUserId,
            IsReporter = isReporter,
            IsRelatedMember = isMember,
            CanManageCommittee = isDirector,
            Members = memberRows,
            Report = report == null ? null : new DisposalAppraisalReportDto
            {
                AppraisalReportId = report.AppraisalReportId,
                MinutesNo = report.MinutesNo,
                MeetingDate = report.MeetingDate,
                AppraisedValue = report.AppraisedValue,
                MarketReferenceValue = report.MarketReferenceValue,
                AppraisalMethod = report.AppraisalMethod,
                AppraisedValueInWords = report.AppraisedValueInWords,
                AppraisalOutcome = report.AppraisalOutcome,
                Summary = report.Summary,
                Recommendation = report.Recommendation,
                AttachmentUrls = report.AttachmentUrls,
                SubmittedBy = report.SubmittedBy,
                SubmittedDate = report.SubmittedDate
            }
        };
    }

    private async Task EnsureMemberDecisionRowAsync(int appraisalId, int appraisalMemberId, int userId, DateTime now)
    {
        if (await _db.DisposalAppraisalMemberDecisions.AnyAsync(d =>
                d.AppraisalId == appraisalId && d.AppraisalMemberId == appraisalMemberId))
            return;

        _db.DisposalAppraisalMemberDecisions.Add(new DisposalAppraisalMemberDecision
        {
            AppraisalId = appraisalId,
            AppraisalMemberId = appraisalMemberId,
            UserId = userId,
            Decision = 0,
            CreatedDate = now
        });
    }

    private async Task SyncReporterFlagsOnAppraisalAsync(DisposalAppraisal appraisal)
    {
        var members = await _db.DisposalAppraisalMembers
            .Where(m => m.AppraisalId == appraisal.AppraisalId)
            .ToListAsync();
        foreach (var m in members)
            m.IsReporter = appraisal.ReporterUserId.HasValue && m.UserId == appraisal.ReporterUserId.Value;
    }

    private int ResolveActorUserId(int? fallbackUserId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var parsedUserId) && parsedUserId > 0)
            return parsedUserId;
        return fallbackUserId ?? 0;
    }
}
