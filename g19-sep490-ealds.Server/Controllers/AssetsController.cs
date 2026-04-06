using System.Globalization;
using System.Text.RegularExpressions;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            .Include(a => a.AssetUsages)
                .ThenInclude(u => u.Employee)
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
            .Include(a => a.AssetUsages)
                .ThenInclude(u => u.Employee)
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
    /// GET /api/assets/department/{departmentId} — Assets currently located in that department.
    /// </summary>
    [HttpGet("department/{departmentId:int}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AssetResponseDTO>>> GetAssetsByDepartment(
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

        var query = _context.Assets
            .Include(a => a.AssetType)
            .Include(a => a.Warehouse)
            .Include(a => a.AssetLocations)
                .ThenInclude(al => al.Department)
            .Include(a => a.AssetUsages)
                .ThenInclude(u => u.Employee)
            .AsNoTracking()
            .Where(a =>
                a.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == deptId) &&
                a.Status != (int)AssetStatus.Disposed &&
                a.Status != (int)AssetStatus.Lost &&
                a.Status != (int)AssetStatus.Liquidated);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(a =>
                a.Code.ToLower().Contains(kw) ||
                a.Name.ToLower().Contains(kw));
        }

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

        var assets = await query.ToListAsync();

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
    /// POST /api/assets - Create asset with General info and Depreciation settings
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetResponseDTO>> Create([FromBody] CreateAssetDTO dto)
    {
        if (CreateHasAssignment(dto))
        {
            var denied = RequireAccountantForAssignment();
            if (denied != null)
                return denied;
            var assignmentValidation = await ValidateCreateAssignmentDtoAsync(dto);
            if (assignmentValidation != null)
                return assignmentValidation;
        }

        if (await _context.Assets.AnyAsync(a => a.Code == dto.Code))
            return BadRequest(new { message = "Asset code already exists." });

        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0." });

        var unitTrim = dto.Unit.Trim();
        if (string.IsNullOrWhiteSpace(unitTrim))
            return BadRequest(new { message = "Unit of measure is required." });
        if (!IsUnitOfMeasureNumericOk(unitTrim))
            return BadRequest(new { message = "Đơn vị tính phải lớn hơn 0 khi nhập dạng số; hoặc nhập tên đơn vị (ví dụ: Cái, Bộ, kg)." });

        var purchaseToday = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.PurchaseDate > purchaseToday)
            return BadRequest(new { message = "Purchase date cannot be in the future." });

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
            Unit = unitTrim,
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

        var assignmentError = await ApplyCreateAssignmentAsync(asset.AssetId, dto);
        if (assignmentError != null)
            return assignmentError;
        if (CreateHasAssignment(dto))
            await _context.SaveChangesAsync();

        await _context.Entry(asset)
            .Reference(a => a.AssetType).LoadAsync();
        await _context.Entry(asset)
            .Reference(a => a.Warehouse).LoadAsync();
        await _context.Entry(asset)
            .Collection(a => a.AssetLocations).Query()
            .Include(al => al.Department).LoadAsync();
        await _context.Entry(asset)
            .Collection(a => a.AssetUsages).Query()
            .Include(u => u.Employee).LoadAsync();

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
        if (UpdateHasAssignment(dto))
        {
            var denied = RequireAccountantForAssignment();
            if (denied != null)
                return denied;
        }

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
        if (dto.Unit != null)
        {
            var u = dto.Unit.Trim();
            if (string.IsNullOrWhiteSpace(u))
                return BadRequest(new { message = "Đơn vị tính không được để trống." });
            if (!IsUnitOfMeasureNumericOk(u))
                return BadRequest(new { message = "Đơn vị tính phải lớn hơn 0 khi nhập dạng số; hoặc nhập tên đơn vị (ví dụ: Cái, Bộ, kg)." });
            asset.Unit = u;
        }
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

        var assignmentError = await ApplyUpdateAssignmentAsync(id, dto);
        if (assignmentError != null)
            return assignmentError;

        await _context.SaveChangesAsync();

        await _context.Entry(asset)
            .Reference(a => a.AssetType).LoadAsync();
        await _context.Entry(asset)
            .Reference(a => a.Warehouse).LoadAsync();
        await _context.Entry(asset)
            .Collection(a => a.AssetLocations).Query()
            .Include(al => al.Department).LoadAsync();
        await _context.Entry(asset)
            .Collection(a => a.AssetUsages).Query()
            .Include(u => u.Employee).LoadAsync();

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
    /// PUT /api/assets/{id}/status — Accountants set operational status (see <see cref="AssetStatus"/>).
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<ActionResult<AssetResponseDTO>> ChangeStatus(int id, [FromBody] ChangeAssetStatusDTO dto)
    {
        if (!Enum.IsDefined(typeof(AssetStatus), dto.Status))
            return BadRequest(new { message = "Invalid asset status value." });

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return NotFound();

        asset.Status = (int)dto.Status;
        await _context.SaveChangesAsync();

        await _context.Entry(asset)
            .Reference(a => a.AssetType).LoadAsync();
        await _context.Entry(asset)
            .Reference(a => a.Warehouse).LoadAsync();
        await _context.Entry(asset)
            .Collection(a => a.AssetLocations).Query()
            .Include(al => al.Department).LoadAsync();
        await _context.Entry(asset)
            .Collection(a => a.AssetUsages).Query()
            .Include(u => u.Employee).LoadAsync();

        var latestDep = await _context.DrepreciationRecords
            .Include(r => r.Policy)
            .AsNoTracking()
            .Where(r => r.AssetId == id)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .FirstOrDefaultAsync();

        var schedules = new List<MaintenanceSchedule>();

        var hasDamageReport = await _context.AssetRequests
            .AsNoTracking()
            .AnyAsync(r =>
                r.AssetId == id &&
                r.Title != null &&
                r.Title.StartsWith("Damage report") &&
                r.Status == 0);

        var response = ToResponseDTO(asset, latestDep, schedules);
        if (hasDamageReport)
        {
            response.Status = AssetStatus.Damaged;
            response.StatusName = AssetStatus.Damaged.ToString();
        }

        return Ok(response);
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

    private bool CanViewDepartmentAssetsForAnyDepartment() =>
        User.IsInRole("ACCOUNTANT") || User.IsInRole("DIRECTOR");

    private static bool CreateHasAssignment(CreateAssetDTO dto) =>
        dto.AssignedDepartmentId.HasValue || dto.ResponsibleEmployeeId.HasValue;

    private static bool UpdateHasAssignment(UpdateAssetDTO dto) =>
        dto.AssignedDepartmentId.HasValue ||
        dto.ResponsibleEmployeeId.HasValue ||
        dto.ClearDepartmentAssignment ||
        dto.ClearResponsibleEmployee;

    private ActionResult<AssetResponseDTO>? RequireAccountantForAssignment()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { message = "Authentication is required to assign or reassign assets." });
        if (!User.IsInRole("ACCOUNTANT"))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only accountants may assign or reassign assets." });
        return null;
    }

    private async Task<ActionResult<AssetResponseDTO>?> ValidateCreateAssignmentDtoAsync(CreateAssetDTO dto)
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

        if (emp != null)
        {
            if (deptId.HasValue && deptId.Value != emp.DepartmentId)
                return BadRequest(new { message = "Assigned department must match the responsible employee's department." });
        }

        return null;
    }

    private async Task<ActionResult<AssetResponseDTO>?> ApplyCreateAssignmentAsync(int assetId, CreateAssetDTO dto)
    {
        if (!dto.AssignedDepartmentId.HasValue && !dto.ResponsibleEmployeeId.HasValue)
            return null;

        var effective = dto.AssignmentEffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        Employee? emp = null;
        if (dto.ResponsibleEmployeeId.HasValue)
            emp = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == dto.ResponsibleEmployeeId.Value);

        int? deptId = dto.AssignedDepartmentId;
        if (emp != null)
        {
            if (!deptId.HasValue)
                deptId = emp.DepartmentId;
        }

        if (deptId.HasValue)
        {
            await CloseCurrentLocationAsync(assetId, excludeLocationId: null, newStartDate: effective);
            _context.AssetLocations.Add(new AssetLocation
            {
                AssetId = assetId,
                DepartmentId = deptId.Value,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }

        if (dto.ResponsibleEmployeeId.HasValue)
        {
            await CloseCurrentUsageAsync(assetId, newStartDate: effective);
            _context.AssetUsages.Add(new AssetUsage
            {
                AssetId = assetId,
                EmployeeId = dto.ResponsibleEmployeeId.Value,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }

        return null;
    }

    private async Task<ActionResult<AssetResponseDTO>?> ApplyUpdateAssignmentAsync(int assetId, UpdateAssetDTO dto)
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

        // Department side
        if (dto.ClearDepartmentAssignment)
        {
            await CloseCurrentLocationAsync(assetId, excludeLocationId: null, newStartDate: effective);
        }
        else if (dto.AssignedDepartmentId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == dto.AssignedDepartmentId.Value))
                return BadRequest(new { message = $"Department {dto.AssignedDepartmentId.Value} does not exist." });

            if (emp != null && dto.AssignedDepartmentId.Value != emp.DepartmentId)
                return BadRequest(new { message = "Assigned department must match the responsible employee's department." });

            await CloseCurrentLocationAsync(assetId, excludeLocationId: null, newStartDate: effective);
            _context.AssetLocations.Add(new AssetLocation
            {
                AssetId = assetId,
                DepartmentId = dto.AssignedDepartmentId.Value,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }
        else if (dto.ResponsibleEmployeeId.HasValue && emp != null)
        {
            await CloseCurrentLocationAsync(assetId, excludeLocationId: null, newStartDate: effective);
            _context.AssetLocations.Add(new AssetLocation
            {
                AssetId = assetId,
                DepartmentId = emp.DepartmentId,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }

        // Usage side
        if (dto.ClearResponsibleEmployee)
        {
            await CloseCurrentUsageAsync(assetId, newStartDate: effective);
        }
        else if (dto.ResponsibleEmployeeId.HasValue)
        {
            await CloseCurrentUsageAsync(assetId, newStartDate: effective);
            _context.AssetUsages.Add(new AssetUsage
            {
                AssetId = assetId,
                EmployeeId = dto.ResponsibleEmployeeId.Value,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }

        return null;
    }

    private async Task CloseCurrentLocationAsync(int assetId, int? excludeLocationId, DateOnly newStartDate)
    {
        var current = await _context.AssetLocations
            .Where(l => l.AssetId == assetId && l.IsCurrent &&
                        (excludeLocationId == null || l.LocationId != excludeLocationId))
            .FirstOrDefaultAsync();

        if (current != null)
        {
            current.IsCurrent = false;
            current.EndDate = newStartDate.AddDays(-1);
        }
    }

    private async Task CloseCurrentUsageAsync(int assetId, DateOnly newStartDate)
    {
        var current = await _context.AssetUsages
            .Where(u => u.AssetId == assetId && u.IsCurrent)
            .FirstOrDefaultAsync();

        if (current != null)
        {
            current.IsCurrent = false;
            current.EndDate = newStartDate.AddDays(-1);
        }
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
                .FirstOrDefault(),
            CurrentResponsibleEmployeeId = a.AssetUsages
                .Where(u => u.IsCurrent)
                .Select(u => (int?)u.EmployeeId)
                .FirstOrDefault(),
            CurrentResponsibleEmployeeName = a.AssetUsages
                .Where(u => u.IsCurrent)
                .Select(u => u.Employee != null ? u.Employee.Name : null)
                .FirstOrDefault(),
            CurrentResponsibleUserId = a.AssetUsages
                .Where(u => u.IsCurrent)
                .Select(u => u.Employee != null ? (int?)u.Employee.UserId : null)
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

    /// <summary>
    /// Text units (e.g. "Cái", "kg") are valid. If the entire value is numeric, it must be &gt; 0.
    /// </summary>
    private static bool IsUnitOfMeasureNumericOk(string trimmedUnit)
    {
        if (!Regex.IsMatch(trimmedUnit, @"^-?\d+(\.\d+)?$"))
            return true;
        return decimal.TryParse(trimmedUnit, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) && n > 0;
    }
}
