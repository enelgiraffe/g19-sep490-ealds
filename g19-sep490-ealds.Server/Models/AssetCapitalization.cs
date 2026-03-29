using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetCapitalization")]
public partial class AssetCapitalization
{
    [Key]
    public int Id { get; set; }

    public int AssetInstanceId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CapitalizedDate { get; set; }

    public int CapitalizedBy { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("AssetCapitalizations")]
    public virtual AssetInstance AssetInstance { get; set; } = null!;

    [ForeignKey("CapitalizedBy")]
    [InverseProperty("AssetCapitalizations")]
    public virtual User CapitalizedByNavigation { get; set; } = null!;
}
