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
            .Include(a => a.AssetLocations)
                .ThenInclude(al => al.Department)
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
            if (status.Value == AssetStatus.Damaged)
            {
                query = query.Where(a =>
                    a.Status == (int)AssetStatus.Damaged ||
                    _context.AssetRequests.Any(r =>
                        r.AssetId == a.AssetId &&
                        r.Title != null &&
                        r.Title.StartsWith("Damage report") &&
                        r.Status == 0));
            }
            else
            {
                query = query.Where(a => a.Status == (int)status.Value);
            }
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

        var assets = await query.ToListAsync();

        // Sync legacy data: assets that already have a damage-report request should be treated as Damaged
        // even if Asset.Status wasn't updated at the time the request was created.
        var assetIds = assets.Select(a => a.AssetId).ToList();
        var damagedIds = await _context.AssetRequests
            .AsNoTracking()
            .Where(r =>
                r.AssetId.HasValue &&
                assetIds.Contains(r.AssetId.Value) &&
                r.Title != null &&
                r.Title.StartsWith("Damage report") &&
                r.Status == 0)
            .Select(r => r.AssetId!.Value)
            .Distinct()
            .ToListAsync();

        var damagedSet = damagedIds.ToHashSet();

        return Ok(assets.Select(a =>
            damagedSet.Contains(a.AssetId)
                ? ToResponseDTO(a, AssetStatus.Damaged)
                : ToResponseDTO(a)));
    }

    /// <summary>
    /// GET /api/assets/{id} - Get asset by id
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetResponseDTO>> GetById(int id)
    {
        var asset = await _context.Assets
            .Include(a => a.AssetType)
            .Include(a => a.Warehouse)
            .Include(a => a.AssetLocations)
                .ThenInclude(al => al.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetId == id);

        if (asset == null)
            return NotFound();

        var hasDamageReport = await _context.AssetRequests
            .AsNoTracking()
            .AnyAsync(r =>
                r.AssetId == id &&
                r.Title != null &&
                r.Title.StartsWith("Damage report") &&
                r.Status == 0);

        var latestDep = await _context.DrepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetId == id)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();

        // Avoid querying MaintenanceSchedule here because some environments
        // don't have IntervalUnit/IntervalValue columns yet.
        var schedules = new List<MaintenanceSchedule>();

        var documents = await _context.Documents
            .AsNoTracking()
            .Where(d => d.Procurement.AssetRequest.AssetId == id)
            .OrderByDescending(d => d.UploadedDate)
            .Select(d => new AssetDocumentDTO
            {
                DocumentId = d.DocumentId,
                DocumentType = d.DocumentType,
                FileUrl = d.FileUrl,
                UploadedDate = d.UploadedDate
            })
            .ToListAsync();

        var dto = ToResponseDTO(asset, latestDep, schedules);
        dto.Documents = documents;
        if (hasDamageReport)
        {
            dto.Status = AssetStatus.Damaged;
            dto.StatusName = AssetStatus.Damaged.ToString();
        }
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

        // Load depreciation info for response
        var latestDep = await _context.DrepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetId == asset.AssetId)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();

        // Avoid querying MaintenanceSchedule here because some environments
        // don't have IntervalUnit/IntervalValue columns yet.
        var schedules = new List<MaintenanceSchedule>();

        return CreatedAtAction(nameof(GetById), new { id = asset.AssetId }, ToResponseDTO(asset, latestDep, schedules));
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
        if (dto.AssetTypeId.HasValue)
        {
            if (!await _context.AssetTypes.AnyAsync(t => t.AssetTypeId == dto.AssetTypeId.Value))
                return BadRequest(new { message = $"AssetTypeId {dto.AssetTypeId.Value} does not exist." });
            asset.AssetTypeId = dto.AssetTypeId.Value;
        }
        if (dto.PurchaseDate.HasValue) asset.PurchaseDate = dto.PurchaseDate.Value;
        if (dto.OriginalPrice.HasValue) asset.OriginalPrice = dto.OriginalPrice.Value;
        if (dto.CurrentValue.HasValue) asset.CurrentValue = dto.CurrentValue.Value;
        if (dto.Status.HasValue) asset.Status = (int)dto.Status.Value;
        if (dto.WarrantyEndDate.HasValue) asset.WarrantyEndDate = dto.WarrantyEndDate;
        if (dto.InUseDate.HasValue) asset.InUseDate = dto.InUseDate;
        if (dto.Unit != null) asset.Unit = dto.Unit;
        if (dto.Quantity.HasValue) asset.Quantity = dto.Quantity.Value;
        if (dto.WarehouseId.HasValue)
        {
            if (!await _context.WarehouseAssets.AnyAsync(w => w.WarehouseId == dto.WarehouseId.Value))
                return BadRequest(new { message = $"WarehouseId {dto.WarehouseId.Value} does not exist." });
            asset.WarehouseId = dto.WarehouseId.Value;
        }

        // Handle depreciation policy update
        if (dto.DepreciationPolicyId.HasValue)
        {
            var existingDep = await _context.DrepreciationRecords
                .FirstOrDefaultAsync(r => r.AssetId == id);
            if (existingDep == null)
            {
                var policy = await _context.DepreciationPolicies.FindAsync(dto.DepreciationPolicyId.Value);
                if (policy != null)
                {
                    var firstPeriod = new DateOnly(asset.PurchaseDate.Year, asset.PurchaseDate.Month, 1);
                    var depRecord = new DrepreciationRecord
                    {
                        AssetId = asset.AssetId,
                        PolicyId = policy.PolicyId,
                        Period = firstPeriod,
                        DepreciationAmount = 0,
                        AccumulatedDepreciation = 0,
                        RemainingValue = asset.CurrentValue,
                        CreateDate = DateTime.UtcNow
                    };
                    _context.DrepreciationRecords.Add(depRecord);
                }
            }
            else
            {
                existingDep.PolicyId = dto.DepreciationPolicyId.Value;
            }
        }

        await _context.SaveChangesAsync();

        await _context.Entry(asset)
            .Reference(a => a.AssetType).LoadAsync();
        await _context.Entry(asset)
            .Reference(a => a.Warehouse).LoadAsync();

        // Reload depreciation info for response
        var latestDep = await _context.DrepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetId == id)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();

        // Avoid querying MaintenanceSchedule here because some environments
        // don't have IntervalUnit/IntervalValue columns yet.
        var schedules = new List<MaintenanceSchedule>();

        return Ok(ToResponseDTO(asset, latestDep, schedules));
    }

    /// <summary>
    /// DELETE /api/assets/{id} - Set asset status to Disposed, Lost, or Liquidated (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<AssetResponseDTO>> Delete(
        int id,
        [FromQuery] AssetStatus? status,
        [FromBody] DeleteAssetDTO? dto)
    {
        var effectiveStatus = status ?? dto?.Status;
        if (!effectiveStatus.HasValue)
            return BadRequest(new { message = "Delete must provide status = Disposed, Lost, or Liquidated (query or body)." });

        if (effectiveStatus.Value != AssetStatus.Disposed &&
            effectiveStatus.Value != AssetStatus.Lost &&
            effectiveStatus.Value != AssetStatus.Liquidated)
            return BadRequest(new { message = "Delete must set status to Disposed, Lost, or Liquidated." });

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return NotFound();

        asset.Status = (int)effectiveStatus.Value;
        await _context.SaveChangesAsync();

        await _context.Entry(asset)
            .Reference(a => a.AssetType).LoadAsync();
        await _context.Entry(asset)
            .Reference(a => a.Warehouse).LoadAsync();

        return Ok(ToResponseDTO(asset));
    }

    private static AssetResponseDTO ToResponseDTO(Asset a, AssetStatus? forcedStatus = null)
    {
        var effectiveStatus = forcedStatus ?? (AssetStatus)a.Status;
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
            Status = effectiveStatus,
            StatusName = effectiveStatus.ToString(),
            WarrantyEndDate = a.WarrantyEndDate,
            InUseDate = a.InUseDate,
            Unit = a.Unit,
            Quantity = a.Quantity,
            WarehouseId = a.WarehouseId,
            WarehouseName = a.Warehouse?.Name,
            CreatedBy = a.CreatedBy,
            CurrentLocationId = a.AssetLocations
                .Where(al => al.IsCurrent)
                .Select(al => (int?)al.LocationId)
                .FirstOrDefault(),
            CurrentDepartmentId = a.AssetLocations
                .Where(al => al.IsCurrent)
                .Select(al => (int?)al.DepartmentId)
                .FirstOrDefault(),
            CurrentDepartmentName = a.AssetLocations
                .Where(al => al.IsCurrent)
                .Select(al => al.Department != null ? al.Department.Name : null)
                .FirstOrDefault()
        };
    }

    private static AssetResponseDTO ToResponseDTO(
        Asset a,
        DrepreciationRecord? latestDep,
        List<MaintenanceSchedule> schedules)
    {
        var dto = ToResponseDTO(a);

        if (latestDep != null)
        {
            dto.DepreciationPolicyId = latestDep.PolicyId;
            dto.DepreciationPolicyName = latestDep.Policy?.Name;
            dto.DepreciationUsefulLifeMonths = latestDep.Policy?.UsefullLifeMonths;
            dto.DepreciationSalvageValue = latestDep.Policy?.SalvageValue;
            dto.DepreciationPeriod = latestDep.Period;
            dto.DepreciationAmount = latestDep.DepreciationAmount;
            dto.AccumulatedDepreciation = latestDep.AccumulatedDepreciation;
            dto.RemainingValue = latestDep.RemainingValue;
        }

        dto.MaintenanceSchedules = schedules.Select(s => new MaintenanceScheduleDTO
        {
            ScheduleId = s.ScheduleId,
            TemplateId = s.TemplateId,
            Content = s.Content,
            TemplateName = s.Template?.Name,
            ScheduleType = s.ScheduleType,
            IntervalMonths = s.IntervalMonths,
            IntervalHours = s.IntervalHours,
            StartDate = s.StartDate,
            NextDueDate = s.NextDueDate,
            EndDate = s.EndDate,
            IsActive = s.IsActive
        }).ToList();

        return dto;
    }
}
