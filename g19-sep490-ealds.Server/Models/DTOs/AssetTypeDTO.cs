using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class AssetTypeResponseDTO
{
    public int AssetTypeId { get; set; }
    public string Name { get; set; } = null!;

    public static AssetTypeResponseDTO FromEntity(AssetType entity)
    {
        return new AssetTypeResponseDTO
        {
            AssetTypeId = entity.AssetTypeId,
            Name = entity.Name
        };
    }
}

