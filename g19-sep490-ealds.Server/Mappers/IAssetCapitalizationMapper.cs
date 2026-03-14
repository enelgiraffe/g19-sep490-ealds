using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Mappers;

public interface IAssetCapitalizationMapper
{
    AssetCapitalization ToEntity(int assetId, string? note, int UserId);

    AssetCapitalizationResponseDTO ToResponse(AssetCapitalization entity);
}