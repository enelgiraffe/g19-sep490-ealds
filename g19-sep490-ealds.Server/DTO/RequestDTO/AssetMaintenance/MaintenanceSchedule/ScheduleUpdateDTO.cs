using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceSchedule;

public class ScheduleUpdateDTO
{
    public int AssetId { get; set; }

    public int TemplateId { get; set; }

    public ScheduleType ScheduleType { get; set; }

    public MaintenanceRepeatIntervalUnit? IntervalUnit { get; set; }

    public int? IntervalValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? NextDueDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }
    public int CreateBy { get; set; }
}