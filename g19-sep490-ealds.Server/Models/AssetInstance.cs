using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetInstance")]
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

    /// <summary>1=Sẵn dùng, 2=Đang sử dụng, 3=Bảo trì, 4=Sửa chữa, 5=Thanh lý</summary>
    public int Status { get; set; }

    public DateOnly? InUseDate { get; set; }

    public DateOnly PurchaseDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OriginalPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CurrentValue { get; set; }

    public int? SupplierId { get; set; }

    [StringLength(100)]
    public string? ContractNo { get; set; }

    [StringLength(255)]
    public string? Condition { get; set; }

    public string? Note { get; set; }

    // Navigation properties
    [ForeignKey("AssetId")]
    [InverseProperty("AssetInstances")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("WarehouseId")]
    [InverseProperty("AssetInstances")]
    public virtual Warehouse Warehouse { get; set; } = null!;

    [ForeignKey("DepreciationPolicyId")]
    [InverseProperty("AssetInstances")]
    public virtual DepreciationPolicy? DepreciationPolicy { get; set; }

    [ForeignKey("SupplierId")]
    [InverseProperty("AssetInstances")]
    public virtual Supplier? Supplier { get; set; }

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetLocation> AssetLocations { get; set; } = new List<AssetLocation>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetUsage> AssetUsages { get; set; } = new List<AssetUsage>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<TransferRecord> TransferRecords { get; set; } = new List<TransferRecord>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<DisposalRecord> DisposalRecords { get; set; } = new List<DisposalRecord>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<DepreciationRecord> DepreciationRecords { get; set; } = new List<DepreciationRecord>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<InventoryTask> InventoryTasks { get; set; } = new List<InventoryTask>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<Guarantee> Guarantees { get; set; } = new List<Guarantee>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetCapitalization> AssetCapitalizations { get; set; } = new List<AssetCapitalization>();

    [InverseProperty("AssetInstance")]
    public virtual ICollection<AssetRevaluation> AssetRevaluations { get; set; } = new List<AssetRevaluation>();
}
