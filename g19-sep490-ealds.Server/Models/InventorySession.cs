using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("InventorySession")]
public partial class InventorySession
{
    [Key]
    public int SessionId { get; set; }

    [StringLength(50)]
    public string Code { get; set; } = null!;

    [StringLength(200)]
    public string Purpose { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime EndDate { get; set; }

    public int DepartmentId { get; set; }

    public int AssetCategoryId { get; set; }

    public int AssetTypeId { get; set; }

    public int Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreateDate { get; set; }

    public int? ProgressPercent { get; set; }

    public int CreatedBy { get; set; }

    public bool IsPeriodic { get; set; }

    public int? PeriodDays { get; set; }

    [ForeignKey("AssetCategoryId")]
    [InverseProperty("InventorySessions")]
    public virtual AssetCategory AssetCategory { get; set; } = null!;

    [ForeignKey("AssetTypeId")]
    [InverseProperty("InventorySessions")]
    public virtual AssetType AssetType { get; set; } = null!;

    [ForeignKey("DepartmentId")]
    [InverseProperty("InventorySessions")]
    public virtual Department Department { get; set; } = null!;
}