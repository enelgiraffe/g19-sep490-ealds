namespace g19_sep490_ealds.Server.Models;

public partial class AssetRequestPurchaseLine
{
    public int LineId { get; set; }

    public int AssetRequestId { get; set; }

    public int LineIndex { get; set; }

    public string? ItemName { get; set; }

    public int Quantity { get; set; }

    public string? Unit { get; set; }

    public string? ModelCode { get; set; }

    public string? EstimatedPrice { get; set; }

    public int? AssetId { get; set; }

    public DateTime? CapitalizedAt { get; set; }

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual Asset? Asset { get; set; }
}
