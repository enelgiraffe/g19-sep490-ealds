namespace g19_sep490_ealds.Server.DTOs.Inventory;

public class CreateInventorySessionDTO
{
    public string Purpose { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DepartmentId { get; set; }
    public int AssetCategoryId { get; set; }
    public int AssetTypeId { get; set; }
    public int CreatedBy { get; set; }
}

public class SubmitInventoryTaskDTO
{
    public bool IsFound { get; set; }
    public string ActualCondition { get; set; } = null!;
    public int ActualDepartmentId { get; set; }
    public int? ActualUserId { get; set; }
    public decimal? ActualValue { get; set; }
    public int CheckedBy { get; set; }
    public string? Note { get; set; }
}

public class StatusEntryPayloadDTO
{
    public string StatusKey { get; set; } = null!;
    public int ActualQty { get; set; }
}

public class SaveAssetInventoryDTO
{
    public int AssetId { get; set; }
    public List<StatusEntryPayloadDTO> StatusEntries { get; set; } = new();
    public int? ActualLocationId { get; set; }
    public int? ActualManagerId { get; set; }
    public int CheckedBy { get; set; }
}

public class UpdateInventorySessionDTO
{
    public string Purpose { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>Request payload for a director to confirm or reject a completed inventory session.</summary>
public class ReviewInventorySessionDTO
{
    public int ReviewedBy { get; set; }
    public int ReviewerRoleId { get; set; }
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// When true, discrepancies are automatically applied to the live asset data
    /// (location, assigned user, value, condition) upon confirmation.
    /// </summary>
    public bool ApplyCorrections { get; set; }
}
