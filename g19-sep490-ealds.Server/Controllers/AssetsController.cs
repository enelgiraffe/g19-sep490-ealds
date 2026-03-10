using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public AssetsController(EaldsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/assets - Get all assets with optional search and filter
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetResponseDTO>>> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status,
        [FromQuery] int? assetTypeId,
        [FromQuery] int? warehouseId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var query = _context.Assets
            .Include(a => a.AssetType)
            .Include(a => a.Warehouse)
            .AsNoTracking()
            .AsQueryable();

        // Keyword search: search in code and name
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(a =>
                a.Code.ToLower().Contains(kw) ||
                a.Name.ToLower().Contains(kw));
        }

        // Filter by status
        if (status.HasValue)
        {
            query = query.Where(a => a.Status == (int)status.Value);
        }

        // Filter by asset type
        if (assetTypeId.HasValue)
        {
            query = query.Where(a => a.AssetTypeId == assetTypeId.Value);
        }

        // Filter by warehouse
        if (warehouseId.HasValue)
        {
            query = query.Where(a => a.WarehouseId == warehouseId.Value);
        }

        // Filter by price range
        if (minPrice.HasValue)
        {
            query = query.Where(a => a.CurrentValue >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(a => a.CurrentValue <= maxPrice.Value);
        }

        // Filter by purchase date range
        if (fromDate.HasValue)
        {
            query = query.Where(a => a.PurchaseDate >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(a => a.PurchaseDate <= toDate.Value);
        }

        var assets = await query
            .Select(a => new AssetResponseDTO
            {
                AssetId = a.AssetId,
                Code = a.Code,
                Name = a.Name,
                AssetTypeId = a.AssetTypeId,
                AssetTypeName = a.AssetType != null ? a.AssetType.Name : null,
                PurchaseDate = a.PurchaseDate,
                OriginalPrice = a.OriginalPrice,
                CurrentValue = a.CurrentValue,
                Status = (AssetStatus)a.Status,
                StatusName = ((AssetStatus)a.Status).ToString(),
                WarrantyEndDate = a.WarrantyEndDate,
                InUseDate = a.InUseDate,
                Unit = a.Unit,
                Quantity = a.Quantity,
                WarehouseId = a.WarehouseId,
                WarehouseName = a.Warehouse != null ? a.Warehouse.Name : null,
                CreatedBy = a.CreatedBy,
                CurrentDepartmentId = a.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => (int?)al.DepartmentId)
                    .FirstOrDefault(),
                CurrentDepartmentName = a.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Department.Name)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(assets);
    }

    /// <summary>
    /// GET /api/assets/{id} - Get asset by id
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetResponseDTO>> GetById(int id)
    {
        var dto = await _context.Assets
            .Include(a => a.AssetType)
            .Include(a => a.Warehouse)
            .Where(a => a.AssetId == id)
            .Select(a => new AssetResponseDTO
            {
                AssetId = a.AssetId,
                Code = a.Code,
                Name = a.Name,
                AssetTypeId = a.AssetTypeId,
                AssetTypeName = a.AssetType != null ? a.AssetType.Name : null,
                PurchaseDate = a.PurchaseDate,
                OriginalPrice = a.OriginalPrice,
                CurrentValue = a.CurrentValue,
                Status = (AssetStatus)a.Status,
                StatusName = ((AssetStatus)a.Status).ToString(),
                WarrantyEndDate = a.WarrantyEndDate,
                InUseDate = a.InUseDate,
                Unit = a.Unit,
                Quantity = a.Quantity,
                WarehouseId = a.WarehouseId,
                WarehouseName = a.Warehouse != null ? a.Warehouse.Name : null,
                CreatedBy = a.CreatedBy,
                CurrentDepartmentId = a.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => (int?)al.DepartmentId)
                    .FirstOrDefault(),
                CurrentDepartmentName = a.AssetLocations
                    .Where(al => al.IsCurrent)
                    .Select(al => al.Department.Name)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (dto == null)
            return NotFound();

        return Ok(dto);
    }

    /// <summary>
    /// POST /api/assets - Create asset with General info and Depreciation settings
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetResponseDTO>> Create([FromBody] CreateAssetDTO dto)
    {
        if (await _context.Assets.AnyAsync(a => a.Code == dto.Code))
            return BadRequest(new { message = "Asset code already exists." });

        var asset = new Asset
        {
            Code = dto.Code,
            Name = dto.Name,
            AssetTypeId = dto.AssetTypeId,
            PurchaseDate = dto.PurchaseDate,
            OriginalPrice = dto.OriginalPrice,
            CurrentValue = dto.CurrentValue,
            Status = (int)AssetStatus.Available,
            WarrantyEndDate = dto.WarrantyEndDate,
            InUseDate = dto.InUseDate,
            Unit = dto.Unit,
            Quantity = dto.Quantity,
            WarehouseId = dto.WarehouseId,
            CreatedBy = dto.CreatedBy
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Depreciation settings: link asset to policy via initial DrepreciationRecord
        if (dto.DepreciationPolicyId.HasValue)
        {
            var policy = await _context.DepreciationPolicies.FindAsync(dto.DepreciationPolicyId.Value);
            if (policy != null)
            {
                var firstPeriod = new DateOnly(dto.PurchaseDate.Year, dto.PurchaseDate.Month, 1);
                var depRecord = new DrepreciationRecord
                {
                    AssetId = asset.AssetId,
                    PolicyId = policy.PolicyId,
                    Period = firstPeriod,
                    DepreciationAmount = 0,
                    AccumulatedDepreciation = 0,
                    RemainingValue = dto.CurrentValue,
                    CreateDate = DateTime.UtcNow
                };
                _context.DrepreciationRecords.Add(depRecord);
                await _context.SaveChangesAsync();
            }
        }

        await _context.Entry(asset)
            .Reference(a => a.AssetType).LoadAsync();
        await _context.Entry(asset)
            .Reference(a => a.Warehouse).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = asset.AssetId }, ToResponseDTO(asset));
    }

    /// <summary>
    /// PUT /api/assets/{id} - Update asset data and status
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetResponseDTO>> Update(int id, [FromBody] UpdateAssetDTO dto)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return NotFound();

        if (dto.Code != null) asset.Code = dto.Code;
        if (dto.Name != null) asset.Name = dto.Name;
        if (dto.AssetTypeId.HasValue) asset.AssetTypeId = dto.AssetTypeId.Value;
        if (dto.PurchaseDate.HasValue) asset.PurchaseDate = dto.PurchaseDate.Value;
        if (dto.OriginalPrice.HasValue) asset.OriginalPrice = dto.OriginalPrice.Value;
        if (dto.CurrentValue.HasValue) asset.CurrentValue = dto.CurrentValue.Value;
        if (dto.Status.HasValue) asset.Status = (int)dto.Status.Value;
        if (dto.WarrantyEndDate.HasValue) asset.WarrantyEndDate = dto.WarrantyEndDate;
        if (dto.InUseDate.HasValue) asset.InUseDate = dto.InUseDate;
        if (dto.Unit != null) asset.Unit = dto.Unit;
        if (dto.Quantity.HasValue) asset.Quantity = dto.Quantity.Value;
        if (dto.WarehouseId.HasValue) asset.WarehouseId = dto.WarehouseId.Value;

        await _context.SaveChangesAsync();

        await _context.Entry(asset)
            .Reference(a => a.AssetType).LoadAsync();
        await _context.Entry(asset)
            .Reference(a => a.Warehouse).LoadAsync();

        return Ok(ToResponseDTO(asset));
    }

    /// <summary>
    /// DELETE /api/assets/{id} - Set asset status to Disposed, Lost, or Liquidated (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<AssetResponseDTO>> Delete(int id, [FromBody] DeleteAssetDTO dto)
    {
        var status = dto.Status;
        if (status != AssetStatus.Disposed && status != AssetStatus.Lost && status != AssetStatus.Liquidated)
            return BadRequest(new { message = "Delete must set status to Disposed, Lost, or Liquidated." });

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return NotFound();

        asset.Status = (int)status;
        await _context.SaveChangesAsync();

        await _context.Entry(asset)
            .Reference(a => a.AssetType).LoadAsync();
        await _context.Entry(asset)
            .Reference(a => a.Warehouse).LoadAsync();

        return Ok(ToResponseDTO(asset));
    }

    private static AssetResponseDTO ToResponseDTO(Asset a)
    {
        return new AssetResponseDTO
        {
            AssetId = a.AssetId,
            Code = a.Code,
            Name = a.Name,
            AssetTypeId = a.AssetTypeId,
            AssetTypeName = a.AssetType?.Name,
            PurchaseDate = a.PurchaseDate,
            OriginalPrice = a.OriginalPrice,
            CurrentValue = a.CurrentValue,
            Status = (AssetStatus)a.Status,
            StatusName = ((AssetStatus)a.Status).ToString(),
            WarrantyEndDate = a.WarrantyEndDate,
            InUseDate = a.InUseDate,
            Unit = a.Unit,
            Quantity = a.Quantity,
            WarehouseId = a.WarehouseId,
            WarehouseName = a.Warehouse?.Name,
            CreatedBy = a.CreatedBy,
            CurrentDepartmentId = null,
            CurrentDepartmentName = null
        };
    }
}
