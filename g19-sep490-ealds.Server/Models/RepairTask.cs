using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class RepairTask
{
    public int TaskId { get; set; }

    public int AssetRequestId { get; set; }

    public int AssetId { get; set; }

    public decimal EstimatedCost { get; set; }

    public string Reason { get; set; } = null!;

    public int Status { get; set; }

    // Fields populated when repair is started (nullable until then)
    [Column(TypeName = "datetime")]
    public DateTime? RepairDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ExpectedCompletionDate { get; set; }

    public string? RepairProgressStatus { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("RepairTasks")]
    public virtual Asset Asset { get; set; } = null!;

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual ICollection<RepairRecord> RepairRecords { get; set; } = new List<RepairRecord>();
}
