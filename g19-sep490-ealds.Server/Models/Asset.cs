using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("Asset")]
[Index("Code", Name = "UQ__Asset__A25C5AA76F9788D7", IsUnique = true)]
public partial class Asset
{
    [Key]
    public int AssetId { get; set; }

    [StringLength(100)]
    public string Code { get; set; } = null!;

    [StringLength(255)]
    public string Name { get; set; } = null!;

    public int AssetTypeId { get; set; }

    public int Status { get; set; }

    [StringLength(50)]
    public string Unit { get; set; } = null!;

    public int? Quantity { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly? InUseDate { get; set; }

    public int? SupplierId { get; set; }

    [StringLength(100)]
    public string? ContractNo { get; set; }

    public string? Specification { get; set; }

    public string? Note { get; set; }

    [InverseProperty("Asset")]
    public virtual ICollection<AssetInstance> AssetInstances { get; set; } = new List<AssetInstance>();

    [InverseProperty("Asset")]
    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    [ForeignKey("AssetTypeId")]
    [InverseProperty("Assets")]
    public virtual AssetType AssetType { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    [InverseProperty("Assets")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Asset")]
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    [ForeignKey("SupplierId")]
    [InverseProperty("Assets")]
    public virtual Supplier? Supplier { get; set; }

    
}
