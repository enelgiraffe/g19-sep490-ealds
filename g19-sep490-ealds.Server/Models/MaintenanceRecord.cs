using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("MaintenanceRecord")]
public partial class MaintenanceRecord
{
    [Key]
    public int RecordId { get; set; }

    public int TaskId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ExecutionDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalCost { get; set; }

    public int Status { get; set; }

    public string WorkPerformed { get; set; } = null!;

    public string ConditionBefore { get; set; } = null!;

    public string ConditionAfter { get; set; } = null!;

    public string? TechnicalNote { get; set; }

    [ForeignKey("TaskId")]
    [InverseProperty("MaintenanceRecords")]
    public virtual MaintenaceTask Task { get; set; } = null!;
}
