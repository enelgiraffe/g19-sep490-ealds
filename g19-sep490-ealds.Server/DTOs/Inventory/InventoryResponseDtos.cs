namespace g19_sep490_ealds.Server.DTOs.Inventory;

public class InventorySessionListItemDTO
{
    public int SessionId { get; set; }
    public string Code { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public string? AssetCategoryName { get; set; }
    public string? AssetTypeName { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = null!;
    public int? ProgressPercent { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public DateTime? CreateDate { get; set; }
    public bool IsPeriodic { get; set; }
    public int? PeriodDays { get; set; }

    /// <summary>Discrepancy rows not yet applied to the book (ResolvedAt null). Zero when not applicable.</summary>
    public int UnresolvedDiscrepancyCount { get; set; }
}

public class InventorySessionDetailDTO : InventorySessionListItemDTO
{
    public int QuantityDiffCount { get; set; }
    public int LocationChangeCount { get; set; }
    public int DepartmentChangeCount { get; set; }
    public int ConditionChangeCount { get; set; }
    public List<InventoryTaskDTO> Tasks { get; set; } = new();
}

public class SessionAssetCheckItemDTO
{
    /// <summary>Catalog asset (master).</summary>
    public int AssetId { get; set; }

    /// <summary>Physical instance being checked in this session.</summary>
    public int AssetInstanceId { get; set; }

    /// <summary>Catalog asset code.</summary>
    public string AssetCode { get; set; } = null!;

    /// <summary>Unique code for this instance (tag/serial).</summary>
    public string InstanceCode { get; set; } = null!;

    public string AssetName { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;

    /// <summary>Book-side instance status (AssetStatus int).</summary>
    public int BookStatus { get; set; }

    /// <summary>Reported status after check; null if not yet submitted.</summary>
    public int? ActualStatus { get; set; }

    public int CheckStatus { get; set; } // 0=Chưa kiểm kê, 2=Hoàn tất

    /// <summary>True when this task has at least one inventory discrepancy row (any mismatch type).</summary>
    public bool HasDiscrepancy { get; set; }
}

public class AssetInventoryDetailDTO
{
    public int AssetId { get; set; }

    public int AssetInstanceId { get; set; }

    /// <summary>Catalog code.</summary>
    public string AssetCode { get; set; } = null!;

    public string InstanceCode { get; set; } = null!;

    public string AssetName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string TypeName { get; set; } = null!;

    /// <summary>Book-side instance status (AssetStatus int).</summary>
    public int BookStatus { get; set; }

    /// <summary>Book-side asset status enum name for display.</summary>
    public string BookAssetStatus { get; set; } = string.Empty;

    /// <summary>Last reported status after check; null if not yet saved.</summary>
    public int? ActualStatus { get; set; }

    /// <summary>AssetStatus enum name stored on the inventory record (empty if legacy / cleared).</summary>
    public string ActualCondition { get; set; } = string.Empty;

    public int? BookLocationId { get; set; }
    public string BookLocationName { get; set; } = null!;
    public int? ActualLocationId { get; set; }
    public List<DropdownItemDTO> Locations { get; set; } = new();
}

public class InventoryTaskDTO
{
    public int TaskId { get; set; }

    /// <summary>Catalog asset id.</summary>
    public int AssetId { get; set; }

    /// <summary>Physical instance id for this task.</summary>
    public int AssetInstanceId { get; set; }

    /// <summary>Catalog code.</summary>
    public string AssetCode { get; set; } = null!;

    public string InstanceCode { get; set; } = null!;

    public string AssetName { get; set; } = null!;
    public string BookCondition { get; set; } = null!;
    public int? BookDepartmentId { get; set; }
    public string? BookDepartmentName { get; set; }
    public int? BookUserId { get; set; }
    public string? BookUserName { get; set; }
    public decimal BookValue { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = null!;
    public DateTime CheckDate { get; set; }
    public string? Note { get; set; }
    public InventoryRecordDTO? Record { get; set; }
    public List<InventoryDiscrepancyDTO> Discrepancies { get; set; } = new();
}

public class InventoryRecordDTO
{
    public int RecordId { get; set; }
    public bool? IsFound { get; set; }
    public string ActualCondition { get; set; } = null!;
    public int ActualDepartmentId { get; set; }
    public string? ActualDepartmentName { get; set; }
    public int? ActualUserId { get; set; }
    public string? ActualUserName { get; set; }
    public DateTime CheckedDate { get; set; }
}

public class InventoryDiscrepancyDTO
{
    public int DiscrepancyId { get; set; }
    public int DiscrepancyType { get; set; }
    public string DiscrepancyTypeName { get; set; } = null!;
    public decimal BookValue { get; set; }
    /// <summary>Quantity on books (from asset).</summary>
    public int BookQuantity { get; set; }
    /// <summary>Quantity counted on site; null if not recorded.</summary>
    public int? ActualQuantity { get; set; }
    /// <summary>Note from department head on the inventory task.</summary>
    public string? TaskNote { get; set; }
    public string? BookDepartmentName { get; set; }
    public int? BookUserId { get; set; }
    public string? BookUserName { get; set; }
    public string BookCondition { get; set; } = null!;
    public decimal ActualValue { get; set; }
    public string? ActualDepartmentName { get; set; }
    public int? ActualUserId { get; set; }
    public string? ActualUserName { get; set; }
    public string ActualCondition { get; set; } = null!;

    /// <summary>UTC when accountant applied actuals to the book; null if still pending.</summary>
    public DateTime? ResolvedAt { get; set; }
}

public class DropdownItemDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

/// <summary>Aggregate discrepancy summary (reserved for clients; not returned by current API).</summary>
public class InventoryReviewSummaryDTO
{
    public int SessionId { get; set; }
    public string Code { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DepartmentName { get; set; } = null!;
    public string? AssetCategoryName { get; set; }
    public string? AssetTypeName { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = null!;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int? ProgressPercent { get; set; }
    public int TotalDiscrepancies { get; set; }
    public int AssetNotFoundCount { get; set; }
    public int QuantityMismatchCount { get; set; }
    public int LocationMismatchCount { get; set; }
    public int UserMismatchCount { get; set; }
    public int ValueMismatchCount { get; set; }
    public int ConditionMismatchCount { get; set; }
    public List<InventoryDiscrepancyDetailDTO> Discrepancies { get; set; } = new();
}

/// <summary>Discrepancy DTO enriched with the asset info it belongs to.</summary>
public class InventoryDiscrepancyDetailDTO : InventoryDiscrepancyDTO
{
    public int TaskId { get; set; }

    public int AssetId { get; set; }

    public int AssetInstanceId { get; set; }

    public string AssetCode { get; set; } = null!;

    public string InstanceCode { get; set; } = null!;

    public string AssetName { get; set; } = null!;
}
