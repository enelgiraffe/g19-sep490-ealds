using System;

namespace g19_sep490_ealds.Server.Models;

public partial class DisposalAppraisalReport
{
    public int AppraisalReportId { get; set; }
    public int AppraisalId { get; set; }
    public string? MinutesNo { get; set; }
    public DateTime? MeetingDate { get; set; }
    public decimal? AppraisedValue { get; set; }
    public decimal? MarketReferenceValue { get; set; }
    public string? Summary { get; set; }
    public string? Recommendation { get; set; }
    public string? AttachmentUrls { get; set; }
    public int SubmittedBy { get; set; }
    public DateTime SubmittedDate { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? DirectorDecision { get; set; }
    public string? DirectorComment { get; set; }
    public int? DirectorReviewedBy { get; set; }
    public DateTime? DirectorReviewedDate { get; set; }
    public string? AppraisalMethod { get; set; }
    public string? AppraisedValueInWords { get; set; }
    /// <summary>Kết quả thẩm định (mô tả ngắn gọn, tách với tóm tắt / kiến nghị).</summary>
    public string? AppraisalOutcome { get; set; }

    public virtual DisposalAppraisal Appraisal { get; set; } = null!;
    public virtual User SubmittedByNavigation { get; set; } = null!;
    public virtual User? UpdatedByNavigation { get; set; }
    public virtual User? DirectorReviewedByNavigation { get; set; }
}

