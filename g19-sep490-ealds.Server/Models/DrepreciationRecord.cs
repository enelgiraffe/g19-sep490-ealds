using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("DrepreciationRecord")]
public partial class DrepreciationRecord
{
    [Key]
    public int RecordId { get; set; }

    public int AssetId { get; set; }

    public int PolicyId { get; set; }

    public DateOnly Period { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DepreciationAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AccumulatedDepreciation { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RemainingValue { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("DrepreciationRecords")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("PolicyId")]
    [InverseProperty("DrepreciationRecords")]
    public virtual DepreciationPolicy Policy { get; set; } = null!;
}