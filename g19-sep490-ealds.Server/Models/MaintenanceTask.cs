using g19_sep490_ealds.Server.Utils.EnumsStatus;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class MaintenanceTask
{
    public int TaskId { get; set; }

    public int? ScheduleId { get; set; }

    public int? AssetRequestId { get; set; }

    public int AssetInstanceId { get; set; }

    public DateTime PlannedDate { get; set; }

    public int AssignTo { get; set; }

    public int? PerformerUserId { get; set; }

    public string? Address { get; set; }

    public string? LocationType { get; set; }

    public int Status { get; set; }

    public DateTime? ExpectedCompletionDate { get; set; }

    public string? MaintenanceProvider { get; set; }

    public string? MaintenanceContent { get; set; }

    public DateTime CreateDate { get; set; }

    public int CreateBy { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual AssetRequest? AssetRequest { get; set; }

    public virtual User AssignToNavigation { get; set; } = null!;

    public virtual User CreateByNavigation { get; set; } = null!;

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    public virtual User? PerformerUser { get; set; }

    public virtual MaintenanceSchedule? Schedule { get; set; }
    [NotMapped]
    public MaintenanceTaskStatus StatusEnum
    {
        get => (MaintenanceTaskStatus)Status;
        set => Status = (int)value;
    }
}
