using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("MaintenanceSchedule")]
public partial class MaintenanceSchedule
{
    [Key]
    public int ScheduleId { get; set; }

    public int AssetId { get; set; }

    public int TemplateId { get; set; }

    public int ScheduleType { get; set; }

    public int? IntervalMonths { get; set; }

    public int? IntervalHours { get; set; }

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

    [ForeignKey("AssetId")]
    [InverseProperty("MaintenanceSchedules")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("CreateBy")]
    [InverseProperty("MaintenanceSchedules")]
    public virtual User CreateByNavigation { get; set; } = null!;

    [InverseProperty("Schedule")]
    public virtual ICollection<MaintenaceTask> MaintenaceTasks { get; set; } = new List<MaintenaceTask>();

    [ForeignKey("TemplateId")]
    [InverseProperty("MaintenanceSchedules")]
    public virtual MaintenanceTemplate Template { get; set; } = null!;
}