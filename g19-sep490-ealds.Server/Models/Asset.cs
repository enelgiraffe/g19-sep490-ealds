using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("Asset")]
[Index("Code", Name = "UQ__Asset__A25C5AA75E51F3B6", IsUnique = true)]
public partial class Asset
{
    [Key]
    public int AssetId { get; set; }

    [StringLength(100)]
    public string Code { get; set; } = null!;

    [StringLength(255)]
    public string Name { get; set; } = null!;

    public int AssetTypeId { get; set; }

    public DateOnly PurchaseDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OriginalPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CurrentValue { get; set; }

    public int Status { get; set; }

    public DateOnly? WarrantyEndDate { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly? InUseDate { get; set; }

    [StringLength(50)]
    public string Unit { get; set; } = null!;

    public int Quantity { get; set; }

    public int WarehouseId { get; set; }

    [InverseProperty("Asset")]
    public virtual ICollection<AssetCapitalization> AssetCapitalizations { get; set; } = new List<AssetCapitalization>();

    [InverseProperty("Asset")]
    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    [InverseProperty("Asset")]
    public virtual ICollection<AssetLocation> AssetLocations { get; set; } = new List<AssetLocation>();

    [InverseProperty("Asset")]
    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    [ForeignKey("AssetTypeId")]
    [InverseProperty("Assets")]
    public virtual AssetType AssetType { get; set; } = null!;

    [InverseProperty("Asset")]
    public virtual ICollection<AssetUsage> AssetUsages { get; set; } = new List<AssetUsage>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("Assets")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Asset")]
    public virtual ICollection<DiposalRecord> DiposalRecords { get; set; } = new List<DiposalRecord>();

    [InverseProperty("Asset")]
    public virtual ICollection<DrepreciationRecord> DrepreciationRecords { get; set; } = new List<DrepreciationRecord>();

    [InverseProperty("Asset")]
    public virtual ICollection<InventoryTask> InventoryTasks { get; set; } = new List<InventoryTask>();

    [InverseProperty("Asset")]
    public virtual ICollection<MaintenaceTask> MaintenaceTasks { get; set; } = new List<MaintenaceTask>();

    [InverseProperty("Asset")]
    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();

    [InverseProperty("Asset")]
    public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();

    [InverseProperty("Asset")]
    public virtual ICollection<TransferRecord> TransferRecords { get; set; } = new List<TransferRecord>();

    // DB hiện tại của bảng MaintenanceRecord không có cột AssetId,
    // nên tránh map quan hệ Asset - MaintenanceRecord thông qua EF.
    [NotMapped]
    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    [ForeignKey("WarehouseId")]
    [InverseProperty("Assets")]
    public virtual WarehouseAsset Warehouse { get; set; } = null!;

    [NotMapped]
    public AssetStatus StatusEnum
    {
        get => (AssetStatus)Status;
        set => Status = (int)value;
    }
}