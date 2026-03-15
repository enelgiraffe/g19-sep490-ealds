namespace g19_sep490_ealds.Server.DTOs.AssetType;

public class AssetTypeResponseDto
{
    public int AssetTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int AssetCount { get; set; }
}

public class AssetTypeDetailDto : AssetTypeResponseDto
{
    public int InventorySessionCount { get; set; }
    public int MaintenanceTemplateCount { get; set; }
}
