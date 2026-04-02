using System;

namespace g19_sep490_ealds.Server.Models;

public partial class DisposalAppraisalMemberDecision
{
    public int AppraisalMemberDecisionId { get; set; }
    public int AppraisalId { get; set; }
    public int AppraisalMemberId { get; set; }
    public int UserId { get; set; }
    public int Decision { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? DecisionDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual DisposalAppraisal Appraisal { get; set; } = null!;
    public virtual DisposalAppraisalMember AppraisalMember { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

