using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetLocation")]
public partial class AssetLocation
{
    [Key]
    public int LocationId { get; set; }

    public int AssetInstanceId { get; set; }

    public int DepartmentId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("AssetLocations")]
    public virtual AssetInstance AssetInstance { get; set; } = null!;

    [ForeignKey("DepartmentId")]
    [InverseProperty("AssetLocations")]
    public virtual Department Department { get; set; } = null!;

    [InverseProperty("FromLocation")]
    public virtual ICollection<TransferRecord> TransferRecordsFrom { get; set; } = new List<TransferRecord>();

    [InverseProperty("ToLocation")]
    public virtual ICollection<TransferRecord> TransferRecordsTo { get; set; } = new List<TransferRecord>();

    [InverseProperty("ActualLocation")]
    public virtual ICollection<InventoryRecord> InventoryRecords { get; set; } = new List<InventoryRecord>();

    [InverseProperty("ActualLocation")]
    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyActualLocations { get; set; } = new List<InventoryDiscrepancy>();

    [InverseProperty("BookLocation")]
    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyBookLocations { get; set; } = new List<InventoryDiscrepancy>();
}