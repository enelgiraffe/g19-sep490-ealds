using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class WorkflowStep
{
    public int StepId { get; set; }

    public int WorkflowId { get; set; }

    public int StepOrder { get; set; }

    public int RoleId { get; set; }

    public bool IsFinalStep { get; set; }

    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    public virtual Role Role { get; set; } = null!;

    public virtual Workflow Workflow { get; set; } = null!;
}
