using System;
using System.Collections.Generic;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("MaintenaceTask")]
public partial class MaintenaceTask
{
    [Key]
    public int TaskId { get; set; }

    public int? ScheduleId { get; set; }

    public int? AssetRequestId { get; set; }

    public int AssetId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime PlannedDate { get; set; }

    public int AssignTo { get; set; }

    public string? Address { get; set; }

    public int Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatDate { get; set; }

    public int CreateBy { get; set; }

    // Fields populated when maintenance is started (nullable until then)
    public int? PerformerUserId { get; set; }

    public string? MaintenanceProvider { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? EstimatedCost { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ExpectedCompletionDate { get; set; }

    public string? MaintenanceContent { get; set; }

    public string? LocationType { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("MaintenaceTasks")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("AssetRequestId")]
    [InverseProperty("MaintenaceTasks")]
    public virtual AssetRequest? AssetRequest { get; set; }

    [ForeignKey("AssignTo")]
    [InverseProperty("MaintenaceTaskAssignToNavigations")]
    public virtual User AssignToNavigation { get; set; } = null!;

    [ForeignKey("CreateBy")]
    [InverseProperty("MaintenaceTaskCreateByNavigations")]
    public virtual User CreateByNavigation { get; set; } = null!;

    [InverseProperty("Task")]
    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    [ForeignKey("ScheduleId")]
    [InverseProperty("MaintenaceTasks")]
    public virtual MaintenanceSchedule? Schedule { get; set; }

    [NotMapped]
    public MaintenanceTaskStatus StatusEnum
    {
        get => (MaintenanceTaskStatus)Status;
        set => Status = (int)value;
    }
}
