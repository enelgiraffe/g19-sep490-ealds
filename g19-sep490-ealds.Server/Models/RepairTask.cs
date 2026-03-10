using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("RepairTask")]
public partial class RepairTask
{
    [Key]
    public int TaskId { get; set; }

    public int AssetRequestId { get; set; }

    public int AssetId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal EstimatedCost { get; set; }

    public string Reason { get; set; } = null!;

    public int Status { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("RepairTasks")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("AssetRequestId")]
    [InverseProperty("RepairTasks")]
    public virtual AssetRequest AssetRequest { get; set; } = null!;

    [InverseProperty("Task")]
    public virtual ICollection<RepairRecord> RepairRecords { get; set; } = new List<RepairRecord>();
}
