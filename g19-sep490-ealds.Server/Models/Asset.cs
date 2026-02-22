using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class Asset
{
    public int AssetId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int AssetTypeId { get; set; }

    public DateOnly PurchaseDate { get; set; }

    public decimal OriginalPrice { get; set; }

    public decimal CurrentValue { get; set; }

    public int Status { get; set; }

    public DateOnly? WarrantyEndDate { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly? InUseDate { get; set; }

    public string Unit { get; set; } = null!;

    public int Quantity { get; set; }

    public int WarehouseId { get; set; }

    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    public virtual ICollection<AssetLocation> AssetLocations { get; set; } = new List<AssetLocation>();

    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual ICollection<AssetUsage> AssetUsages { get; set; } = new List<AssetUsage>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<DiposalRecord> DiposalRecords { get; set; } = new List<DiposalRecord>();

    public virtual ICollection<DrepreciationRecord> DrepreciationRecords { get; set; } = new List<DrepreciationRecord>();

    public virtual ICollection<InventoryTask> InventoryTasks { get; set; } = new List<InventoryTask>();

    public virtual ICollection<MaintenaceTask> MaintenaceTasks { get; set; } = new List<MaintenaceTask>();

    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();

    public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();

    public virtual ICollection<TransferRecord> TransferRecords { get; set; } = new List<TransferRecord>();

    public virtual WarehouseAsset Warehouse { get; set; } = null!;
}
