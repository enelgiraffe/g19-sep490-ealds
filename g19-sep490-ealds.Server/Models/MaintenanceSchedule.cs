using g19_sep490_ealds.Server.Utils.EnumsStatus;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;
[Table("MaintenanceSchedule")]
public partial class MaintenanceSchedule
{
    public int ScheduleId { get; set; }

    public int AssetId { get; set; }

    public int TemplateId { get; set; }

    public int ScheduleType { get; set; }

    public int? IntervalValue { get; set; }

    public int? IntervalUnit { get; set; }

    public int? IntervalMonths { get; set; }

    public int? IntervalHours { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? NextDueDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }

    public int CreateBy { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual Asset Asset { get; set; } = null!;
    [ForeignKey("CreateBy")]
    public virtual User CreateByNavigation { get; set; } = null!;

    public virtual MaintenanceTemplate Template { get; set; } = null!;

    public virtual ICollection<MaintenaceTask> MaintenaceTasks { get; set; } = new List<MaintenaceTask>();

    [NotMapped]
    public MaintenanceRepeatIntervalUnit? IntervalUnitEnum
    {
        get => IntervalUnit.HasValue
            ? (MaintenanceRepeatIntervalUnit)IntervalUnit.Value
            : null;

        set => IntervalUnit = value.HasValue ? (int)value.Value : null;
    }

    [NotMapped]
    public ScheduleType ScheduleTypeEnum
    {
        get => (ScheduleType)ScheduleType;
        set => ScheduleType = (int)value;
    }
}
