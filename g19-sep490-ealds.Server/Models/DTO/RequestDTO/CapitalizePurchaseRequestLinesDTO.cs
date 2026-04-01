using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.Models.DTO.RequestDTO;

public class CapitalizePurchaseRequestLinesDTO
{
    [Required]
    public int AssetRequestId { get; set; }

    [Required]
    public int WarehouseId { get; set; }

    [Required]
    public int AssetTypeId { get; set; }

    /// <summary>Defaults to the purchase request create date when omitted.</summary>
    public DateOnly? PurchaseDate { get; set; }
    public string? AssetSpecification { get; set; }
    public string? AssetNote { get; set; }

    public string? Note { get; set; }

    public List<CapitalizationDocumentInputDTO>? Documents { get; set; }

    [Required]
    public List<PurchaseLineCapitalizeInputDTO> Lines { get; set; } = new();
}

public class PurchaseLineCapitalizeInputDTO
{
    [Required]
    public int LineId { get; set; }

    /// <summary>Prefix for generated catalog <see cref="Asset.Code"/> (e.g. TS → TS01).</summary>
    [Required]
    public string AssetCatalogPrefix { get; set; } = string.Empty;

    /// <summary>Required when line quantity &gt; 1; prefix for instance codes (e.g. LAP → LAP01).</summary>
    public string? InstanceCodePrefix { get; set; }
}
