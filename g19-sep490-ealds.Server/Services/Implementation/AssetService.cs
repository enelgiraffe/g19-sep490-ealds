using System.Security.Claims;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.DTOs.Assets;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetService : IAssetService
{
    private const string FixedAssetMarker = "[FIXED_ASSET]";
    private readonly EaldsDbContext _context;
    private readonly ILogger<AssetService> _logger;
    private readonly IMaintenanceTemplateService _maintenanceTemplates;
    private readonly int _departmentHeadRoleId;

    public AssetService(
        EaldsDbContext context,
        ILogger<AssetService> logger,
        IMaintenanceTemplateService maintenanceTemplates,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _maintenanceTemplates = maintenanceTemplates;
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
    }

    public async Task<IEnumerable<AssetResponseDTO>> GetAllAsync(
        ClaimsPrincipal user,
        string? keyword,
        AssetStatus? status,
        int? assetTypeId,
        bool warehouseStockOnly)
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

        var scope = await DepartmentAssetScope.ResolveForUserAsync(user, _context, _departmentHeadRoleId);
        if (scope.IsRestricted && !scope.DepartmentId.HasValue)
            return Array.Empty<AssetResponseDTO>();

        if (warehouseStockOnly)
        {
            query = query.Where(a => _context.AssetInstances.Any(i =>
                i.AssetId == a.AssetId &&
                !i.AssetLocations.Any(al => al.IsCurrent)));
        }
        else if (scope.IsRestricted && scope.DepartmentId is int scopedDept && scopedDept > 0)
        {
            query = query.Where(a => _context.AssetInstances.Any(i =>
                i.AssetId == a.AssetId &&
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == scopedDept) &&
                i.Status != (int)AssetStatus.Disposed &&
                i.Status != (int)AssetStatus.Lost &&
                i.Status != (int)AssetStatus.Liquidated));
        }

        var assets = await query.ToListAsync();
        return assets.Select(a => ToAssetResponseDTO(a));
    }

    public async Task<IReadOnlyList<int>> GetCatalogEligibleAssetTypeIdsAsync(ClaimsPrincipal user, bool forAllocation)
    {
        var scope = await DepartmentAssetScope.ResolveForUserAsync(user, _context, _departmentHeadRoleId);
        if (scope.IsRestricted && !scope.DepartmentId.HasValue)
            return Array.Empty<int>();

        if (forAllocation)
        {
            return await _context.Assets
                .AsNoTracking()
                .Where(a => _context.AssetInstances.Any(i =>
                    i.AssetId == a.AssetId &&
                    !i.AssetLocations.Any(al => al.IsCurrent)))
                .Select(a => a.AssetTypeId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();
        }

        IQueryable<Asset> handoverQuery = _context.Assets.AsNoTracking();
        if (scope.IsRestricted && scope.DepartmentId is int scopedDept && scopedDept > 0)
        {
            handoverQuery = handoverQuery.Where(a => _context.AssetInstances.Any(i =>
                i.AssetId == a.AssetId &&
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == scopedDept) &&
                i.Status != (int)AssetStatus.Disposed &&
                i.Status != (int)AssetStatus.Lost &&
                i.Status != (int)AssetStatus.Liquidated));
        }
        else
        {
            handoverQuery = handoverQuery.Where(a => _context.AssetInstances.Any(i =>
                i.AssetId == a.AssetId &&
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId > 0) &&
                i.Status != (int)AssetStatus.Disposed &&
                i.Status != (int)AssetStatus.Lost &&
                i.Status != (int)AssetStatus.Liquidated));
        }

        return await handoverQuery
            .Select(a => a.AssetTypeId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAssetCodePrefixesAsync()
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

        return prefixes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<AssetDetailResponseDTO> GetByIdAsync(ClaimsPrincipal user, int id)
    {
        var scope = await DepartmentAssetScope.ResolveForUserAsync(user, _context, _departmentHeadRoleId);
        if (scope.IsRestricted && !scope.DepartmentId.HasValue)
            throw new KeyNotFoundException();

        var dto = await BuildAssetDetailAsync(id, scope);
        if (dto == null)
            throw new KeyNotFoundException();
        if (scope.IsRestricted && scope.DepartmentId.HasValue && (dto.Instances?.Count ?? 0) == 0)
            throw new KeyNotFoundException();
        return dto;
    }

    public async Task<IEnumerable<AssetInstanceResponseDTO>> GetAssetsByDepartmentAsync(
        ClaimsPrincipal user,
        int userId,
        int departmentId,
        string? keyword,
        AssetStatus? status)
    {
        if (!await _context.Departments.AnyAsync(d => d.DepartmentId == departmentId))
            throw new KeyNotFoundException($"Department {departmentId} not found.");

        if (!CanViewDepartmentAssetsForAnyDepartment(user))
        {
            var employee = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
                throw new KeyNotFoundException("No employee profile linked to this user.");
            if (employee.DepartmentId != departmentId)
                throw new UnauthorizedAccessException("You can only view assets for your own department.");
        }

        var query = _context.AssetInstances
            .Include(i => i.Asset).ThenInclude(a => a!.AssetType)
            .Include(i => i.Warehouse)
            .Include(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .AsNoTracking()
            .Where(i =>
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == departmentId) &&
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

        return instances.Select(i =>
            ToAssetInstanceResponseDTO(i, latestDeps.GetValueOrDefault(i.AssetInstanceId), null)).ToList();
    }

    public async Task<AssetDetailResponseDTO> CreateAsync(ClaimsPrincipal user, int? actorUserId, CreateAssetDTO dto)
    {
        if (dto.InitialInstance != null && InstanceCreateHasAssignment(dto.InitialInstance))
        {
            if (!user.IsInRole("ACCOUNTANT"))
                throw new UnauthorizedAccessException("Only accountants may assign or reassign assets.");
            await ValidateCreateInstanceAssignmentAsync(dto.InitialInstance);
        }

        string catalogCode;
        if (!string.IsNullOrWhiteSpace(dto.AssetCodePrefix))
        {
            var prefix = dto.AssetCodePrefix.Trim();
            if (!IsValidInstanceCodePrefix(prefix))
                throw new InvalidOperationException("Invalid asset code prefix (letters/digits only, 1–32 characters).");

            var generated = await GenerateAssetCatalogCodesForPrefixAsync(prefix, 1);
            catalogCode = generated[0];
            if (await _context.Assets.AnyAsync(a => a.Code == catalogCode))
                throw new InvalidOperationException($"Asset code {catalogCode} already exists.");
        }
        else
        {
            catalogCode = (dto.Code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(catalogCode))
                throw new InvalidOperationException("Asset code or asset code prefix is required.");

            if (await _context.Assets.AnyAsync(a => a.Code == catalogCode))
                throw new InvalidOperationException("Asset code already exists.");
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
            Note = BuildAssetNote(dto.Note, dto.IsFixedAsset || dto.InitialInstance?.IsFixedAsset == true)
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        if (dto.Documents is { Count: > 0 })
            await TryAddAssetDocumentsAsync(asset.AssetId, dto.Documents, dto.CreatedBy);

        if (dto.InitialInstance != null)
        {
            var init = dto.InitialInstance;
            var qty = dto.Quantity ?? 1;
            if (qty < 1)
                throw new InvalidOperationException("Quantity must be at least 1.");

            if (!await _context.Warehouses.AnyAsync(w => w.WarehouseId == init.WarehouseId))
                throw new InvalidOperationException($"WarehouseId {init.WarehouseId} does not exist.");

            List<string> instanceCodes;
            if (!string.IsNullOrWhiteSpace(dto.InstanceCodePrefix))
            {
                var prefix = dto.InstanceCodePrefix.Trim();
                if (!IsValidInstanceCodePrefix(prefix))
                    throw new InvalidOperationException("Invalid instance code prefix (letters/digits only, 1–32 characters).");

                instanceCodes = await GenerateInstanceCodesForPrefixAsync(prefix, qty);
                foreach (var code in instanceCodes)
                {
                    if (await _context.AssetInstances.AnyAsync(i => i.InstanceCode == code))
                        throw new InvalidOperationException($"Instance code {code} already exists.");
                }
            }
            else
            {
                if (qty > 1)
                    throw new InvalidOperationException("Instance code prefix is required when quantity is greater than 1.");

                if (await _context.AssetInstances.AnyAsync(i => i.InstanceCode == init.InstanceCode))
                    throw new InvalidOperationException("Instance code already exists.");

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

                // Tạo record AssetCapitalization nếu là tài sản cố định.
                var shouldCreateCapitalization = dto.IsFixedAsset || init.IsFixedAsset;
                if (shouldCreateCapitalization)
                {
                    _context.AssetCapitalizations.Add(new AssetCapitalization
                    {
                        AssetInstanceId = instance.AssetInstanceId,
                        CapitalizedDate = DateTime.UtcNow,
                        CapitalizedBy = dto.CreatedBy,
                        Note = "Tài sản cố định được tạo tự động",
                        CreateDate = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                try
                {
                    await _maintenanceTemplates.EnsureSchedulesForNewInstanceAsync(instance.AssetInstanceId, actorUserId);
                }
                catch
                {
                    // Không hủy tạo cá thể nếu đồng bộ quy định bảo dưỡng lỗi.
                }

                await ApplyCreateInstanceAssignmentAsync(instance.AssetInstanceId, init);
                if (InstanceCreateHasAssignment(init))
                    await _context.SaveChangesAsync();
            }
        }

        var created = await BuildAssetDetailAsync(asset.AssetId);
        if (created == null)
            throw new KeyNotFoundException();
        return created;
    }

    public async Task<AssetDocumentDTO> AddDocumentAsync(int userId, int assetId, AddAssetDocumentDTO dto)
    {
        if (!await _context.Assets.AnyAsync(a => a.AssetId == assetId))
            throw new KeyNotFoundException();

        var url = dto.FileUrl?.Trim();
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException("FileUrl is required.");
        if (url.Length > 2000)
            throw new InvalidOperationException("File URL is too long.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("FileUrl must be an absolute http or https URL.");

        if (!await _context.Users.AnyAsync(u => u.UserId == userId))
            throw new InvalidOperationException("User not found.");

        var entity = new Document
        {
            AssetId = assetId,
            FileUrl = url,
            DocumentType = dto.DocumentType,
            UploadedBy = userId,
            UploadedDate = DateTime.UtcNow,
            ProcurementId = null
        };
        _context.Documents.Add(entity);
        await _context.SaveChangesAsync();

        return new AssetDocumentDTO
        {
            DocumentId = entity.DocumentId,
            DocumentType = entity.DocumentType,
            FileUrl = entity.FileUrl,
            UploadedDate = entity.UploadedDate
        };
    }

    public async Task RemoveDocumentAsync(int assetId, int documentId)
    {
        var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId);
        if (doc == null || doc.AssetId != assetId)
            throw new KeyNotFoundException();

        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();
    }

    public async Task<AssetDetailResponseDTO> UpdateAsync(int id, UpdateAssetDTO dto)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            throw new KeyNotFoundException();

        if (dto.Code != null) asset.Code = dto.Code;
        if (dto.Name != null) asset.Name = dto.Name;
        if (dto.AssetTypeId.HasValue)
        {
            if (!await _context.AssetTypes.AnyAsync(t => t.AssetTypeId == dto.AssetTypeId.Value))
                throw new InvalidOperationException($"AssetTypeId {dto.AssetTypeId.Value} does not exist.");
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
            throw new KeyNotFoundException();
        return updated;
    }

    public async Task<AssetDetailResponseDTO> ChangeStatusAsync(int id, ChangeAssetStatusDTO dto)
    {
        if (!Enum.IsDefined(typeof(AssetStatus), dto.Status))
            throw new InvalidOperationException("Invalid asset status value.");

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            throw new KeyNotFoundException();

        asset.Status = (int)dto.Status;
        await _context.SaveChangesAsync();

        var detail = await BuildAssetDetailAsync(id);
        if (detail == null)
            throw new KeyNotFoundException();
        return detail;
    }

    public async Task<AssetResponseDTO> DeleteAsync(int id, AssetStatus? status, DeleteAssetDTO? dto)
    {
        var effectiveStatus = status ?? dto?.Status;
        if (!effectiveStatus.HasValue)
            throw new InvalidOperationException("Delete must provide status = Disposed, Lost, or Liquidated (query or body).");

        if (effectiveStatus.Value != AssetStatus.Disposed &&
            effectiveStatus.Value != AssetStatus.Lost &&
            effectiveStatus.Value != AssetStatus.Liquidated)
            throw new InvalidOperationException("Delete must set status to Disposed, Lost, or Liquidated.");

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            throw new KeyNotFoundException();

        asset.Status = (int)effectiveStatus.Value;
        await _context.SaveChangesAsync();

        await _context.Entry(asset).Reference(a => a.AssetType).LoadAsync();
        return ToAssetResponseDTO(asset);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<AssetDetailResponseDTO?> BuildAssetDetailAsync(int id, AssetDepartmentScope scope = default)
    {
        var asset = await _context.Assets
            .Include(a => a.AssetType)
            .Include(a => a.MaintenanceSchedules).ThenInclude(s => s.Template)
            .Include(a => a.AssetInstances).ThenInclude(i => i.MaintenanceSchedules).ThenInclude(s => s.Template)
            .Include(a => a.AssetInstances).ThenInclude(i => i.Warehouse)
            .Include(a => a.AssetInstances).ThenInclude(i => i.AssetLocations).ThenInclude(al => al.Department)
            .Include(a => a.AssetInstances).ThenInclude(i => i.AssetUsages).ThenInclude(u => u.Employee)
            .Include(a => a.AssetInstances).ThenInclude(i => i.AssetCapitalizations)
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
        var usageHistoriesByInstance = await BuildUsageHistoriesByInstanceAsync(instanceList);

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

        var allInstances = asset.AssetInstances.OrderBy(i => i.InstanceCode).ToList();
        var fromAssetNav = asset.MaintenanceSchedules
            .Where(s => s.IsActive)
            .Select(ToMaintenanceScheduleDto);
        var fromAllInstances = allInstances.SelectMany(inst =>
            inst.MaintenanceSchedules.Where(s => s.IsActive).Select(s =>
            {
                var schedDto = ToMaintenanceScheduleDto(s);
                schedDto.AssetInstanceId ??= inst.AssetInstanceId;
                if (string.IsNullOrWhiteSpace(schedDto.InstanceCode))
                    schedDto.InstanceCode = inst.InstanceCode;
                return schedDto;
            }));
        var schedules = fromAssetNav
            .Concat(fromAllInstances)
            .GroupBy(s => s.ScheduleId)
            .Select(g => g.First())
            .OrderBy(s => s.ScheduleId)
            .ToList();

        var baseDto = ToAssetResponseDTO(asset, null);

        return new AssetDetailResponseDTO
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
                    latestDepsByInstance.GetValueOrDefault(i.AssetInstanceId),
                    usageHistoriesByInstance.GetValueOrDefault(i.AssetInstanceId)))
                .ToList()
        };
    }

    private async Task TryAddAssetDocumentsAsync(int assetId, List<CreateAssetDocumentDTO> documents, int uploadedBy)
    {
        if (uploadedBy <= 0 || !await _context.Users.AnyAsync(u => u.UserId == uploadedBy))
            throw new InvalidOperationException("CreatedBy must be a valid user when attaching documents.");

        var toAdd = new List<Document>();
        foreach (var doc in documents)
        {
            var url = doc.FileUrl?.Trim();
            if (string.IsNullOrEmpty(url))
                continue;
            if (url.Length > 2000)
                throw new InvalidOperationException("A document URL exceeds the maximum length.");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException("Each document must use an http or https URL.");

            toAdd.Add(new Document
            {
                AssetId = assetId,
                FileUrl = url,
                DocumentType = doc.DocumentType,
                UploadedBy = uploadedBy,
                UploadedDate = DateTime.UtcNow,
                ProcurementId = null
            });
        }

        foreach (var row in toAdd)
            _context.Documents.Add(row);

        if (toAdd.Count > 0)
            await _context.SaveChangesAsync();
    }

    private async Task<Dictionary<int, DepreciationRecord>> LoadLatestDepreciationByInstanceAsync(List<int> instanceIds)
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

    private static bool CanViewDepartmentAssetsForAnyDepartment(ClaimsPrincipal user) =>
        user.IsInRole("ACCOUNTANT") || user.IsInRole("DIRECTOR");

    private static bool InstanceCreateHasAssignment(CreateAssetInstanceDTO dto) =>
        dto.AssignedDepartmentId.HasValue || dto.ResponsibleEmployeeId.HasValue;

    private async Task ValidateCreateInstanceAssignmentAsync(CreateAssetInstanceDTO dto)
    {
        Employee? emp = null;
        if (dto.ResponsibleEmployeeId.HasValue)
        {
            emp = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == dto.ResponsibleEmployeeId.Value);
            if (emp == null)
                throw new InvalidOperationException($"Employee {dto.ResponsibleEmployeeId.Value} not found.");
        }

        int? deptId = dto.AssignedDepartmentId;
        if (deptId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == deptId.Value))
                throw new InvalidOperationException($"Department {deptId.Value} does not exist.");
        }

        if (emp != null && deptId.HasValue && deptId.Value != emp.DepartmentId)
            throw new InvalidOperationException("Assigned department must match the responsible employee's department.");
    }

    private async Task ApplyCreateInstanceAssignmentAsync(int assetInstanceId, CreateAssetInstanceDTO dto)
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

    private async Task CloseCurrentLocationForInstanceAsync(int assetInstanceId, int? excludeLocationId, DateOnly newStartDate)
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

    private static (decimal[] Originals, decimal[] Currents) SplitValueAcrossInstances(decimal originalPrice, decimal currentValue, int qty)
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
        List<AssetUsageHistoryDTO>? usageHistories,
        AssetStatus? forcedStatus = null)
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
            IsFixedAsset = i.AssetCapitalizations != null && i.AssetCapitalizations.Any(),
            DepreciationPolicyId = i.DepreciationPolicyId,
            UsageHistories = usageHistories
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

    private static string? BuildAssetNote(string? originalNote, bool isFixedAsset)
    {
        var cleaned = StripFixedAssetMarker(originalNote);
        if (!isFixedAsset)
            return cleaned;

        return string.IsNullOrWhiteSpace(cleaned)
            ? FixedAssetMarker
            : $"{FixedAssetMarker} {cleaned}";
    }

    private static string? StripFixedAssetMarker(string? note)
    {
        var text = note?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (!text.StartsWith(FixedAssetMarker, StringComparison.Ordinal))
            return text;

        var suffix = text[FixedAssetMarker.Length..].Trim();
        return string.IsNullOrWhiteSpace(suffix) ? null : suffix;
    }

    private async Task<Dictionary<int, List<AssetUsageHistoryDTO>>> BuildUsageHistoriesByInstanceAsync(List<AssetInstance> instances)
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
                    var reportNumber = transferRequestId > 0
                        ? $"YC-{transferRequestId}"
                        : $"VT-{location.LocationId}";
                    var operation = transferRequestId > 0 ? "Điều chuyển" : "Cập nhật vị trí";
                    var place = string.IsNullOrWhiteSpace(location.Note)
                        ? location.Department?.Name
                        : $"{location.Department?.Name} · {location.Note}";

                    return new AssetUsageHistoryDTO
                    {
                        AssetInstanceId = instance.AssetInstanceId,
                        InstanceCode = instance.InstanceCode,
                        ExecutionDate = location.StartDate,
                        ReportNumber = reportNumber,
                        Operation = operation,
                        Condition = instance.Condition,
                        Location = place,
                        Value = instance.CurrentValue
                    };
                });

            var lifecycleRows = lifecyclesByInstance.GetValueOrDefault(instance.AssetInstanceId, [])
                .Select(lc => new AssetUsageHistoryDTO
                {
                    AssetInstanceId = instance.AssetInstanceId,
                    InstanceCode = instance.InstanceCode,
                    ExecutionDate = DateOnly.FromDateTime(lc.OccurredAt.ToLocalTime()),
                    ReportNumber = $"LC-{lc.AuditId}",
                    Operation = GetLifecycleOperationLabel(lc.ActionType),
                    Condition = lc.Description,
                    Location = null,
                    Value = null
                });

            result[instance.AssetInstanceId] = locationRows
                .Concat(lifecycleRows)
                .OrderByDescending(r => r.ExecutionDate)
                .ThenByDescending(r => r.ReportNumber)
                .ToList();
        }

        return result;
    }

    private static MaintenanceScheduleDTO ToMaintenanceScheduleDto(MaintenanceSchedule s)
    {
        int? intervalMonths = s.IntervalUnit == (int)MaintenanceRepeatIntervalUnit.Month
            ? s.IntervalValue
            : null;

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
            IntervalHours = null,
            IntervalValue = s.IntervalValue,
            IntervalUnit = s.IntervalUnit,
            StartDate = s.StartDate,
            NextDueDate = s.NextDueDate,
            EndDate = s.EndDate,
            IsActive = s.IsActive
        };
    }
}
