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
}

/// <summary>
/// DTO for delete: sets asset status to Disposed, Lost, or Liquidated.
/// </summary>
public class DeleteAssetDTO
{
    public AssetStatus Status { get; set; } // Disposed, Lost, or Liquidated
    public string? Reason { get; set; }
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
    public int? CurrentDepartmentId { get; set; }

    public string? CurrentDepartmentName { get; set; }
}
