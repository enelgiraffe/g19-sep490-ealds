using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("WarehouseAsset")]
public partial class WarehouseAsset
{
    [Key]
    public int WarehouseId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    [InverseProperty("Warehouse")]
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}