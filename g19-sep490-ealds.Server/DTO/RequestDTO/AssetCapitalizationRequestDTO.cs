using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTO.RequestDTO;
public class AssetCapitalizationRequestDTO
{
    public int AssetId { get; set; }
    public string? Note { get; set; }
    public int? AssetRequestId { get; set; }
    public List<CapitalizationDocumentInputDTO>? Documents { get; set; }
}