using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

/// <summary>
/// Physical asset rows: warehouse, valuation, location, custodian, depreciation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AssetInstancesController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public AssetInstancesController(EaldsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/assetinstances — Search and filter instances (replaces former filters on GET /api/assets).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetInstanceResponseDTO>>> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status,
        [FromQuery] int? assetTypeId,
        [FromQuery] int? warehouseId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var query = _context.AssetInstances
            .Include(i => i.Asset).ThenInclude(a => a!.AssetType)
            .Include(i => i.Warehouse)
            .Include(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .Include(i => i.Guarantees)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(i =>
                i.InstanceCode.ToLower().Contains(kw) ||
                (i.SerialNumber != null && i.SerialNumber.ToLower().Contains(kw)) ||
                (i.Asset != null && i.Asset.Code.ToLower().Contains(kw)) ||
                (i.Asset != null && i.Asset.Name.ToLower().Contains(kw)));
        }

        if (status.HasValue)
        {
            if (status.Value == AssetStatus.Damaged)
            {
                query = query.Where(i =>
                    i.Status == (int)AssetStatus.Damaged ||
                    (i.Asset != null && _context.AssetRequests.Any(r =>
                        r.AssetId == i.Asset.AssetId &&
                        r.Title != null &&
                        r.Title.StartsWith("Damage report") &&
                        r.Status == 0)));
            }
            else
            {
                query = query.Where(i => i.Status == (int)status.Value);
            }
        }

        if (assetTypeId.HasValue)
            query = query.Where(i => i.Asset != null && i.Asset.AssetTypeId == assetTypeId.Value);

        if (warehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == warehouseId.Value);

        if (minPrice.HasValue)
            query = query.Where(i => i.CurrentValue >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(i => i.CurrentValue <= maxPrice.Value);

        if (fromDate.HasValue)
            query = query.Where(i => i.PurchaseDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(i => i.PurchaseDate <= toDate.Value);

        var instances = await query.ToListAsync();

        var assetIds = instances.Select(i => i.AssetId).Distinct().ToList();
        var damagedAssetIds = await _context.AssetRequests
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
        var damagedSet = damagedAssetIds.ToHashSet();

        var instanceIds = instances.Select(i => i.AssetInstanceId).ToList();
        var latestDeps = await LoadLatestDepreciationByInstanceAsync(instanceIds);

        return Ok(instances.Select(i =>
        {
            var forced = damagedSet.Contains(i.AssetId) ? AssetStatus.Damaged : (AssetStatus?)null;
            return ToDto(i, latestDeps.GetValueOrDefault(i.AssetInstanceId), forced);
        }));
    }

    /// <summary>
    /// GET /api/assetinstances/instance-code-prefixes — Distinct letter-prefixes used for instance codes (LAP01-style),
    /// not catalog <see cref="Asset.Code"/> (mã tài sản). Only instance rows whose code ends with digits and differs
    /// from the parent asset code contribute; suggested prefix must not equal any catalog code.
    /// </summary>
    [HttpGet("instance-code-prefixes")]
    public async Task<ActionResult<IEnumerable<string>>> GetInstanceCodePrefixes()
    {
        var catalogCodes = await _context.Assets
            .AsNoTracking()
            .Where(a => a.Code != null && a.Code != string.Empty)
            .Select(a => a.Code!)
            .ToListAsync();

        var catalogSet = new HashSet<string>(catalogCodes, StringComparer.OrdinalIgnoreCase);

        var rows = await _context.AssetInstances
            .AsNoTracking()
            .Select(i => new
            {
                i.InstanceCode,
                AssetCode = i.Asset != null ? i.Asset.Code : null
            })
            .ToListAsync();

        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var ic = row.InstanceCode;
            if (string.IsNullOrWhiteSpace(ic))
                continue;

            var icTrim = ic.Trim();
            if (!EndsWithDigit(icTrim))
                continue;

            if (!string.IsNullOrEmpty(row.AssetCode) &&
                string.Equals(icTrim, row.AssetCode.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            var p = StripTrailingDigitsPrefix(icTrim);
            if (string.IsNullOrWhiteSpace(p))
                continue;

            if (catalogSet.Contains(p))
                continue;

            prefixes.Add(p);
        }

        return Ok(prefixes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static bool EndsWithDigit(string code) =>
        code.Length > 0 && char.IsDigit(code[^1]);

    private static string StripTrailingDigitsPrefix(string code)
    {
        if (string.IsNullOrEmpty(code))
            return string.Empty;
        var i = code.Length - 1;
        while (i >= 0 && char.IsDigit(code[i]))
            i--;
        return i < 0 ? string.Empty : code[..(i + 1)];
    }

    /// <summary>
    /// GET /api/assetinstances/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetInstanceResponseDTO>> GetById(int id)
    {
        var instance = await _context.AssetInstances
            .Include(i => i.Asset).ThenInclude(a => a!.AssetType)
            .Include(i => i.Warehouse)
            .Include(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .Include(i => i.Guarantees)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.AssetInstanceId == id);

        if (instance == null)
            return NotFound();

        var hasDamageReport = instance.Asset != null && await _context.AssetRequests
            .AsNoTracking()
            .AnyAsync(r =>
                r.AssetId == instance.Asset.AssetId &&
                r.Title != null &&
                r.Title.StartsWith("Damage report") &&
                r.Status == 0);

        var latestDepSnapshot = await _context.DepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetInstanceId == id)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();

        var forced = hasDamageReport ? AssetStatus.Damaged : (AssetStatus?)null;
        return Ok(ToDto(instance, latestDepSnapshot, forced));
    }

    /// <summary>
    /// POST /api/assetinstances — Add a physical instance to an existing catalog asset.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetInstanceResponseDTO>> Create([FromBody] CreateAssetInstanceDTO dto)
    {
        if (!dto.AssetId.HasValue || dto.AssetId.Value <= 0)
            return BadRequest(new { message = "AssetId is required." });

        if (InstanceCreateHasAssignment(dto))
        {
            var denied = RequireAccountantForAssignment();
            if (denied != null)
                return denied;
            var v = await ValidateCreateAssignmentAsync(dto);
            if (v != null)
                return v;
        }

        if (!await _context.Assets.AnyAsync(a => a.AssetId == dto.AssetId.Value))
            return BadRequest(new { message = $"Asset {dto.AssetId.Value} not found." });

        if (await _context.AssetInstances.AnyAsync(i => i.InstanceCode == dto.InstanceCode))
            return BadRequest(new { message = "Instance code already exists." });

        if (!await _context.Warehouses.AnyAsync(w => w.WarehouseId == dto.WarehouseId))
            return BadRequest(new { message = $"WarehouseId {dto.WarehouseId} does not exist." });

        var instance = new AssetInstance
        {
            AssetId = dto.AssetId.Value,
            WarehouseId = dto.WarehouseId,
            DepreciationPolicyId = dto.DepreciationPolicyId,
            InstanceCode = dto.InstanceCode,
            SerialNumber = dto.SerialNumber,
            Status = (int)AssetStatus.Available,
            InUseDate = dto.InUseDate,
            PurchaseDate = dto.PurchaseDate,
            OriginalPrice = dto.OriginalPrice,
            CurrentValue = dto.CurrentValue,
            SupplierId = dto.SupplierId,
            ContractNo = dto.ContractNo,
            Condition = dto.Condition,
            Note = dto.Note
        };

        _context.AssetInstances.Add(instance);
        await _context.SaveChangesAsync();

        if (dto.DepreciationPolicyId.HasValue)
        {
            var policy = await _context.DepreciationPolicies.FindAsync(dto.DepreciationPolicyId.Value);
            if (policy != null)
            {
                var firstPeriod = new DateOnly(dto.PurchaseDate.Year, dto.PurchaseDate.Month, 1);
                _context.DepreciationRecords.Add(new DepreciationRecord
                {
                    AssetInstanceId = instance.AssetInstanceId,
                    PolicyId = policy.PolicyId,
                    Period = firstPeriod,
                    DepreciationAmount = 0,
                    AccumulatedDepreciation = 0,
                    OriginalValue = dto.CurrentValue,
                    RemainingValue = dto.CurrentValue,
                    CreateDate = DateTime.UtcNow,
                    IsPosted = false
                });
                await _context.SaveChangesAsync();
            }
        }

        await ApplyCreateAssignmentAsync(instance.AssetInstanceId, dto);
        if (InstanceCreateHasAssignment(dto))
            await _context.SaveChangesAsync();

        var latestDep = await _context.DepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetInstanceId == instance.AssetInstanceId)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();

        var reloaded = await _context.AssetInstances
            .Include(i => i.Asset).ThenInclude(a => a!.AssetType)
            .Include(i => i.Warehouse)
            .Include(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .Include(i => i.Guarantees)
            .AsNoTracking()
            .FirstAsync(i => i.AssetInstanceId == instance.AssetInstanceId);

        return CreatedAtAction(nameof(GetById), new { id = instance.AssetInstanceId },
            ToDto(reloaded, latestDep, null));
    }

    /// <summary>
    /// PUT /api/assetinstances/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetInstanceResponseDTO>> Update(int id, [FromBody] UpdateAssetInstanceDTO dto)
    {
        if (UpdateHasAssignment(dto))
        {
            var denied = RequireAccountantForAssignment();
            if (denied != null)
                return denied;
        }

        var instance = await _context.AssetInstances.FindAsync(id);
        if (instance == null)
            return NotFound();

        if (dto.InstanceCode != null)
        {
            if (await _context.AssetInstances.AnyAsync(i => i.InstanceCode == dto.InstanceCode && i.AssetInstanceId != id))
                return BadRequest(new { message = "Instance code already exists." });
            instance.InstanceCode = dto.InstanceCode;
        }

        if (dto.SerialNumber != null) instance.SerialNumber = dto.SerialNumber;
        if (dto.WarehouseId.HasValue)
        {
            if (!await _context.Warehouses.AnyAsync(w => w.WarehouseId == dto.WarehouseId.Value))
                return BadRequest(new { message = $"WarehouseId {dto.WarehouseId.Value} does not exist." });
            instance.WarehouseId = dto.WarehouseId.Value;
        }
        if (dto.PurchaseDate.HasValue) instance.PurchaseDate = dto.PurchaseDate.Value;
        if (dto.OriginalPrice.HasValue) instance.OriginalPrice = dto.OriginalPrice.Value;
        if (dto.CurrentValue.HasValue) instance.CurrentValue = dto.CurrentValue.Value;
        if (dto.Status.HasValue) instance.Status = (int)dto.Status.Value;
        if (dto.InUseDate.HasValue) instance.InUseDate = dto.InUseDate;
        if (dto.DepreciationPolicyId.HasValue) instance.DepreciationPolicyId = dto.DepreciationPolicyId;
        if (dto.SupplierId.HasValue) instance.SupplierId = dto.SupplierId;
        if (dto.ContractNo != null) instance.ContractNo = dto.ContractNo;
        if (dto.Condition != null) instance.Condition = dto.Condition;
        if (dto.Note != null) instance.Note = dto.Note;

        if (dto.DepreciationPolicyId.HasValue)
        {
            var existingDep = await _context.DepreciationRecords
                .FirstOrDefaultAsync(r => r.AssetInstanceId == id);
            if (existingDep == null)
            {
                var policy = await _context.DepreciationPolicies.FindAsync(dto.DepreciationPolicyId.Value);
                if (policy != null)
                {
                    var firstPeriod = new DateOnly(instance.PurchaseDate.Year, instance.PurchaseDate.Month, 1);
                    _context.DepreciationRecords.Add(new DepreciationRecord
                    {
                        AssetInstanceId = id,
                        PolicyId = policy.PolicyId,
                        Period = firstPeriod,
                        DepreciationAmount = 0,
                        AccumulatedDepreciation = 0,
                        OriginalValue = instance.CurrentValue,
                        RemainingValue = instance.CurrentValue,
                        CreateDate = DateTime.UtcNow,
                        IsPosted = false
                    });
                }
            }
            else
            {
                existingDep.PolicyId = dto.DepreciationPolicyId.Value;
            }
        }

        var hasWarrantyPayload =
            dto.WarrantyPeriodValue.HasValue ||
            dto.WarrantyPeriodUnit != null ||
            dto.WarrantyConditions != null ||
            dto.WarrantyStartDate.HasValue ||
            dto.WarrantyEndDate.HasValue;

        if (hasWarrantyPayload)
        {
            var latestGuarantee = await _context.Guarantees
                .Where(g => g.AssetInstanceId == id)
                .OrderByDescending(g => g.WarrantyEndDate)
                .ThenByDescending(g => g.GuaranteeId)
                .FirstOrDefaultAsync();

            if (latestGuarantee == null)
            {
                if (!dto.WarrantyPeriodValue.HasValue ||
                    string.IsNullOrWhiteSpace(dto.WarrantyPeriodUnit) ||
                    !dto.WarrantyStartDate.HasValue ||
                    !dto.WarrantyEndDate.HasValue)
                {
                    return BadRequest(new
                    {
                        message = "To create warranty info, provide warranty period value, unit, start date, and end date."
                    });
                }

                if (dto.WarrantyPeriodValue.Value <= 0)
                    return BadRequest(new { message = "Warranty period value must be greater than 0." });

                if (dto.WarrantyEndDate.Value < dto.WarrantyStartDate.Value)
                    return BadRequest(new { message = "Warranty end date must be greater than or equal to start date." });

                latestGuarantee = new Guarantee
                {
                    AssetInstanceId = id,
                    WarrantyPeriodValue = dto.WarrantyPeriodValue.Value,
                    WarrantyPeriodUnit = dto.WarrantyPeriodUnit.Trim(),
                    WarrantyConditions = dto.WarrantyConditions,
                    StartDate = dto.WarrantyStartDate.Value,
                    WarrantyEndDate = dto.WarrantyEndDate.Value
                };
                _context.Guarantees.Add(latestGuarantee);
            }
            else
            {
                if (dto.WarrantyPeriodValue.HasValue)
                {
                    if (dto.WarrantyPeriodValue.Value <= 0)
                        return BadRequest(new { message = "Warranty period value must be greater than 0." });
                    latestGuarantee.WarrantyPeriodValue = dto.WarrantyPeriodValue.Value;
                }
                if (dto.WarrantyPeriodUnit != null)
                    latestGuarantee.WarrantyPeriodUnit = dto.WarrantyPeriodUnit.Trim();
                if (dto.WarrantyConditions != null)
                    latestGuarantee.WarrantyConditions = dto.WarrantyConditions;
                if (dto.WarrantyStartDate.HasValue)
                    latestGuarantee.StartDate = dto.WarrantyStartDate.Value;
                if (dto.WarrantyEndDate.HasValue)
                    latestGuarantee.WarrantyEndDate = dto.WarrantyEndDate.Value;

                if (latestGuarantee.WarrantyEndDate < latestGuarantee.StartDate)
                    return BadRequest(new { message = "Warranty end date must be greater than or equal to start date." });
            }
        }

        var hasDepreciationPayload =
            dto.DepreciationPeriod.HasValue ||
            dto.DepreciationAmount.HasValue ||
            dto.AccumulatedDepreciation.HasValue ||
            dto.RemainingValue.HasValue;

        if (hasDepreciationPayload)
        {
            var latestDepToUpdate = await _context.DepreciationRecords
                .Where(r => r.AssetInstanceId == id)
                .OrderByDescending(r => r.Period)
                .ThenByDescending(r => r.CreateDate)
                .FirstOrDefaultAsync();

            if (latestDepToUpdate != null)
            {
                if (dto.DepreciationPeriod.HasValue)
                    latestDepToUpdate.Period = dto.DepreciationPeriod.Value;
                if (dto.DepreciationAmount.HasValue)
                {
                    if (dto.DepreciationAmount.Value < 0)
                        return BadRequest(new { message = "Depreciation amount cannot be negative." });
                    latestDepToUpdate.DepreciationAmount = dto.DepreciationAmount.Value;
                }
                if (dto.AccumulatedDepreciation.HasValue)
                {
                    if (dto.AccumulatedDepreciation.Value < 0)
                        return BadRequest(new { message = "Accumulated depreciation cannot be negative." });
                    latestDepToUpdate.AccumulatedDepreciation = dto.AccumulatedDepreciation.Value;
                }
                if (dto.RemainingValue.HasValue)
                {
                    if (dto.RemainingValue.Value < 0)
                        return BadRequest(new { message = "Remaining value cannot be negative." });
                    latestDepToUpdate.RemainingValue = dto.RemainingValue.Value;
                }
            }
        }

        var assignmentError = await ApplyUpdateAssignmentAsync(id, dto);
        if (assignmentError != null)
            return assignmentError;

        await _context.SaveChangesAsync();

        var latestDep = await _context.DepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetInstanceId == id)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();

        var reloaded = await _context.AssetInstances
            .Include(i => i.Asset).ThenInclude(a => a!.AssetType)
            .Include(i => i.Warehouse)
            .Include(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .Include(i => i.Guarantees)
            .AsNoTracking()
            .FirstAsync(i => i.AssetInstanceId == id);

        return Ok(ToDto(reloaded, latestDep, null));
    }

    /// <summary>
    /// PUT /api/assetinstances/{id}/status — Accountants set operational status on the instance.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<ActionResult<AssetInstanceResponseDTO>> ChangeStatus(int id, [FromBody] ChangeAssetInstanceStatusDTO dto)
    {
        if (!Enum.IsDefined(typeof(AssetStatus), dto.Status))
            return BadRequest(new { message = "Invalid asset status value." });

        var instance = await _context.AssetInstances.FindAsync(id);
        if (instance == null)
            return NotFound();

        instance.Status = (int)dto.Status;
        await _context.SaveChangesAsync();

        var latestDep = await _context.DepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetInstanceId == id)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();

        var reloaded = await _context.AssetInstances
            .Include(i => i.Asset).ThenInclude(a => a!.AssetType)
            .Include(i => i.Warehouse)
            .Include(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .Include(i => i.Guarantees)
            .AsNoTracking()
            .FirstAsync(i => i.AssetInstanceId == id);

        var hasDamageReport = reloaded.Asset != null && await _context.AssetRequests
            .AsNoTracking()
            .AnyAsync(r =>
                r.AssetId == reloaded.Asset.AssetId &&
                r.Title != null &&
                r.Title.StartsWith("Damage report") &&
                r.Status == 0);

        return Ok(ToDto(reloaded, latestDep, hasDamageReport ? AssetStatus.Damaged : null));
    }

    /// <summary>
    /// DELETE /api/assetinstances/{id} — Set instance status to Disposed, Lost, or Liquidated.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<AssetInstanceResponseDTO>> Delete(
        int id,
        [FromQuery] AssetStatus? status,
        [FromBody] DeleteAssetInstanceDTO? dto)
    {
        var effectiveStatus = status ?? dto?.Status;
        if (!effectiveStatus.HasValue)
            return BadRequest(new { message = "Delete must provide status = Disposed, Lost, or Liquidated (query or body)." });

        if (effectiveStatus.Value != AssetStatus.Disposed &&
            effectiveStatus.Value != AssetStatus.Lost &&
            effectiveStatus.Value != AssetStatus.Liquidated)
            return BadRequest(new { message = "Delete must set status to Disposed, Lost, or Liquidated." });

        var instance = await _context.AssetInstances.FindAsync(id);
        if (instance == null)
            return NotFound();

        instance.Status = (int)effectiveStatus.Value;
        await _context.SaveChangesAsync();

        var reloaded = await _context.AssetInstances
            .Include(i => i.Asset).ThenInclude(a => a!.AssetType)
            .Include(i => i.Warehouse)
            .Include(i => i.Guarantees)
            .AsNoTracking()
            .FirstAsync(i => i.AssetInstanceId == id);

        return Ok(ToDto(reloaded, null, null));
    }

    private async Task<Dictionary<int, DepreciationRecord>> LoadLatestDepreciationByInstanceAsync(
        List<int> instanceIds)
    {
        if (instanceIds.Count == 0)
            return new Dictionary<int, DepreciationRecord>();

        var rows = await _context.DepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => instanceIds.Contains(r.AssetInstanceId))
            .ToListAsync();

        return rows
            .GroupBy(r => r.AssetInstanceId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.Period).ThenByDescending(r => r.CreateDate).First());
    }

    private static bool InstanceCreateHasAssignment(CreateAssetInstanceDTO dto) =>
        dto.AssignedDepartmentId.HasValue || dto.ResponsibleEmployeeId.HasValue;

    private static bool UpdateHasAssignment(UpdateAssetInstanceDTO dto) =>
        dto.AssignedDepartmentId.HasValue ||
        dto.ResponsibleEmployeeId.HasValue ||
        dto.ClearDepartmentAssignment ||
        dto.ClearResponsibleEmployee;

    private ActionResult<AssetInstanceResponseDTO>? RequireAccountantForAssignment()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { message = "Authentication is required to assign or reassign assets." });
        if (!User.IsInRole("ACCOUNTANT"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only accountants may assign or reassign assets." });
        return null;
    }

    private async Task<ActionResult<AssetInstanceResponseDTO>?> ValidateCreateAssignmentAsync(CreateAssetInstanceDTO dto)
    {
        Employee? emp = null;
        if (dto.ResponsibleEmployeeId.HasValue)
        {
            emp = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == dto.ResponsibleEmployeeId.Value);
            if (emp == null)
                return BadRequest(new { message = $"Employee {dto.ResponsibleEmployeeId.Value} not found." });
        }

        int? deptId = dto.AssignedDepartmentId;
        if (deptId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == deptId.Value))
                return BadRequest(new { message = $"Department {deptId.Value} does not exist." });
        }

        if (emp != null && deptId.HasValue && deptId.Value != emp.DepartmentId)
            return BadRequest(new { message = "Assigned department must match the responsible employee's department." });

        return null;
    }

    private async Task ApplyCreateAssignmentAsync(int assetInstanceId, CreateAssetInstanceDTO dto)
    {
        if (!dto.AssignedDepartmentId.HasValue && !dto.ResponsibleEmployeeId.HasValue)
            return;

        var effective = dto.AssignmentEffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        Employee? emp = null;
        if (dto.ResponsibleEmployeeId.HasValue)
            emp = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == dto.ResponsibleEmployeeId.Value);

        int? deptId = dto.AssignedDepartmentId;
        if (emp != null && !deptId.HasValue)
            deptId = emp.DepartmentId;

        if (deptId.HasValue)
        {
            await CloseCurrentLocationAsync(assetInstanceId, null, effective);
            _context.AssetLocations.Add(new AssetLocation
            {
                AssetInstanceId = assetInstanceId,
                DepartmentId = deptId.Value,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }

        if (dto.ResponsibleEmployeeId.HasValue)
        {
            await CloseCurrentUsageAsync(assetInstanceId, effective);
            _context.AssetUsages.Add(new AssetUsage
            {
                AssetInstanceId = assetInstanceId,
                EmployeeId = dto.ResponsibleEmployeeId.Value,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }
    }

    private async Task<ActionResult<AssetInstanceResponseDTO>?> ApplyUpdateAssignmentAsync(int assetInstanceId, UpdateAssetInstanceDTO dto)
    {
        if (!dto.AssignedDepartmentId.HasValue &&
            !dto.ResponsibleEmployeeId.HasValue &&
            !dto.ClearDepartmentAssignment &&
            !dto.ClearResponsibleEmployee)
            return null;

        if (dto.ClearDepartmentAssignment && dto.AssignedDepartmentId.HasValue)
            return BadRequest(new { message = "Cannot clear department and assign a department in the same request." });

        if (dto.ClearResponsibleEmployee && dto.ResponsibleEmployeeId.HasValue)
            return BadRequest(new { message = "Cannot clear responsible employee and assign one in the same request." });

        if (dto.ClearDepartmentAssignment && dto.ResponsibleEmployeeId.HasValue)
            return BadRequest(new { message = "Cannot clear department while assigning a responsible employee." });

        var effective = dto.AssignmentEffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        Employee? emp = null;
        if (dto.ResponsibleEmployeeId.HasValue)
        {
            emp = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == dto.ResponsibleEmployeeId.Value);
            if (emp == null)
                return BadRequest(new { message = $"Employee {dto.ResponsibleEmployeeId.Value} not found." });
        }

        if (dto.ClearDepartmentAssignment)
        {
            await CloseCurrentLocationAsync(assetInstanceId, null, effective);
        }
        else if (dto.AssignedDepartmentId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == dto.AssignedDepartmentId.Value))
                return BadRequest(new { message = $"Department {dto.AssignedDepartmentId.Value} does not exist." });

            if (emp != null && dto.AssignedDepartmentId.Value != emp.DepartmentId)
                return BadRequest(new { message = "Assigned department must match the responsible employee's department." });

            await CloseCurrentLocationAsync(assetInstanceId, null, effective);
            _context.AssetLocations.Add(new AssetLocation
            {
                AssetInstanceId = assetInstanceId,
                DepartmentId = dto.AssignedDepartmentId.Value,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }
        else if (dto.ResponsibleEmployeeId.HasValue && emp != null)
        {
            await CloseCurrentLocationAsync(assetInstanceId, null, effective);
            _context.AssetLocations.Add(new AssetLocation
            {
                AssetInstanceId = assetInstanceId,
                DepartmentId = emp.DepartmentId,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }

        if (dto.ClearResponsibleEmployee)
        {
            await CloseCurrentUsageAsync(assetInstanceId, effective);
        }
        else if (dto.ResponsibleEmployeeId.HasValue)
        {
            await CloseCurrentUsageAsync(assetInstanceId, effective);
            _context.AssetUsages.Add(new AssetUsage
            {
                AssetInstanceId = assetInstanceId,
                EmployeeId = dto.ResponsibleEmployeeId.Value,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }

        return null;
    }

    private async Task CloseCurrentLocationAsync(int assetInstanceId, int? excludeLocationId, DateOnly newStartDate)
    {
        var current = await _context.AssetLocations
            .Where(l => l.AssetInstanceId == assetInstanceId && l.IsCurrent &&
                        (excludeLocationId == null || l.LocationId != excludeLocationId))
            .FirstOrDefaultAsync();

        if (current != null)
        {
            current.IsCurrent = false;
            current.EndDate = newStartDate.AddDays(-1);
        }
    }

    private async Task CloseCurrentUsageAsync(int assetInstanceId, DateOnly newStartDate)
    {
        var current = await _context.AssetUsages
            .Where(u => u.AssetInstanceId == assetInstanceId && u.IsCurrent)
            .FirstOrDefaultAsync();

        if (current != null)
        {
            current.IsCurrent = false;
            current.EndDate = newStartDate.AddDays(-1);
        }
    }

    private static AssetInstanceResponseDTO ToDto(
        AssetInstance i,
        DepreciationRecord? latestDep,
        AssetStatus? forcedStatus)
    {
        var effectiveStatus = forcedStatus ?? (AssetStatus)i.Status;
        var dto = new AssetInstanceResponseDTO
        {
            AssetInstanceId = i.AssetInstanceId,
            AssetId = i.AssetId,
            AssetTypeId = i.Asset?.AssetTypeId ?? 0,
            AssetCode = i.Asset?.Code,
            AssetName = i.Asset?.Name,
            Specification = i.Asset?.Specification,
            InstanceCode = i.InstanceCode,
            SerialNumber = i.SerialNumber,
            WarehouseId = i.WarehouseId,
            WarehouseName = i.Warehouse?.Name,
            PurchaseDate = i.PurchaseDate,
            OriginalPrice = i.OriginalPrice,
            CurrentValue = i.CurrentValue,
            Status = effectiveStatus,
            StatusName = effectiveStatus.ToString(),
            InUseDate = i.InUseDate,
            SupplierId = i.SupplierId,
            ContractNo = i.ContractNo,
            Condition = i.Condition,
            Note = i.Note,
            CurrentLocationId = i.AssetLocations
                .Where(al => al.IsCurrent)
                .Select(al => (int?)al.LocationId)
                .FirstOrDefault(),
            CurrentDepartmentId = i.AssetLocations
                .Where(al => al.IsCurrent)
                .Select(al => (int?)al.DepartmentId)
                .FirstOrDefault(),
            CurrentDepartmentName = i.AssetLocations
                .Where(al => al.IsCurrent)
                .Select(al => al.Department != null ? al.Department.Name : null)
                .FirstOrDefault(),
            CurrentLocationNote = i.AssetLocations
                .Where(al => al.IsCurrent)
                .Select(al => al.Note)
                .FirstOrDefault(),
            CurrentResponsibleEmployeeId = i.AssetUsages
                .Where(u => u.IsCurrent)
                .Select(u => (int?)u.EmployeeId)
                .FirstOrDefault(),
            CurrentResponsibleEmployeeName = i.AssetUsages
                .Where(u => u.IsCurrent)
                .Select(u => u.Employee != null ? u.Employee.Name : null)
                .FirstOrDefault(),
            CurrentResponsibleUserId = i.AssetUsages
                .Where(u => u.IsCurrent)
                .Select(u => u.Employee != null ? (int?)u.Employee.UserId : null)
                .FirstOrDefault(),
            DepreciationPolicyId = i.DepreciationPolicyId,
            Guarantees = i.Guarantees?.Select(g => new GuaranteeDTO
            {
                GuaranteeId = g.GuaranteeId,
                WarrantyPeriodValue = g.WarrantyPeriodValue,
                WarrantyPeriodUnit = g.WarrantyPeriodUnit,
                WarrantyConditions = g.WarrantyConditions,
                StartDate = g.StartDate,
                WarrantyEndDate = g.WarrantyEndDate
            }).ToList()
        };

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

        return dto;
    }
}
