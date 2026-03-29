using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("DisposalRecord")]
public partial class DisposalRecord
{
    [Key]
    public int DiposalId { get; set; }

    public int AssetInstanceId { get; set; }

    public int AssetRequestId { get; set; }

    public int DiposalMethod { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiposalValue { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DiposalDate { get; set; }

    public string? Reason { get; set; }

    public int ExecutedBy { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("DisposalRecords")]
    public virtual AssetInstance AssetInstance { get; set; } = null!;

    [ForeignKey("AssetRequestId")]
    [InverseProperty("DisposalRecords")]
    public virtual AssetRequest AssetRequest { get; set; } = null!;

    [ForeignKey("ExecutedBy")]
    [InverseProperty("DisposalRecords")]
    public virtual User ExecutedByNavigation { get; set; } = null!;
}