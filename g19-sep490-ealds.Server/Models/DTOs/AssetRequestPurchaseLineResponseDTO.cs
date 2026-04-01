namespace g19_sep490_ealds.Server.Models.DTOs;

public class AssetRequestPurchaseLineResponseDTO
{
    public int LineId { get; set; }

    public int LineIndex { get; set; }

    public string? ItemName { get; set; }

    public int Quantity { get; set; }

    public string? Unit { get; set; }

    public string? ModelCode { get; set; }

    public string? EstimatedPrice { get; set; }

    public int? AssetId { get; set; }

    public string? AssetCode { get; set; }

    public string? AssetName { get; set; }

    public DateTime? CapitalizedAt { get; set; }
}
