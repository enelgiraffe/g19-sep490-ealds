using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Mappers.Implementation;

public class AssetCapitalizationMapper : IAssetCapitalizationMapper
{
    public AssetCapitalization ToEntity(int assetInstanceId, string? note, int UserId)
    {
        AssetCapitalization assetCap = new AssetCapitalization();
        assetCap.AssetInstanceId = assetInstanceId;
        assetCap.CapitalizedDate = DateTime.UtcNow.AddHours(7);
        assetCap.CapitalizedBy = UserId;
        assetCap.Note = note;
        assetCap.CreateDate = DateTime.UtcNow.AddHours(7);
        return assetCap;
    }

    public AssetCapitalizationResponseDTO ToResponse(AssetCapitalization entity, int catalogAssetId)
    {
        AssetCapitalizationResponseDTO response = new AssetCapitalizationResponseDTO();
        response.AssetInstanceId = entity.AssetInstanceId;
        response.AssetId = catalogAssetId;
        response.CapitalizedDate = entity.CapitalizedDate;
        response.CapitalizedBy = entity.CapitalizedBy;
        response.Note = entity.Note;
        return response;
    }
}
