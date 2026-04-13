namespace g19_sep490_ealds.Server.Models;

public partial class DisposalRecord
{
    public int DiposalId { get; set; }

    public int AssetInstanceId { get; set; }

    public int AssetRequestId { get; set; }

    public int DiposalMethod { get; set; }

    public decimal DiposalValue { get; set; }

    public DateTime DiposalDate { get; set; }

    public string? Reason { get; set; }

    public int ExecutedBy { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual User ExecutedByNavigation { get; set; } = null!;
}