namespace g19_sep490_ealds.Server.Models;

public partial class AssetLocation
{
    public int LocationId { get; set; }

    public int AssetInstanceId { get; set; }

    public int DepartmentId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? Note { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyActualLocations { get; set; } = new List<InventoryDiscrepancy>();

    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyBookLocations { get; set; } = new List<InventoryDiscrepancy>();

    public virtual ICollection<InventoryRecord> InventoryRecords { get; set; } = new List<InventoryRecord>();

    public virtual ICollection<TransferRecord> TransferRecordFromLocations { get; set; } = new List<TransferRecord>();

    public virtual ICollection<TransferRecord> TransferRecordToLocations { get; set; } = new List<TransferRecord>();
}
