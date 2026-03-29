using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("MaintenanceSchedule")]
public partial class MaintenanceSchedule
{
    [Key]
    public int ScheduleId { get; set; }

    // Scope: chỉ 1 trong 2
    public int? AssetId { get; set; }
    public int? AssetInstanceId { get; set; }

    public int TemplateId { get; set; }

    public int ScheduleType { get; set; }

    public int? IntervalValue { get; set; }

    public int? IntervalUnit { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NextDueDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public int CreateBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    public string? Content { get; set; }

    // Navigation
    [ForeignKey("AssetId")]
    [InverseProperty("MaintenanceSchedules")]
    public virtual Asset? Asset { get; set; }

    [ForeignKey("AssetInstanceId")]
    public virtual AssetInstance? AssetInstance { get; set; }

    [ForeignKey("TemplateId")]
    [InverseProperty("MaintenanceSchedules")]
    public virtual MaintenanceTemplate Template { get; set; } = null!;

    [ForeignKey("CreateBy")]
    [InverseProperty("MaintenanceSchedules")]
    public virtual User CreateByNavigation { get; set; } = null!;

    [InverseProperty("Schedule")]
    public virtual ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();
}
