using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("WorkflowStep")]
public partial class WorkflowStep
{
    [Key]
    public int StepId { get; set; }

    public int WorkflowId { get; set; }

    public int StepOrder { get; set; }

    public int RoleId { get; set; }

    public bool IsFinalStep { get; set; }

    [InverseProperty("Step")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [ForeignKey("RoleId")]
    [InverseProperty("WorkflowSteps")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("WorkflowId")]
    [InverseProperty("WorkflowSteps")]
    public virtual Workflow Workflow { get; set; } = null!;
}
