using g19_sep490_ealds.Server.Utils.EnumsStatus;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("MaintenanceSchedule")]
public partial class MaintenanceSchedule
{
    [Key]
    public int ScheduleId { get; set; }

    public int AssetInstanceId { get; set; }

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

    public bool? IsActive { get; set; }

    public int CreateBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    public string? Content { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("MaintenanceSchedules")]
    public virtual AssetInstance AssetInstance { get; set; } = null!;

    [InverseProperty("Schedule")]
    public virtual ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();

    [ForeignKey("TemplateId")]
    [InverseProperty("MaintenanceSchedules")]
    public virtual MaintenanceTemplate Template { get; set; } = null!;

    [NotMapped]
    public ScheduleType ScheduleTypeEnum
    {
        get => (ScheduleType)ScheduleType;
        set => ScheduleType = (int)value;
    }

    [NotMapped]
    public MaintenanceRepeatIntervalUnit? IntervalUnitEnum
    {
        get => IntervalUnit.HasValue
            ? (MaintenanceRepeatIntervalUnit)IntervalUnit.Value
            : null;

        set => IntervalUnit = value.HasValue ? (int)value.Value : null;
    }
}