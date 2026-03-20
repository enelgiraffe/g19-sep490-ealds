using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Models;

[Table("MaintenanceRecord")]
public partial class MaintenanceRecord
{
    [Key]
    public int RecordId { get; set; }

    public int TaskId { get; set; }
    public int AssetId { get; set; }

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


    [ForeignKey("AssetId")]
    [InverseProperty("MaintenanceRecords")]
    public virtual Asset Asset { get; set; } = null!;

    [NotMapped]
    public MaintenanceRecordStatus StatusEnum
    {
        get => (MaintenanceRecordStatus)Status;
        set => Status = (int)value;
    }
}
