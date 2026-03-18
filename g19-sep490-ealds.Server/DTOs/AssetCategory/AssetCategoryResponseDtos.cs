namespace g19_sep490_ealds.Server.DTOs.AssetCategory;

public class AssetCategoryResponseDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AssetTypeCount { get; set; }
}

public class AssetCategoryDetailDto : AssetCategoryResponseDto
{
    public IEnumerable<AssetTypeInCategoryDto> AssetTypes { get; set; } = [];
}

public class AssetTypeInCategoryDto
{
    public int AssetTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AssetCount { get; set; }
}
