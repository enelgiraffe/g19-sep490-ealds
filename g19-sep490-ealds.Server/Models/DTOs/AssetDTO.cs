using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Models.DTOs;

/// <summary>
/// DTO for creating an asset with General info and Depreciation settings.
/// </summary>
public class CreateAssetDTO
{
    // General info
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AssetTypeId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public DateOnly? InUseDate { get; set; }
    public string Unit { get; set; } = null!;
    public int Quantity { get; set; }
    public int WarehouseId { get; set; }
    public int CreatedBy { get; set; }

    // Depreciation settings (optional - links asset to a depreciation policy)
    public int? DepreciationPolicyId { get; set; }

    /// <summary>Optional assignment (requires ACCOUNTANT). Department and/or responsible employee.</summary>
    public int? AssignedDepartmentId { get; set; }

    /// <summary>Optional custodian; must belong to <see cref="AssignedDepartmentId"/> when both are set.</summary>
    public int? ResponsibleEmployeeId { get; set; }

    /// <summary>Start date for location/usage rows; defaults to UTC today.</summary>
    public DateOnly? AssignmentEffectiveDate { get; set; }
}

/// <summary>
/// DTO for updating asset data and status.
/// </summary>
public class UpdateAssetDTO
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? AssetTypeId { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? CurrentValue { get; set; }
    public AssetStatus? Status { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public DateOnly? InUseDate { get; set; }
    public string? Unit { get; set; }
    public int? Quantity { get; set; }
    public int? WarehouseId { get; set; }

    // Depreciation policy (can be linked/unlinked on update)
    public int? DepreciationPolicyId { get; set; }

    /// <summary>Set or change current department (requires ACCOUNTANT). Closes the previous current location.</summary>
    public int? AssignedDepartmentId { get; set; }

    /// <summary>Set or change responsible employee (requires ACCOUNTANT). Updates location to this employee's department when used alone.</summary>
    public int? ResponsibleEmployeeId { get; set; }

    /// <summary>Effective date for assignment changes; defaults to UTC today.</summary>
    public DateOnly? AssignmentEffectiveDate { get; set; }

    /// <summary>Close the current department assignment without adding a new one.</summary>
    public bool ClearDepartmentAssignment { get; set; }

    /// <summary>Close the current responsible-employee assignment without adding a new one.</summary>
    public bool ClearResponsibleEmployee { get; set; }
}

/// <summary>
/// DTO for accountant-only status change (<c>PUT /api/assets/{id}/status</c>).
/// </summary>
public class ChangeAssetStatusDTO
{
    public AssetStatus Status { get; set; }

    /// <summary>Optional note for audit / client display (not persisted unless you extend the model).</summary>
    public string? Note { get; set; }
}

/// <summary>
/// DTO for delete: sets asset status to Disposed, Lost, or Liquidated.
/// </summary>
public class DeleteAssetDTO
{
    public AssetStatus Status { get; set; } // Disposed, Lost, or Liquidated
    public string? Reason { get; set; }
}

public class MaintenanceScheduleDTO
{
    public int ScheduleId { get; set; }
    public int? TemplateId { get; set; }
    public string? Content { get; set; }
    public string? TemplateName { get; set; }
    public int ScheduleType { get; set; }
    public int? IntervalMonths { get; set; }
    public int? IntervalHours { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? NextDueDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsActive { get; set; }
}

public class AssetDocumentDTO
{
    public int DocumentId { get; set; }
    public int DocumentType { get; set; }
    public string FileUrl { get; set; } = null!;
    public DateTime UploadedDate { get; set; }
}

/// <summary>
/// Response DTO for asset read operations.
/// </summary>
public class AssetResponseDTO
{
    public int AssetId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AssetTypeId { get; set; }
    public string? AssetTypeName { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public AssetStatus Status { get; set; }
    public string StatusName { get; set; } = null!;
    public DateOnly? WarrantyEndDate { get; set; }
    public DateOnly? InUseDate { get; set; }
    public string Unit { get; set; } = null!;
    public int Quantity { get; set; }
    public int WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int CreatedBy { get; set; }

    /// <summary>
    /// Phòng ban hiện tại đang sử dụng tài sản (nếu có bản ghi AssetLocation IsCurrent).
    /// </summary>
    public int? CurrentLocationId { get; set; }
    public int? CurrentDepartmentId { get; set; }
    public string? CurrentDepartmentName { get; set; }

    /// <summary>Current custodian (AssetUsage IsCurrent), if any.</summary>
    public int? CurrentResponsibleEmployeeId { get; set; }

    public string? CurrentResponsibleEmployeeName { get; set; }

    public int? CurrentResponsibleUserId { get; set; }

    // Depreciation (optional; populated in GET by id)
    public int? DepreciationPolicyId { get; set; }
    public string? DepreciationPolicyName { get; set; }
    public int? DepreciationUsefulLifeMonths { get; set; }
    public decimal? DepreciationSalvageValue { get; set; }
    public DateOnly? DepreciationPeriod { get; set; }
    public decimal? DepreciationAmount { get; set; }
    public decimal? AccumulatedDepreciation { get; set; }
    public decimal? RemainingValue { get; set; }

    // Maintenance (optional; populated in GET by id)
    public List<MaintenanceScheduleDTO>? MaintenanceSchedules { get; set; }

    // Documents (optional; populated in GET by id)
    public List<AssetDocumentDTO>? Documents { get; set; }
}
