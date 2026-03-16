namespace g19_sep490_ealds.Server.DTOs.AssetLocation;

public class AssetLocationResponseDto
{
    public int LocationId { get; set; }
    public int AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Note { get; set; }
}
