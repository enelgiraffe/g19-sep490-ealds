using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class Approval
{
    public int ApprovalId { get; set; }

    public int StepId { get; set; }

    public int AssetRequestId { get; set; }

    public int Decision { get; set; }

    public DateTime DecisionDate { get; set; }

    public int ApprovedUserId { get; set; }

    public int ApprovedRoleId { get; set; }

    public string? Comment { get; set; }

    public virtual Role ApprovedRole { get; set; } = null!;

    public virtual User ApprovedUser { get; set; } = null!;

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual WorkflowStep Step { get; set; } = null!;
}
