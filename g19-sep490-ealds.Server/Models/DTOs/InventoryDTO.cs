namespace g19_sep490_ealds.Server.Models.DTOs;

// ── Request DTOs ──────────────────────────────────────────────────────────────

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

// ── Response DTOs ─────────────────────────────────────────────────────────────

public class InventorySessionListItemDTO
{
    public int SessionId { get; set; }
    public string Code { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public string AssetCategoryName { get; set; } = null!;
    public string AssetTypeName { get; set; } = null!;
    public int Status { get; set; }
    public string StatusName { get; set; } = null!;
    public int? ProgressPercent { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public DateTime? CreateDate { get; set; }
}

public class InventorySessionDetailDTO : InventorySessionListItemDTO
{
    public List<InventoryTaskDTO> Tasks { get; set; } = new();
}

public class InventoryTaskDTO
{
    public int TaskId { get; set; }
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = null!;
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
    public string? BookDepartmentName { get; set; }
    public int? BookUserId { get; set; }
    public string? BookUserName { get; set; }
    public string BookCondition { get; set; } = null!;
    public decimal ActualValue { get; set; }
    public string? ActualDepartmentName { get; set; }
    public int? ActualUserId { get; set; }
    public string? ActualUserName { get; set; }
    public string ActualCondition { get; set; } = null!;
}

public class DropdownItemDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}
