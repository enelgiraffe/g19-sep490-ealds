namespace g19_sep490_ealds.Server.DTO.ResponseDTO;

public class AssetCapitalizationResponseDTO
{
    public int AssetInstanceId { get; set; }

    public int AssetId { get; set; }

    public DateTime CapitalizedDate { get; set; }

    public int? CapitalizedBy { get; set; }

    public string? Note { get; set; }
}
