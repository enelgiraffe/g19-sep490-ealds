using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTO.RequestDTO;

public class AssetCapitalizationRequestDTO
{
    /// <summary>When set, capitalization applies to this physical instance.</summary>
    public int? AssetInstanceId { get; set; }

    /// <summary>Catalog asset id; used when no <see cref="AssetInstanceId"/> (first instance is chosen).</summary>
    public int? AssetId { get; set; }

    public string? Note { get; set; }

    public int? AssetRequestId { get; set; }

    public List<CapitalizationDocumentInputDTO>? Documents { get; set; }
}
