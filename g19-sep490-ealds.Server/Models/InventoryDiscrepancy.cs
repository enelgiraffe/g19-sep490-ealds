using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("InventoryDiscrepancy")]
public partial class InventoryDiscrepancy
{
    [Key]
    public int DiscrepancyId { get; set; }

    public int TaskId { get; set; }

    public int DiscrepancyType { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal BookValue { get; set; }

    public int BookLocationId { get; set; }

    public int? BookUserId { get; set; }

    public string BookCondition { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ActualValue { get; set; }

    public int ActualLocationId { get; set; }

    public int? ActualUserId { get; set; }

    public string ActualCondition { get; set; } = null!;

    [ForeignKey("ActualLocationId")]
    [InverseProperty("InventoryDiscrepancyActualLocations")]
    public virtual AssetLocation ActualLocation { get; set; } = null!;

    [ForeignKey("ActualUserId")]
    [InverseProperty("InventoryDiscrepancyActualUsers")]
    public virtual User? ActualUser { get; set; }

    [ForeignKey("BookLocationId")]
    [InverseProperty("InventoryDiscrepancyBookLocations")]
    public virtual AssetLocation BookLocation { get; set; } = null!;

    [ForeignKey("BookUserId")]
    [InverseProperty("InventoryDiscrepancyBookUsers")]
    public virtual User? BookUser { get; set; }

    [ForeignKey("TaskId")]
    [InverseProperty("InventoryDiscrepancies")]
    public virtual InventoryTask Task { get; set; } = null!;
}