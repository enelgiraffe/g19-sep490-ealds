using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("Guarantee")]
public partial class Guarantee
{
    [Key]
    public int GuaranteeId { get; set; }

    public int AssetInstanceId { get; set; }

    public int WarrantyPeriodValue { get; set; }

    [StringLength(20)]
    public string WarrantyPeriodUnit { get; set; } = null!;

    public string? WarrantyConditions { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly WarrantyEndDate { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("Guarantees")]
    public virtual AssetInstance AssetInstance { get; set; } = null!;
}
