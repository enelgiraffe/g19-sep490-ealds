using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("DepreciationRecord")]
public partial class DepreciationRecord
{
    [Key]
    public int RecordId { get; set; }

    public int AssetInstanceId { get; set; }

    public int PolicyId { get; set; }

    public DateOnly Period { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DepreciationAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OriginalValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RemainingValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AccumulatedDepreciation { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    public bool? IsLocked { get; set; }

    public bool IsPosted { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("DepreciationRecords")]
    public virtual AssetInstance AssetInstance { get; set; } = null!;

    [ForeignKey("PolicyId")]
    [InverseProperty("DepreciationRecords")]
    public virtual DepreciationPolicy Policy { get; set; } = null!;
}
