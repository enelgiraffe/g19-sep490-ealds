using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Capitalization;

public class AssetCapitalizationFromRequestDTO
{
    [Required]
    public int AssetRequestId { get; set; }

    public string? Note { get; set; }

    // Fields used when creating asset from purchase request
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int AssetTypeId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int WarehouseId { get; set; }
    public string? AssetSpecification { get; set; }
    public string? AssetNote { get; set; }

    public List<CapitalizationDocumentInputDTO>? Documents { get; set; }
}

