using g19_sep490_ealds.Server.DTO.RequestDTO.AssetType;
using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Mappers;

public interface IAssetTypeMapper
{
    // request => Entity(DTO)

    AssetType CreateToEntity(AssetTypeCreateDTO create);
    AssetType UpdateToEntity(AssetTypeUpdateDTO update);
    AssetType DeleteToEntity(AssetTypeDeleteDTO delete);

    // Entity(DTO) => Response
    AssetTypeResponseDTO EntityToResponse(AssetType entity);
    IEnumerable<AssetTypeResponseDTO> ListEntityToResponse(IEnumerable<AssetType> entities);
}