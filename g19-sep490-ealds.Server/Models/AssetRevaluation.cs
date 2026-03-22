using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetRevaluation")]
public partial class AssetRevaluation
{
    [Key]
    public int Id { get; set; }

    public int AssetId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OldValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal NewValue { get; set; }

    public DateTime EffectiveDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("AssetRevaluations")]
    public virtual Asset Asset { get; set; } = null!;
}