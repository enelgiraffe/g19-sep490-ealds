using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetRevaluation")]
public partial class AssetRevaluation
{
    [Key]
    public int Id { get; set; }

    public int AssetInstanceId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OldValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal NewValue { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime EffectiveDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("AssetRevaluations")]
    public virtual AssetInstance AssetInstance { get; set; } = null!;
}
