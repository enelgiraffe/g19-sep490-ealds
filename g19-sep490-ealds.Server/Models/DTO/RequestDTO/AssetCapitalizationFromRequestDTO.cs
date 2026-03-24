using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTO.RequestDTO;

public class CapitalizationDocumentInputDTO
{
    public string? Name { get; set; }
    public string Url { get; set; } = null!;
}

public class AssetCapitalizationFromRequestDTO
{
    public int AssetRequestId { get; set; }
    public string? Note { get; set; }
    public List<CapitalizationDocumentInputDTO>? Documents { get; set; }

    // Asset creation fields
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AssetTypeId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = null!;
    public int Quantity { get; set; }
    public int WarehouseId { get; set; }
}

