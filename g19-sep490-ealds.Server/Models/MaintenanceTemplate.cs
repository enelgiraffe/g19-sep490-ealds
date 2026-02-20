using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("MaintenanceTemplate")]
public partial class MaintenanceTemplate
{
    [Key]
    public int TemplateId { get; set; }

    public int AssetTypeId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int FrequencyType { get; set; }

    public int RepeatIntervalValue { get; set; }

    [StringLength(100)]
    public string RepeatIntervalUnit { get; set; } = null!;

    public bool IsActive { get; set; }

    [ForeignKey("AssetTypeId")]
    [InverseProperty("MaintenanceTemplates")]
    public virtual AssetType AssetType { get; set; } = null!;

    [InverseProperty("Template")]
    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
}