using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.DTOs.AssetTypes;

public class AssetTypeResponseDTO
{
    public int AssetTypeId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    public static AssetTypeResponseDTO FromEntity(AssetType entity)
    {
        return new AssetTypeResponseDTO
        {
            AssetTypeId = entity.AssetTypeId,
            CategoryId = entity.CategoryId,
            Name = entity.Name
        };
    }
}
