using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Services.Interface;
using System.Security.Claims;

namespace g19_sep490_ealds.Server.Controllers;

/// <summary>
/// Physical asset rows: warehouse, valuation, location, custodian, depreciation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AssetInstancesController : ControllerBase
{
    private readonly EaldsDbContext _context;
    private readonly int _departmentHeadRoleId;
    private readonly IMaintenanceTemplateService _maintenanceTemplates;

    public AssetInstancesController(
        EaldsDbContext context,
        IConfiguration configuration,
        IMaintenanceTemplateService maintenanceTemplates)
    {
        _context = context;
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
        _maintenanceTemplates = maintenanceTemplates;
    }

    /// <summary>
    /// GET /api/assetinstances — Search and filter instances (replaces former filters on GET /api/assets).
    /// </summary>
    /// <param name="forTransferSelection">When true, department heads may list instances in all departments (e.g. transfer picker).</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetInstanceResponseDTO>>> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status,
        [FromQuery] int? assetTypeId,
        [FromQuery] int? warehouseId,
        [FromQuery] int? currentDepartmentId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] bool forTransferSelection = false)
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
            query = query.Where(i => i.Status == (int)status.Value);

        if (assetTypeId.HasValue)
            query = query.Where(i => i.Asset != null && i.Asset.AssetTypeId == assetTypeId.Value);

        if (warehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == warehouseId.Value);

        if (currentDepartmentId.HasValue)
            query = query.Where(i =>
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == currentDepartmentId.Value));

        if (minPrice.HasValue)
            query = query.Where(i => i.CurrentValue >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(i => i.CurrentValue <= maxPrice.Value);

        if (fromDate.HasValue)
            query = query.Where(i => i.PurchaseDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(i => i.PurchaseDate <= toDate.Value);

        var scope = await DepartmentAssetScope.ResolveForUserAsync(User, _context, _departmentHeadRoleId);
        if (!forTransferSelection)
        {
            if (scope.IsRestricted && !scope.DepartmentId.HasValue)
                return Ok(Array.Empty<AssetInstanceResponseDTO>());

            if (scope.IsRestricted && scope.DepartmentId is int headDeptId && headDeptId > 0)
            {
                query = query.Where(i =>
                    i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == headDeptId) &&
                    i.Status != (int)AssetStatus.Disposed &&
                    i.Status != (int)AssetStatus.Lost &&
                    i.Status != (int)AssetStatus.Liquidated);
            }
        }

        var instances = await query.ToListAsync();

        var instanceIds = instances.Select(i => i.AssetInstanceId).ToList();
        var latestDeps = await LoadLatestDepreciationByInstanceAsync(instanceIds);
        var usageHistoriesByInstance = await BuildUsageHistoriesByInstanceAsync(instances);

        return Ok(instances.Select(i => ToDto(
            i,
            latestDeps.GetValueOrDefault(i.AssetInstanceId),
            usageHistoriesByInstance.GetValueOrDefault(i.AssetInstanceId),
            null)));
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
        var scope = await DepartmentAssetScope.ResolveForUserAsync(User, _context, _departmentHeadRoleId);
        if (scope.IsRestricted && !scope.DepartmentId.HasValue)
            return NotFound();

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

        if (scope.IsRestricted && scope.DepartmentId is int allowedDeptId &&
            !DepartmentAssetScope.InstanceBelongsToDepartment(instance, allowedDeptId))
            return NotFound();

        var allDepRecords = await _context.DepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetInstanceId == id)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .ToListAsync();
        var latestDepSnapshot = allDepRecords.FirstOrDefault();

        var usageHistories = (await BuildUsageHistoriesByInstanceAsync(new List<AssetInstance> { instance }))
            .GetValueOrDefault(instance.AssetInstanceId);
        return Ok(ToDto(instance, latestDepSnapshot, usageHistories, null, allDepRecords));
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

        try
        {
            var actorUserId = TryGetCurrentUserId();
            await _maintenanceTemplates.EnsureSchedulesForNewInstanceAsync(instance.AssetInstanceId, actorUserId);
        }
        catch
        {
            // Không hủy tạo cá thể nếu đồng bộ quy định bảo dưỡng lỗi.
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
            ToDto(
                reloaded,
                latestDep,
                (await BuildUsageHistoriesByInstanceAsync(new List<AssetInstance> { reloaded }))
                    .GetValueOrDefault(reloaded.AssetInstanceId),
                null));
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
        var hadDepreciationPolicy = instance.DepreciationPolicyId.HasValue;

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
        if (dto.DepreciationPolicyId.HasValue)
        {
            var policy = await _context.DepreciationPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PolicyId == dto.DepreciationPolicyId.Value && p.IsActive);
            if (policy == null)
            {
                return BadRequest(new
                {
                    message = "Depreciation policy not found or inactive."
                });
            }
            if (policy.SalvageValue >= instance.OriginalPrice)
            {
                return BadRequest(new
                {
                    message = "Salvage value must be less than asset original price."
                });
            }

            if (instance.DepreciationPolicyId.HasValue &&
                instance.DepreciationPolicyId.Value != dto.DepreciationPolicyId.Value)
            {
                return BadRequest(new
                {
                    message = "Depreciation policy has already been assigned and cannot be changed."
                });
            }

            instance.DepreciationPolicyId = dto.DepreciationPolicyId;
        }
        if (dto.SupplierId.HasValue) instance.SupplierId = dto.SupplierId;
        if (dto.ContractNo != null) instance.ContractNo = dto.ContractNo;
        if (dto.Condition != null) instance.Condition = dto.Condition;
        if (dto.Note != null) instance.Note = dto.Note;

        if (dto.DepreciationPolicyId.HasValue && !hadDepreciationPolicy)
        {
            var existingDep = await _context.DepreciationRecords
                .Where(r => r.AssetInstanceId == id)
                .OrderByDescending(r => r.Period)
                .ThenByDescending(r => r.CreateDate)
                .FirstOrDefaultAsync();
            if (existingDep != null)
                existingDep.PolicyId = dto.DepreciationPolicyId.Value;
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
            // Validate values first
            if (dto.DepreciationAmount.HasValue && dto.DepreciationAmount.Value < 0)
                return BadRequest(new { message = "Depreciation amount cannot be negative." });
            if (dto.AccumulatedDepreciation.HasValue && dto.AccumulatedDepreciation.Value < 0)
                return BadRequest(new { message = "Accumulated depreciation cannot be negative." });
            if (dto.RemainingValue.HasValue && dto.RemainingValue.Value < 0)
                return BadRequest(new { message = "Remaining value cannot be negative." });

            // Bao gồm cả record chưa lưu (vừa được thêm bởi block DepreciationPolicyId ở trên)
            var latestDepToUpdate = _context.DepreciationRecords.Local
                .Where(r => r.AssetInstanceId == id)
                .OrderByDescending(r => r.Period)
                .ThenByDescending(r => r.CreateDate)
                .FirstOrDefault()
                ?? await _context.DepreciationRecords
                    .Where(r => r.AssetInstanceId == id)
                    .OrderByDescending(r => r.Period)
                    .ThenByDescending(r => r.CreateDate)
                    .FirstOrDefaultAsync();

            if (latestDepToUpdate != null)
            {
                if (dto.DepreciationPeriod.HasValue)
                    latestDepToUpdate.Period = dto.DepreciationPeriod.Value;
                if (dto.DepreciationAmount.HasValue)
                    latestDepToUpdate.DepreciationAmount = dto.DepreciationAmount.Value;
                if (dto.AccumulatedDepreciation.HasValue)
                    latestDepToUpdate.AccumulatedDepreciation = dto.AccumulatedDepreciation.Value;
                if (dto.RemainingValue.HasValue)
                    latestDepToUpdate.RemainingValue = dto.RemainingValue.Value;
            }
            // Chưa có DepreciationRecord nào thì không tự tạo record tại API update.
            // Record sẽ được sinh bởi job cuối tháng hoặc chạy thủ công từ module khấu hao.
        }

        var assignmentError = await ApplyUpdateAssignmentAsync(id, dto);
        if (assignmentError != null)
            return assignmentError;

        await _context.SaveChangesAsync();

        try
        {
            var actorUserId = TryGetCurrentUserId();
            if (actorUserId.HasValue)
            {
                var roleId = await _context.UserRoles
                    .Where(ur => ur.UserId == actorUserId.Value)
                    .Select(ur => ur.RoleId)
                    .FirstOrDefaultAsync();
                if (roleId > 0)
                {
                    var updatedDesc = hasDepreciationPayload
                        ? "Chỉnh sửa thông tin khấu hao"
                        : hasWarrantyPayload
                            ? "Cập nhật thông tin bảo hành"
                            : "Chỉnh sửa thông tin cá thể";
                    _context.AssetLifeCycles.Add(new AssetLifeCycle
                    {
                        AssetInstanceId = id,
                        ActionType = (int)AssetLifeActionType.Updated,
                        RelatedEntityType = 2,
                        RelatedEntityId = id,
                        ActorUserId = actorUserId.Value,
                        ActorRoleId = roleId,
                        Description = updatedDesc,
                        OccurredAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
            }
        }
        catch
        {
            // Không hủy update nếu ghi log lifecycle thất bại
        }

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

        return Ok(ToDto(
            reloaded,
            latestDep,
            (await BuildUsageHistoriesByInstanceAsync(new List<AssetInstance> { reloaded }))
                .GetValueOrDefault(reloaded.AssetInstanceId),
            null));
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

        return Ok(ToDto(
            reloaded,
            latestDep,
            (await BuildUsageHistoriesByInstanceAsync(new List<AssetInstance> { reloaded }))
                .GetValueOrDefault(reloaded.AssetInstanceId),
            null));
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

        return Ok(ToDto(
            reloaded,
            null,
            (await BuildUsageHistoriesByInstanceAsync(new List<AssetInstance> { reloaded }))
                .GetValueOrDefault(reloaded.AssetInstanceId),
            null));
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

    private int? TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            return null;
        return userId;
    }

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
        List<AssetUsageHistoryDTO>? usageHistories,
        AssetStatus? forcedStatus,
        List<DepreciationRecord>? allDepreciationRecords = null)
    {
        var effectiveStatus = forcedStatus ?? (AssetStatus)i.Status;
        var dto = new AssetInstanceResponseDTO
        {
            AssetInstanceId = i.AssetInstanceId,
            AssetId = i.AssetId,
            AssetTypeId = i.Asset?.AssetTypeId ?? 0,
            AssetTypeName = i.Asset?.AssetType?.Name,
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
            UsageHistories = usageHistories,
            Guarantees = i.Guarantees?.Select(g => new GuaranteeDTO
            {
                GuaranteeId = g.GuaranteeId,
                WarrantyPeriodValue = g.WarrantyPeriodValue,
                WarrantyPeriodUnit = g.WarrantyPeriodUnit,
                WarrantyConditions = g.WarrantyConditions,
                StartDate = g.StartDate,
                WarrantyEndDate = g.WarrantyEndDate
            }).ToList(),
            DepreciationRecords = allDepreciationRecords?.Select(r => new DepreciationRecordDTO
            {
                RecordId = r.RecordId,
                Period = r.Period,
                DepreciationAmount = r.DepreciationAmount,
                OriginalValue = r.OriginalValue,
                RemainingValue = r.RemainingValue,
                AccumulatedDepreciation = r.AccumulatedDepreciation,
                CreateDate = r.CreateDate,
                IsPosted = r.IsPosted
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

    private static string GetLifecycleOperationLabel(int actionType) => actionType switch
    {
        (int)AssetLifeActionType.Created => "Tạo mới",
        (int)AssetLifeActionType.StatusChanged => "Thay đổi trạng thái",
        (int)AssetLifeActionType.Capitalized => "Vốn hóa",
        (int)AssetLifeActionType.Disposed => "Thanh lý",
        (int)AssetLifeActionType.Updated => "Chỉnh sửa thông tin",
        _ => "Hoạt động khác"
    };

    private async Task<Dictionary<int, List<AssetUsageHistoryDTO>>> BuildUsageHistoriesByInstanceAsync(
        List<AssetInstance> instances)
    {
        var result = instances.ToDictionary(
            i => i.AssetInstanceId,
            _ => new List<AssetUsageHistoryDTO>());
        if (instances.Count == 0)
            return result;

        var instanceIds = instances.Select(i => i.AssetInstanceId).ToList();

        var toLocationIds = instances
            .SelectMany(i => i.AssetLocations)
            .Select(l => l.LocationId)
            .Distinct()
            .ToList();

        var transferByToLocationId = toLocationIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.TransferRecords
                .AsNoTracking()
                .Where(t => toLocationIds.Contains(t.ToLocationId))
                .GroupBy(t => t.ToLocationId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.OrderByDescending(t => t.TransferDate)
                        .Select(t => t.AssetRequestId)
                        .First());

        var lifecycleActionTypes = new[]
        {
            (int)AssetLifeActionType.Created,
            (int)AssetLifeActionType.StatusChanged,
            (int)AssetLifeActionType.Capitalized,
            (int)AssetLifeActionType.Disposed,
            (int)AssetLifeActionType.Updated
        };
        var lifecyclesByInstance = await _context.AssetLifeCycles
            .AsNoTracking()
            .Where(lc => instanceIds.Contains(lc.AssetInstanceId) && lifecycleActionTypes.Contains(lc.ActionType))
            .GroupBy(lc => lc.AssetInstanceId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        foreach (var instance in instances)
        {
            var locationRows = instance.AssetLocations
                .Select(location =>
                {
                    transferByToLocationId.TryGetValue(location.LocationId, out var transferRequestId);
                    return new AssetUsageHistoryDTO
                    {
                        AssetInstanceId = instance.AssetInstanceId,
                        InstanceCode = instance.InstanceCode,
                        ExecutionDate = location.StartDate,
                        ReportNumber = transferRequestId > 0
                            ? $"YC-{transferRequestId}"
                            : $"VT-{location.LocationId}",
                        Operation = transferRequestId > 0 ? "Điều chuyển" : "Cập nhật vị trí",
                        Condition = instance.Condition,
                        Location = string.IsNullOrWhiteSpace(location.Note)
                            ? location.Department?.Name
                            : $"{location.Department?.Name} · {location.Note}",
                        Value = instance.CurrentValue
                    };
                });

            var lifecycleRows = lifecyclesByInstance.GetValueOrDefault(instance.AssetInstanceId, [])
                .Select(lc =>
                {
                    var eventDate = DateOnly.FromDateTime(lc.OccurredAt.ToLocalTime());
                    var locationAtTime = instance.AssetLocations
                        .Where(loc => loc.StartDate <= eventDate
                            && (loc.EndDate == null || loc.EndDate >= eventDate))
                        .OrderByDescending(loc => loc.StartDate)
                        .FirstOrDefault();
                    var locationLabel = locationAtTime != null
                        ? (string.IsNullOrWhiteSpace(locationAtTime.Note)
                            ? locationAtTime.Department?.Name
                            : $"{locationAtTime.Department?.Name} · {locationAtTime.Note}")
                        : null;
                    return new AssetUsageHistoryDTO
                    {
                        AssetInstanceId = instance.AssetInstanceId,
                        InstanceCode = instance.InstanceCode,
                        ExecutionDate = eventDate,
                        ReportNumber = $"LC-{lc.AuditId}",
                        Operation = GetLifecycleOperationLabel(lc.ActionType),
                        Condition = lc.Description,
                        Location = locationLabel,
                        Value = instance.CurrentValue
                    };
                });

            result[instance.AssetInstanceId] = locationRows
                .Concat(lifecycleRows)
                .OrderByDescending(r => r.ExecutionDate)
                .ThenByDescending(r => r.ReportNumber)
                .ToList();
        }

        return result;
    }
}
