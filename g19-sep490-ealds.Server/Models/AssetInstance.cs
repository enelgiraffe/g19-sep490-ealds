using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetInstance")]
[Index("InstanceCode", Name = "UQ__AssetIns__5850F4DDD6AA4C9E", IsUnique = true)]
public partial class AssetInstance
{
    [Key]
    public int AssetInstanceId { get; set; }

    public int AssetId { get; set; }

    public int WarehouseId { get; set; }

    public int? DepreciationPolicyId { get; set; }

    [StringLength(100)]
    public string InstanceCode { get; set; } = null!;

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    public int Status { get; set; }

    public DateOnly? InUseDate { get; set; }

    public DateOnly PurchaseDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OriginalPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CurrentValue { get; set; }

    [StringLength(255)]
    public string? Condition { get; set; }

    public string? Note { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("AssetInstances")]
    public virtual Asset Asset { get; set; } = null!;

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetCapitalization> AssetCapitalizations { get; set; } = new List<AssetCapitalization>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetLocation> AssetLocations { get; set; } = new List<AssetLocation>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetRevaluation> AssetRevaluations { get; set; } = new List<AssetRevaluation>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetUsage> AssetUsages { get; set; } = new List<AssetUsage>();

    [ForeignKey("DepreciationPolicyId")]
    [InverseProperty("AssetInstances")]
    public virtual DepreciationPolicy? DepreciationPolicy { get; set; }

    [InverseProperty("AssetInstance")]
    public virtual ICollection<DepreciationRecord> DepreciationRecords { get; set; } = new List<DepreciationRecord>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<DisposalRecord> DisposalRecords { get; set; } = new List<DisposalRecord>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<Guarantee> Guarantees { get; set; } = new List<Guarantee>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<InventoryTask> InventoryTasks { get; set; } = new List<InventoryTask>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<TransferRecord> TransferRecords { get; set; } = new List<TransferRecord>();

    [ForeignKey("WarehouseId")]
    [InverseProperty("AssetInstances")]
    public virtual Warehouse Warehouse { get; set; } = null!;

    [NotMapped]
    public AssetStatus StatusEnum
    {
        get => (AssetStatus)Status;
        set => Status = (int)value;
    }
}