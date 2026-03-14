namespace g19_sep490_ealds.Server.DTO.ResponseDTO;

public class AssetTypeResponseDTO
{
    public int AssetTypeId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; }

    public AssetTypeResponseDTO()
    {
    }

    public AssetTypeResponseDTO(int assetTypeId, int categoryId, string name)
    {
        AssetTypeId = assetTypeId;
        CategoryId = categoryId;
        Name = name;
    }
}