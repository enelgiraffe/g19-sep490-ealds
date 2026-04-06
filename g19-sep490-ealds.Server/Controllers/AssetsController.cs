using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly EaldsDbContext _context;
    private readonly int _departmentHeadRoleId;

    public AssetsController(EaldsDbContext context, IConfiguration configuration)
    {
        _context = context;
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
    }

    /// <summary>
    /// GET /api/assets — Catalog assets (keyword, type, catalog status).
    /// Filters for warehouse, price, purchase date, and per-instance location belong on <c>GET /api/assetinstances</c>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetResponseDTO>>> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status,
        [FromQuery] int? assetTypeId)
    {
        var query = _context.Assets
            .Include(a => a.AssetType)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(a =>
                a.Code.ToLower().Contains(kw) ||
                a.Name.ToLower().Contains(kw));
        }

        if (status.HasValue)
            query = query.Where(a => a.Status == (int)status.Value);

        if (assetTypeId.HasValue)
            query = query.Where(a => a.AssetTypeId == assetTypeId.Value);

        var scope = await DepartmentAssetScope.ResolveForUserAsync(User, _context, _departmentHeadRoleId);
        if (scope.IsRestricted && !scope.DepartmentId.HasValue)
            return Ok(Array.Empty<AssetResponseDTO>());

        if (scope.IsRestricted && scope.DepartmentId is int scopedDept && scopedDept > 0)
        {
            query = query.Where(a => _context.AssetInstances.Any(i =>
                i.AssetId == a.AssetId &&
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == scopedDept) &&
                i.Status != (int)AssetStatus.Disposed &&
                i.Status != (int)AssetStatus.Lost &&
                i.Status != (int)AssetStatus.Liquidated));
        }

        var assets = await query.ToListAsync();

        return Ok(assets.Select(a => ToAssetResponseDTO(a)));
    }

    /// <summary>
    /// GET /api/assets/code-prefixes — Distinct catalog code prefixes (trailing digits stripped) for datalist / suggestions.
    /// </summary>
    [HttpGet("code-prefixes")]
    public async Task<ActionResult<IEnumerable<string>>> GetAssetCodePrefixes()
    {
        var codes = await _context.Assets
            .AsNoTracking()
            .Where(a => a.Code != null && a.Code != string.Empty)
            .Select(a => a.Code!)
            .ToListAsync();

        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in codes)
        {
            var trimmed = code.Trim();
            if (!EndsWithDigit(trimmed))
                continue;
            var p = StripTrailingDigitsPrefix(trimmed);
            if (!string.IsNullOrWhiteSpace(p))
                prefixes.Add(p);
        }

        return Ok(prefixes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>
    /// GET /api/assets/{id} — Catalog detail, maintenance schedules, documents, and all instances.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetDetailResponseDTO>> GetById(int id)
    {
        var scope = await DepartmentAssetScope.ResolveForUserAsync(User, _context, _departmentHeadRoleId);
        if (scope.IsRestricted && !scope.DepartmentId.HasValue)
            return NotFound();

        var dto = await BuildAssetDetailAsync(id, scope);
        if (dto == null)
            return NotFound();
        if (scope.IsRestricted && scope.DepartmentId.HasValue && (dto.Instances?.Count ?? 0) == 0)
            return NotFound();
        return Ok(dto);
    }

    private async Task<AssetDetailResponseDTO?> BuildAssetDetailAsync(int id, AssetDepartmentScope scope = default)
    {
        var asset = await _context.Assets
            .Include(a => a.AssetType)
            .Include(a => a.MaintenanceSchedules).ThenInclude(s => s.Template)
            .Include(a => a.AssetInstances).ThenInclude(i => i.MaintenanceSchedules).ThenInclude(s => s.Template)
            .Include(a => a.AssetInstances).ThenInclude(i => i.Warehouse)
            .Include(a => a.AssetInstances).ThenInclude(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(a => a.AssetInstances).ThenInclude(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetId == id);

        if (asset == null)
            return null;

        var visibleInstances = asset.AssetInstances.AsEnumerable();
        if (scope.IsRestricted && scope.DepartmentId is int scopedDeptId)
            visibleInstances = visibleInstances.Where(i => DepartmentAssetScope.InstanceBelongsToDepartment(i, scopedDeptId));

        var instanceList = visibleInstances.OrderBy(i => i.InstanceCode).ToList();
        var instanceIds = instanceList.Select(i => i.AssetInstanceId).ToList();
        var latestDepsByInstance = await LoadLatestDepreciationByInstanceAsync(instanceIds);

        var documents = await _context.Documents
            .AsNoTracking()
            .Where(d => d.AssetId == id)
            .OrderByDescending(d => d.UploadedDate)
            .Select(d => new AssetDocumentDTO
            {
                DocumentId = d.DocumentId,
                DocumentType = d.DocumentType,
                FileUrl = d.FileUrl,
                UploadedDate = d.UploadedDate
            })
            .ToListAsync();

        var schedules = asset.MaintenanceSchedules
            .Where(s => s.IsActive)
            .Select(ToMaintenanceScheduleDto)
            .Concat(
                instanceList.SelectMany(inst =>
                    inst.MaintenanceSchedules.Where(s => s.IsActive).Select(s =>
                    {
                        var schedDto = ToMaintenanceScheduleDto(s);
                        schedDto.AssetInstanceId = inst.AssetInstanceId;
                        schedDto.InstanceCode = inst.InstanceCode;
                        return schedDto;
                    })))
            .OrderBy(s => s.ScheduleId)
            .ToList();

        var baseDto = ToAssetResponseDTO(asset, null);

        var dto = new AssetDetailResponseDTO
        {
            AssetId = baseDto.AssetId,
            Code = baseDto.Code,
            Name = baseDto.Name,
            AssetTypeId = baseDto.AssetTypeId,
            AssetTypeName = baseDto.AssetTypeName,
            Status = baseDto.Status,
            StatusName = baseDto.StatusName,
            Unit = baseDto.Unit,
            Quantity = baseDto.Quantity,
            CreatedBy = baseDto.CreatedBy,
            InUseDate = baseDto.InUseDate,
            Specification = baseDto.Specification,
            Note = baseDto.Note,
            Documents = documents,
            MaintenanceSchedules = schedules,
            Instances = instanceList
                .Select(i => ToAssetInstanceResponseDTO(
                    i,
                    latestDepsByInstance.GetValueOrDefault(i.AssetInstanceId)))
                .ToList()
        };

        return dto;
    }

    /// <summary>
    /// GET /api/assets/department/{departmentId} — Instances currently located in that department.
    /// </summary>
    [HttpGet("department/{departmentId:int}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AssetInstanceResponseDTO>>> GetAssetsByDepartment(
        int departmentId,
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status)
    {
        if (!await _context.Departments.AnyAsync(d => d.DepartmentId == departmentId))
            return NotFound(new { message = $"Department {departmentId} not found." });

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            return Unauthorized(new { message = "Invalid user identity." });

        if (!CanViewDepartmentAssetsForAnyDepartment())
        {
            var employee = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
                return NotFound(new { message = "No employee profile linked to this user." });
            if (employee.DepartmentId != departmentId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "You can only view assets for your own department." });
        }

        var deptId = departmentId;

        var query = _context.AssetInstances
            .Include(i => i.Asset).ThenInclude(a => a!.AssetType)
            .Include(i => i.Warehouse)
            .Include(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .AsNoTracking()
            .Where(i =>
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == deptId) &&
                i.Status != (int)AssetStatus.Disposed &&
                i.Status != (int)AssetStatus.Lost &&
                i.Status != (int)AssetStatus.Liquidated);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(i =>
                i.InstanceCode.ToLower().Contains(kw) ||
                (i.Asset != null && i.Asset.Code.ToLower().Contains(kw)) ||
                (i.Asset != null && i.Asset.Name.ToLower().Contains(kw)));
        }

        if (status.HasValue)
            query = query.Where(i => i.Status == (int)status.Value);

        var instances = await query.ToListAsync();

        var instanceIds = instances.Select(i => i.AssetInstanceId).ToList();
        var latestDeps = await LoadLatestDepreciationByInstanceAsync(instanceIds);

        var result = instances.Select(i =>
            ToAssetInstanceResponseDTO(i, latestDeps.GetValueOrDefault(i.AssetInstanceId), null)).ToList();

        return Ok(result);
    }

    /// <summary>
    /// POST /api/assets — Create catalog asset; optional <see cref="CreateAssetDTO.InitialInstance"/> creates the first row in <c>AssetInstance</c>.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetDetailResponseDTO>> Create([FromBody] CreateAssetDTO dto)
    {
        if (dto.InitialInstance != null)
        {
            if (InstanceCreateHasAssignment(dto.InitialInstance))
            {
                var denied = RequireAccountantForAssignment();
                if (denied != null)
                    return denied;
                var assignmentValidation = await ValidateCreateInstanceAssignmentAsync(dto.InitialInstance);
                if (assignmentValidation != null)
                    return assignmentValidation;
            }
        }

        string catalogCode;
        if (!string.IsNullOrWhiteSpace(dto.AssetCodePrefix))
        {
            var prefix = dto.AssetCodePrefix.Trim();
            if (!IsValidInstanceCodePrefix(prefix))
                return BadRequest(new { message = "Invalid asset code prefix (letters/digits only, 1–32 characters)." });

            var generated = await GenerateAssetCatalogCodesForPrefixAsync(prefix, 1);
            catalogCode = generated[0];
            if (await _context.Assets.AnyAsync(a => a.Code == catalogCode))
                return BadRequest(new { message = $"Asset code {catalogCode} already exists." });
        }
        else
        {
            catalogCode = (dto.Code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(catalogCode))
                return BadRequest(new { message = "Asset code or asset code prefix is required." });

            if (await _context.Assets.AnyAsync(a => a.Code == catalogCode))
                return BadRequest(new { message = "Asset code already exists." });
        }

        var asset = new Asset
        {
            Code = catalogCode,
            Name = dto.Name,
            AssetTypeId = dto.AssetTypeId,
            Status = (int)AssetStatus.Available,
            Unit = dto.Unit,
            Quantity = dto.Quantity,
            CreatedBy = dto.CreatedBy,
            InUseDate = dto.InUseDate,
            Specification = dto.Specification,
            Note = dto.Note
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        if (dto.InitialInstance != null)
        {
            var init = dto.InitialInstance;
            var qty = dto.Quantity ?? 1;
            if (qty < 1)
                return BadRequest(new { message = "Quantity must be at least 1." });

            if (!await _context.Warehouses.AnyAsync(w => w.WarehouseId == init.WarehouseId))
                return BadRequest(new { message = $"WarehouseId {init.WarehouseId} does not exist." });

            List<string> instanceCodes;
            if (!string.IsNullOrWhiteSpace(dto.InstanceCodePrefix))
            {
                var prefix = dto.InstanceCodePrefix.Trim();
                if (!IsValidInstanceCodePrefix(prefix))
                    return BadRequest(new { message = "Invalid instance code prefix (letters/digits only, 1–32 characters)." });

                instanceCodes = await GenerateInstanceCodesForPrefixAsync(prefix, qty);
                foreach (var code in instanceCodes)
                {
                    if (await _context.AssetInstances.AnyAsync(i => i.InstanceCode == code))
                        return BadRequest(new { message = $"Instance code {code} already exists." });
                }
            }
            else
            {
                if (qty > 1)
                    return BadRequest(new { message = "Instance code prefix is required when quantity is greater than 1." });

                if (await _context.AssetInstances.AnyAsync(i => i.InstanceCode == init.InstanceCode))
                    return BadRequest(new { message = "Instance code already exists." });

                instanceCodes = new List<string> { init.InstanceCode };
            }

            var (originals, currents) = SplitValueAcrossInstances(init.OriginalPrice, init.CurrentValue, qty);

            for (var index = 0; index < qty; index++)
            {
                var code = instanceCodes[index];
                var serial = qty == 1 ? init.SerialNumber : null;

                var instance = new AssetInstance
                {
                    AssetId = asset.AssetId,
                    WarehouseId = init.WarehouseId,
                    DepreciationPolicyId = init.DepreciationPolicyId,
                    InstanceCode = code,
                    SerialNumber = serial,
                    Status = (int)AssetStatus.Available,
                    InUseDate = init.InUseDate,
                    PurchaseDate = init.PurchaseDate,
                    OriginalPrice = originals[index],
                    CurrentValue = currents[index],
                    SupplierId = init.SupplierId,
                    ContractNo = init.ContractNo,
                    Condition = init.Condition,
                    Note = init.Note
                };
                _context.AssetInstances.Add(instance);
                await _context.SaveChangesAsync();

                if (init.DepreciationPolicyId.HasValue)
                {
                    var policy = await _context.DepreciationPolicies.FindAsync(init.DepreciationPolicyId.Value);
                    if (policy != null)
                    {
                        var firstPeriod = new DateOnly(init.PurchaseDate.Year, init.PurchaseDate.Month, 1);
                        _context.DepreciationRecords.Add(new DepreciationRecord
                        {
                            AssetInstanceId = instance.AssetInstanceId,
                            PolicyId = policy.PolicyId,
                            Period = firstPeriod,
                            DepreciationAmount = 0,
                            AccumulatedDepreciation = 0,
                            OriginalValue = currents[index],
                            RemainingValue = currents[index],
                            CreateDate = DateTime.UtcNow,
                            IsPosted = false
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                await ApplyCreateInstanceAssignmentAsync(instance.AssetInstanceId, init);
                if (InstanceCreateHasAssignment(init))
                    await _context.SaveChangesAsync();
            }
        }

        var created = await BuildAssetDetailAsync(asset.AssetId);
        if (created == null)
            return NotFound();
        return CreatedAtAction(nameof(GetById), new { id = asset.AssetId }, created);
    }

    /// <summary>
    /// PUT /api/assets/{id} — Update catalog fields only.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetDetailResponseDTO>> Update(int id, [FromBody] UpdateAssetDTO dto)
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
        if (dto.Status.HasValue) asset.Status = (int)dto.Status.Value;
        if (dto.Unit != null) asset.Unit = dto.Unit;
        if (dto.Quantity.HasValue) asset.Quantity = dto.Quantity.Value;
        if (dto.InUseDate.HasValue) asset.InUseDate = dto.InUseDate;
        if (dto.Specification != null) asset.Specification = dto.Specification;
        if (dto.Note != null) asset.Note = dto.Note;

        await _context.SaveChangesAsync();
        var updated = await BuildAssetDetailAsync(id);
        if (updated == null)
            return NotFound();
        return Ok(updated);
    }

    /// <summary>
    /// PUT /api/assets/{id}/status — Accountants set catalog-level status.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<ActionResult<AssetDetailResponseDTO>> ChangeStatus(int id, [FromBody] ChangeAssetStatusDTO dto)
    {
        if (!Enum.IsDefined(typeof(AssetStatus), dto.Status))
            return BadRequest(new { message = "Invalid asset status value." });

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return NotFound();

        asset.Status = (int)dto.Status;
        await _context.SaveChangesAsync();

        var detail = await BuildAssetDetailAsync(id);
        if (detail == null)
            return NotFound();
        return Ok(detail);
    }

    /// <summary>
    /// DELETE /api/assets/{id} — Set catalog status to Disposed, Lost, or Liquidated.
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

        await _context.Entry(asset).Reference(a => a.AssetType).LoadAsync();
        return Ok(ToAssetResponseDTO(asset));
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

    private bool CanViewDepartmentAssetsForAnyDepartment() =>
        User.IsInRole("ACCOUNTANT") || User.IsInRole("DIRECTOR");

    private static bool InstanceCreateHasAssignment(CreateAssetInstanceDTO dto) =>
        dto.AssignedDepartmentId.HasValue || dto.ResponsibleEmployeeId.HasValue;

    private ActionResult<AssetDetailResponseDTO>? RequireAccountantForAssignment()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { message = "Authentication is required to assign or reassign assets." });
        if (!User.IsInRole("ACCOUNTANT"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only accountants may assign or reassign assets." });
        return null;
    }

    private async Task<ActionResult<AssetDetailResponseDTO>?> ValidateCreateInstanceAssignmentAsync(
        CreateAssetInstanceDTO dto)
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

    private async Task ApplyCreateInstanceAssignmentAsync(
        int assetInstanceId,
        CreateAssetInstanceDTO dto)
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
            await CloseCurrentLocationForInstanceAsync(assetInstanceId, null, effective);
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
            await CloseCurrentUsageForInstanceAsync(assetInstanceId, effective);
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

    private async Task CloseCurrentLocationForInstanceAsync(
        int assetInstanceId,
        int? excludeLocationId,
        DateOnly newStartDate)
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

    private async Task CloseCurrentUsageForInstanceAsync(int assetInstanceId, DateOnly newStartDate)
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

    private static bool IsValidInstanceCodePrefix(string prefix) =>
        prefix.Length is >= 1 and <= 32 && prefix.All(char.IsLetterOrDigit);

    private async Task<List<string>> GenerateInstanceCodesForPrefixAsync(string prefix, int count)
    {
        var codes = await _context.AssetInstances
            .AsNoTracking()
            .Select(i => i.InstanceCode)
            .ToListAsync();

        return GenerateSequentialCodesForPrefix(prefix, count, codes);
    }

    private async Task<List<string>> GenerateAssetCatalogCodesForPrefixAsync(string prefix, int count)
    {
        var codes = await _context.Assets
            .AsNoTracking()
            .Select(a => a.Code)
            .ToListAsync();

        return GenerateSequentialCodesForPrefix(prefix, count, codes);
    }

    private static List<string> GenerateSequentialCodesForPrefix(string prefix, int count, List<string> existingCodes)
    {
        var maxSuffix = 0;
        foreach (var code in existingCodes)
        {
            if (code.Length <= prefix.Length)
                continue;
            if (!code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            var suffix = code[prefix.Length..];
            if (suffix.Length == 0 || !suffix.All(char.IsDigit))
                continue;
            if (int.TryParse(suffix, System.Globalization.NumberStyles.Integer, null, out var n))
                maxSuffix = Math.Max(maxSuffix, n);
        }

        var endNumber = maxSuffix + count;
        var width = Math.Max(2, endNumber.ToString().Length);
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var num = maxSuffix + 1 + i;
            list.Add(prefix + num.ToString().PadLeft(width, '0'));
        }

        return list;
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

    private static (decimal[] Originals, decimal[] Currents) SplitValueAcrossInstances(
        decimal originalPrice,
        decimal currentValue,
        int qty)
    {
        var o = new decimal[qty];
        var c = new decimal[qty];
        if (qty == 1)
        {
            o[0] = originalPrice;
            c[0] = currentValue;
            return (o, c);
        }

        var oEach = Math.Round(originalPrice / qty, 2, MidpointRounding.AwayFromZero);
        var cEach = Math.Round(currentValue / qty, 2, MidpointRounding.AwayFromZero);
        for (var i = 0; i < qty - 1; i++)
        {
            o[i] = oEach;
            c[i] = cEach;
        }

        o[qty - 1] = originalPrice - oEach * (qty - 1);
        c[qty - 1] = currentValue - cEach * (qty - 1);
        return (o, c);
    }

    private static AssetResponseDTO ToAssetResponseDTO(Asset a, AssetStatus? forcedStatus = null)
    {
        var effectiveStatus = forcedStatus ?? (AssetStatus)a.Status;
        return new AssetResponseDTO
        {
            AssetId = a.AssetId,
            Code = a.Code,
            Name = a.Name,
            AssetTypeId = a.AssetTypeId,
            AssetTypeName = a.AssetType?.Name,
            Status = effectiveStatus,
            StatusName = effectiveStatus.ToString(),
            Unit = a.Unit,
            Quantity = a.Quantity,
            CreatedBy = a.CreatedBy,
            InUseDate = a.InUseDate,
            Specification = a.Specification,
            Note = a.Note
        };
    }

    private static AssetInstanceResponseDTO ToAssetInstanceResponseDTO(
        AssetInstance i,
        DepreciationRecord? latestDep,
        AssetStatus? forcedStatus = null)
    {
        var effectiveStatus = forcedStatus ?? (AssetStatus)i.Status;
        var dto = new AssetInstanceResponseDTO
        {
            AssetInstanceId = i.AssetInstanceId,
            AssetId = i.AssetId,
            AssetTypeId = i.Asset?.AssetTypeId ?? 0,
            AssetCode = i.Asset?.Code,
            AssetName = i.Asset?.Name,
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
            DepreciationPolicyId = i.DepreciationPolicyId
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

    private static MaintenanceScheduleDTO ToMaintenanceScheduleDto(MaintenanceSchedule s)
    {
        int? intervalMonths = s.IntervalUnit == (int)MaintenanceRepeatIntervalUnit.Month
            ? s.IntervalValue
            : null;
        int? intervalHours = null;

        return new MaintenanceScheduleDTO
        {
            ScheduleId = s.ScheduleId,
            AssetInstanceId = s.AssetInstanceId,
            InstanceCode = s.AssetInstance?.InstanceCode,
            TemplateId = s.TemplateId,
            Content = s.Content,
            TemplateName = s.Template?.Name,
            ScheduleType = s.ScheduleType,
            IntervalMonths = intervalMonths,
            IntervalHours = intervalHours,
            IntervalValue = s.IntervalValue,
            IntervalUnit = s.IntervalUnit,
            StartDate = s.StartDate,
            NextDueDate = s.NextDueDate,
            EndDate = s.EndDate,
            IsActive = s.IsActive
        };
    }
}
