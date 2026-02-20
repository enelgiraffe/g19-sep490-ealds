using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("DiposalRecord")]
public partial class DiposalRecord
{
    [Key]
    public int DiposalId { get; set; }

    public int AssetRequestId { get; set; }

    public int AssetId { get; set; }

    public int DiposalMethod { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiposalValue { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DiposalDate { get; set; }

    public string? Reason { get; set; }

    public int ExecutedBy { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("DiposalRecords")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("AssetRequestId")]
    [InverseProperty("DiposalRecords")]
    public virtual AssetRequest AssetRequest { get; set; } = null!;

    [ForeignKey("ExecutedBy")]
    [InverseProperty("DiposalRecords")]
    public virtual User ExecutedByNavigation { get; set; } = null!;
}