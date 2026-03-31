using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Models.DTOs;

/// <summary>
/// DTO for creating an <see cref="Asset"/> (catalog / product definition).
/// Financial and warehouse fields belong on <see cref="CreateAssetInstanceDTO"/> (or <see cref="CreateAssetDTO.InitialInstance"/>).
/// </summary>
public class CreateAssetDTO
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AssetTypeId { get; set; }
    public string Unit { get; set; } = null!;
    public int? Quantity { get; set; }
    public int CreatedBy { get; set; }
    public DateOnly? InUseDate { get; set; }
    public string? Specification { get; set; }
    public string? Note { get; set; }

    /// <summary>Optional first physical row: same transaction as the asset when provided.</summary>
    public CreateAssetInstanceDTO? InitialInstance { get; set; }
}

/// <summary>
/// DTO for updating catalog fields on an <see cref="Asset"/>.
/// </summary>
public class UpdateAssetDTO
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? AssetTypeId { get; set; }
    public AssetStatus? Status { get; set; }
    public string? Unit { get; set; }
    public int? Quantity { get; set; }
    public DateOnly? InUseDate { get; set; }
    public string? Specification { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// DTO for accountant-only status change on an <see cref="Asset"/> (<c>PUT /api/assets/{id}/status</c>).
/// </summary>
public class ChangeAssetStatusDTO
{
    public AssetStatus Status { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// DTO for delete on an <see cref="Asset"/> (sets catalog status to Disposed, Lost, or Liquidated).
/// </summary>
public class DeleteAssetDTO
{
    public AssetStatus Status { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Creates or describes a physical <see cref="AssetInstance"/>.
/// </summary>
public class CreateAssetInstanceDTO
{
    /// <summary>Required when POSTing to <c>/api/asset-instances</c>; omitted when nested under <see cref="CreateAssetDTO.InitialInstance"/>.</summary>
    public int? AssetId { get; set; }

    public string InstanceCode { get; set; } = null!;
    public string? SerialNumber { get; set; }
    public int WarehouseId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public DateOnly? InUseDate { get; set; }
    public int? DepreciationPolicyId { get; set; }
    public int? SupplierId { get; set; }
    public string? ContractNo { get; set; }
    public string? Condition { get; set; }
    public string? Note { get; set; }

    /// <summary>Optional assignment (requires ACCOUNTANT).</summary>
    public int? AssignedDepartmentId { get; set; }
    public int? ResponsibleEmployeeId { get; set; }
    public DateOnly? AssignmentEffectiveDate { get; set; }
}

/// <summary>
/// DTO for updating an <see cref="AssetInstance"/>.
/// </summary>
public class UpdateAssetInstanceDTO
{
    public string? InstanceCode { get; set; }
    public string? SerialNumber { get; set; }
    public int? WarehouseId { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? CurrentValue { get; set; }
    public AssetStatus? Status { get; set; }
    public DateOnly? InUseDate { get; set; }
    public int? DepreciationPolicyId { get; set; }
    public int? SupplierId { get; set; }
    public string? ContractNo { get; set; }
    public string? Condition { get; set; }
    public string? Note { get; set; }

    public int? AssignedDepartmentId { get; set; }
    public int? ResponsibleEmployeeId { get; set; }
    public DateOnly? AssignmentEffectiveDate { get; set; }
    public bool ClearDepartmentAssignment { get; set; }
    public bool ClearResponsibleEmployee { get; set; }
}

/// <summary>
/// Accountant status change for an instance (<c>PUT /api/asset-instances/{id}/status</c>).
/// </summary>
public class ChangeAssetInstanceStatusDTO
{
    public AssetStatus Status { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Soft delete / retirement for an instance (<c>DELETE /api/asset-instances/{id}</c>).
/// </summary>
public class DeleteAssetInstanceDTO
{
    public AssetStatus Status { get; set; }
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
    public int? IntervalValue { get; set; }
    public int? IntervalUnit { get; set; }
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
/// Response for catalog <see cref="Asset"/> list/detail (no per-instance financials).
/// </summary>
public class AssetResponseDTO
{
    public int AssetId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AssetTypeId { get; set; }
    public string? AssetTypeName { get; set; }
    public AssetStatus Status { get; set; }
    public string StatusName { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public int? Quantity { get; set; }
    public int CreatedBy { get; set; }
    public DateOnly? InUseDate { get; set; }
    public string? Specification { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// GET /api/assets/{id} — catalog plus schedules, documents, and instance summaries.
/// </summary>
public class AssetDetailResponseDTO : AssetResponseDTO
{
    public List<MaintenanceScheduleDTO>? MaintenanceSchedules { get; set; }
    public List<AssetDocumentDTO>? Documents { get; set; }
    public List<AssetInstanceResponseDTO>? Instances { get; set; }
}

/// <summary>
/// Response for a physical <see cref="AssetInstance"/>.
/// </summary>
public class AssetInstanceResponseDTO
{
    public int AssetInstanceId { get; set; }
    public int AssetId { get; set; }
    public int AssetTypeId { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetName { get; set; }
    public string InstanceCode { get; set; } = null!;
    public string? SerialNumber { get; set; }
    public int WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public AssetStatus Status { get; set; }
    public string StatusName { get; set; } = null!;
    public DateOnly? InUseDate { get; set; }
    public int? SupplierId { get; set; }
    public string? ContractNo { get; set; }
    public string? Condition { get; set; }
    public string? Note { get; set; }

    public int? CurrentLocationId { get; set; }
    public int? CurrentDepartmentId { get; set; }
    public string? CurrentDepartmentName { get; set; }
    public int? CurrentResponsibleEmployeeId { get; set; }
    public string? CurrentResponsibleEmployeeName { get; set; }
    public int? CurrentResponsibleUserId { get; set; }

    public int? DepreciationPolicyId { get; set; }
    public string? DepreciationPolicyName { get; set; }
    public int? DepreciationUsefulLifeMonths { get; set; }
    public decimal? DepreciationSalvageValue { get; set; }
    public DateOnly? DepreciationPeriod { get; set; }
    public decimal? DepreciationAmount { get; set; }
    public decimal? AccumulatedDepreciation { get; set; }
    public decimal? RemainingValue { get; set; }
    public List<GuaranteeDTO>? Guarantees { get; set; }
}

public class GuaranteeDTO
{
    public int GuaranteeId { get; set; }
    public int WarrantyPeriodValue { get; set; }
    public string WarrantyPeriodUnit { get; set; } = null!;
    public string? WarrantyConditions { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly WarrantyEndDate { get; set; }
}
