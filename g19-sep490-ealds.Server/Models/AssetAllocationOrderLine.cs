namespace g19_sep490_ealds.Server.Models;

public partial class AssetAllocationOrderLine
{
    public int AssetAllocationOrderLineId { get; set; }

    public int AssetAllocationOrderId { get; set; }

    public int AssetTypeId { get; set; }

    public int AssetId { get; set; }

    public int Quantity { get; set; }

    public string? Reason { get; set; }

    public virtual AssetAllocationOrder Order { get; set; } = null!;

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual Asset Asset { get; set; } = null!;
}
