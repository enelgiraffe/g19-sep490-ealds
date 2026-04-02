using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class DisposalAppraisalListItemDto
{
    public int AppraisalId { get; set; }
    public int AssetRequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public int RequestStatus { get; set; }
    public DateTime RequestCreateDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? MeetingLocation { get; set; }
    public int? MeetingDepartmentId { get; set; }
    public string? MeetingDepartmentName { get; set; }
    public int Status { get; set; }
    public bool IsReporter { get; set; }
    public bool IsRelatedMember { get; set; }
    public bool HasReport { get; set; }
}

public class DisposalAppraisalMemberDto
{
    public int AppraisalMemberId { get; set; }
    public int UserId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberRole { get; set; }
    public bool IsReporter { get; set; }
    public int Decision { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? DecisionDate { get; set; }
}

public class DisposalAppraisalReportDto
{
    public int? AppraisalReportId { get; set; }
    public string? MinutesNo { get; set; }
    public DateTime? MeetingDate { get; set; }
    public decimal? AppraisedValue { get; set; }
    public decimal? MarketReferenceValue { get; set; }
    public string? AppraisalMethod { get; set; }
    public string? AppraisedValueInWords { get; set; }
    public string? AppraisalOutcome { get; set; }
    public string? Summary { get; set; }
    public string? Recommendation { get; set; }
    public string? AttachmentUrls { get; set; }
    public int? SubmittedBy { get; set; }
    public DateTime? SubmittedDate { get; set; }
}

public class DisposalAppraisalDetailDto
{
    public int AppraisalId { get; set; }
    public int AssetRequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public int RequestStatus { get; set; }
    public DateTime RequestCreateDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? MeetingLocation { get; set; }
    public int? MeetingDepartmentId { get; set; }
    public string? MeetingDepartmentName { get; set; }
    public int Status { get; set; }
    public int? ReporterUserId { get; set; }
    public bool IsReporter { get; set; }
    public bool IsRelatedMember { get; set; }
    public bool CanManageCommittee { get; set; }
    public DisposalAppraisalReportDto? Report { get; set; }
    public List<DisposalAppraisalMemberDto> Members { get; set; } = new();
}

public class CreateDisposalAppraisalDto
{
    public int UserId { get; set; }
    public int AssetRequestId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? MeetingLocation { get; set; }
    public int? MeetingDepartmentId { get; set; }
    public int? ReporterUserId { get; set; }
}

public class UpdateDisposalAppraisalDto
{
    public int UserId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? MeetingLocation { get; set; }
    public int? MeetingDepartmentId { get; set; }
    public int? ReporterUserId { get; set; }
}

public class AddDisposalAppraisalMemberDto
{
    public int UserId { get; set; }
    public int MemberUserId { get; set; }
    public string? MemberRole { get; set; }
    public bool SetAsReporter { get; set; }
}

public class SaveDisposalAppraisalReportDto
{
    public int UserId { get; set; }
    public string? MinutesNo { get; set; }
    public DateTime? MeetingDate { get; set; }
    public decimal? AppraisedValue { get; set; }
    public decimal? MarketReferenceValue { get; set; }
    public string? AppraisalMethod { get; set; }
    public string? AppraisedValueInWords { get; set; }
    public string? AppraisalOutcome { get; set; }
    public string? Summary { get; set; }
    public string? Recommendation { get; set; }
    public string? AttachmentUrls { get; set; }
}

public class DecideDisposalAppraisalDto
{
    public int UserId { get; set; }
    public int Decision { get; set; } // 1=confirm, 2=reject
    public string? RejectReason { get; set; }
}

