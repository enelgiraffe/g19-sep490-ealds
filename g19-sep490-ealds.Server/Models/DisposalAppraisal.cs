using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class DisposalAppraisal
{
    public int AppraisalId { get; set; }
    public int AssetRequestId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? MeetingLocation { get; set; }
    public int? MeetingDepartmentId { get; set; }
    public int? ReporterUserId { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual AssetRequest AssetRequest { get; set; } = null!;
    public virtual Department? MeetingDepartment { get; set; }
    public virtual User CreatedByNavigation { get; set; } = null!;
    public virtual User? ReporterUser { get; set; }
    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<DisposalAppraisalMember> DisposalAppraisalMembers { get; set; } = new List<DisposalAppraisalMember>();
    public virtual ICollection<DisposalAppraisalReport> DisposalAppraisalReports { get; set; } = new List<DisposalAppraisalReport>();
    public virtual ICollection<DisposalAppraisalMemberDecision> DisposalAppraisalMemberDecisions { get; set; } = new List<DisposalAppraisalMemberDecision>();
}

