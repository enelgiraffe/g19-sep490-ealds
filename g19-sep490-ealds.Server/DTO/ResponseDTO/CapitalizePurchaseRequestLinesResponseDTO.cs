namespace g19_sep490_ealds.Server.DTO.ResponseDTO;

public class CapitalizePurchaseRequestLinesResponseDTO
{
    public int AssetRequestId { get; set; }

    public int Status { get; set; }

    public List<AssetCapitalizationResponseDTO> CapitalizedInstances { get; set; } = new();
}
