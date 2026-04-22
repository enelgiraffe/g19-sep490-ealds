namespace g19_sep490_ealds.Server.DTOs.Inventory;

public class CreateInventorySessionDTO
{
    public string? Purpose { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DepartmentId { get; set; }
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public int CreatedBy { get; set; }
    public bool IsPeriodic { get; set; }
    public int? PeriodDays { get; set; }
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

public class SaveAssetInventoryDTO
{
    /// <summary>Optional echo; must match the route <c>assetInstanceId</c> when set.</summary>
    public int AssetInstanceId { get; set; }

    /// <summary>On-site reported status: AssetStatus enum int (required).</summary>
    public int ActualStatus { get; set; }

    public int? ActualLocationId { get; set; }
    public int CheckedBy { get; set; }
}

public class UpdateInventorySessionDTO
{
    public string? Purpose { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    /// <summary>New recurrence interval in days. Only applied when the session is periodic.</summary>
    public int? PeriodDays { get; set; }
    /// <summary>When set and different from the stored department, pending tasks are replaced for the new department.</summary>
    public int? DepartmentId { get; set; }
}

/// <summary>Request payload for inventory session cancellation (optional review notes).</summary>
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
