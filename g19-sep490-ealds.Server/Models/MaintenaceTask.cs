using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class MaintenaceTask
{
    public int TaskId { get; set; }

    public int? ScheduleId { get; set; }

    public int? AssetRequestId { get; set; }

    public int AssetId { get; set; }

    public DateTime PlannedDate { get; set; }

    public int AssignTo { get; set; }

    public string? Address { get; set; }

    public int Status { get; set; }

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

    public virtual AssetRequest? AssetRequest { get; set; }

    public virtual User AssignToNavigation { get; set; } = null!;

    public virtual User CreateByNavigation { get; set; } = null!;

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    public virtual MaintenanceSchedule? Schedule { get; set; }
}
