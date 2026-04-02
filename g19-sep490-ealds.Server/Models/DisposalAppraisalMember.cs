using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class DisposalAppraisalMember
{
    public int AppraisalMemberId { get; set; }
    public int AppraisalId { get; set; }
    public int UserId { get; set; }
    public bool IsReporter { get; set; }
    public string? MemberRole { get; set; }
    public int AddedBy { get; set; }
    public DateTime AddedDate { get; set; }

    public virtual DisposalAppraisal Appraisal { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User AddedByNavigation { get; set; } = null!;
    public virtual ICollection<DisposalAppraisalMemberDecision> DisposalAppraisalMemberDecisions { get; set; } = new List<DisposalAppraisalMemberDecision>();
}

