using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTO.ResponseDTO;

namespace g19_sep490_ealds.Server.Mappers;

public interface IAssetCapitalizationMapper
{
    AssetCapitalization ToEntity(int assetId, string? note, int UserId);

    AssetCapitalizationResponseDTO ToResponse(AssetCapitalization entity);
}