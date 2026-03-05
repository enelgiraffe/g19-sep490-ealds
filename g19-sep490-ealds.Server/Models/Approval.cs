using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("Approval")]
public partial class Approval
{
    [Key]
    public int ApprovalId { get; set; }

    public int StepId { get; set; }

    public int AssetRequestId { get; set; }

    public int Decision { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DecisionDate { get; set; }

    public int ApprovedUserId { get; set; }

    public int ApprovedRoleId { get; set; }

    public string? Comment { get; set; }

    [ForeignKey("ApprovedRoleId")]
    [InverseProperty("Approvals")]
    public virtual Role ApprovedRole { get; set; } = null!;

    [ForeignKey("ApprovedUserId")]
    [InverseProperty("Approvals")]
    public virtual User ApprovedUser { get; set; } = null!;

    [ForeignKey("AssetRequestId")]
    [InverseProperty("Approvals")]
    public virtual AssetRequest AssetRequest { get; set; } = null!;

    [ForeignKey("StepId")]
    [InverseProperty("Approvals")]
    public virtual WorkflowStep Step { get; set; } = null!;
}
