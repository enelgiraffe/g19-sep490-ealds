using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace g19_sep490_ealds.Server.Mappers.Implementation;

public class AssetTypeMapper : IAssetTypeMapper
{
    public AssetType CreateToEntity(AssetTypeCreateDTO create)
    {
        AssetType type = new AssetType();
        type.CategoryId = create.CategoryId;
        type.Name = create.Name;
        return type;

    }

    public AssetType DeleteToEntity(AssetTypeDeleteDTO delete)
    {
        AssetType type = new AssetType();
        type.AssetTypeId = delete.AssetTypeId;
        type.CategoryId = delete.CategoryId;
        type.Name = delete.Name;
        return type;
    }

    public AssetTypeResponseDTO EntityToResponse(AssetType entity)
    {
        AssetTypeResponseDTO response = new AssetTypeResponseDTO();
        response.AssetTypeId = entity.AssetTypeId;
        response.CategoryId = entity.CategoryId;
        response.Name = entity.Name;
        return response;
    }

    public IEnumerable<AssetTypeResponseDTO> ListEntityToResponse(IEnumerable<AssetType> entities)
    {
        return entities.Select(x => EntityToResponse(x)).ToList();
    }

    public AssetType UpdateToEntity(AssetTypeUpdateDTO update)
    {
        AssetType type = new AssetType();
        type.AssetTypeId = update.AssetTypeId;
        type.CategoryId = update.CategoryId;
        type.Name = update.Name;
        return type;
    }
}
