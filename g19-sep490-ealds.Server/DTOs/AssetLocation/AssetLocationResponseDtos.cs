namespace g19_sep490_ealds.Server.DTOs.AssetLocation;

public class AssetLocationResponseDto
{
    public int LocationId { get; set; }

    /// <summary>Physical row this location belongs to.</summary>
    public int AssetInstanceId { get; set; }

    /// <summary>Catalog asset id (from the related asset instance).</summary>
    public int AssetId { get; set; }

    public string InstanceCode { get; set; } = string.Empty;

    public string AssetName { get; set; } = string.Empty;

    public string AssetCode { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? Note { get; set; }
}
