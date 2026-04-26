using g19_sep490_ealds.Server.DTOs.AssetTypes;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetTypeService : IAssetTypeService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<AssetTypeService> _logger;

    public AssetTypeService(EaldsDbContext context, ILogger<AssetTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<AssetTypeResponseDto>> GetAllAsync(int? categoryId, string? keyword)
    {
        var query = _context.AssetTypes.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(kw));
        }

        return await query
            .OrderBy(t => t.Category.Name)
            .ThenBy(t => t.Name)
            .Select(t => new AssetTypeResponseDto
            {
                AssetTypeId = t.AssetTypeId,
                Name = t.Name,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                AssetCount = t.Assets.Count
            })
            .ToListAsync();
    }

    public async Task<AssetTypeDetailDto> GetByIdAsync(int id)
    {
        var assetType = await _context.AssetTypes
            .AsNoTracking()
            .Where(t => t.AssetTypeId == id)
            .Select(t => new AssetTypeDetailDto
            {
                AssetTypeId = t.AssetTypeId,
                Name = t.Name,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                AssetCount = t.Assets.Count,
                InventorySessionCount = t.InventorySessions.Count,
                MaintenanceTemplateCount = t.MaintenanceTemplates.Count
            })
            .FirstOrDefaultAsync();

        if (assetType == null)
            throw new KeyNotFoundException($"Asset type with id {id} not found.");

        return assetType;
    }

    public async Task<AssetTypeResponseDto> CreateAsync(CreateAssetTypeDto dto)
    {
        var categoryExists = await _context.AssetCategories.AnyAsync(c => c.CategoryId == dto.CategoryId);
        if (!categoryExists)
            throw new KeyNotFoundException($"Asset category with id {dto.CategoryId} not found.");

        var nameExists = await _context.AssetTypes.AnyAsync(t =>
            t.CategoryId == dto.CategoryId &&
            t.Name.ToLower() == dto.Name.Trim().ToLower());
        if (nameExists)
            throw new InvalidOperationException($"Asset type '{dto.Name}' already exists in this category.");

        var assetType = new AssetType
        {
            CategoryId = dto.CategoryId,
            Name = dto.Name.Trim()
        };

        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        var categoryName = await _context.AssetCategories
            .AsNoTracking()
            .Where(c => c.CategoryId == assetType.CategoryId)
            .Select(c => c.Name)
            .FirstAsync();

        return new AssetTypeResponseDto
        {
            AssetTypeId = assetType.AssetTypeId,
            Name = assetType.Name,
            CategoryId = assetType.CategoryId,
            CategoryName = categoryName,
            AssetCount = 0
        };
    }

    public async Task<AssetTypeResponseDto> UpdateAsync(int id, UpdateAssetTypeDto dto)
    {
        var assetType = await _context.AssetTypes.FindAsync(id);
        if (assetType == null)
            throw new KeyNotFoundException($"Asset type with id {id} not found.");

        var categoryExists = await _context.AssetCategories.AnyAsync(c => c.CategoryId == dto.CategoryId);
        if (!categoryExists)
            throw new KeyNotFoundException($"Asset category with id {dto.CategoryId} not found.");

        var nameExists = await _context.AssetTypes.AnyAsync(t =>
            t.CategoryId == dto.CategoryId &&
            t.Name.ToLower() == dto.Name.Trim().ToLower() &&
            t.AssetTypeId != id);
        if (nameExists)
            throw new InvalidOperationException($"Asset type '{dto.Name}' already exists in this category.");

        assetType.CategoryId = dto.CategoryId;
        assetType.Name = dto.Name.Trim();
        await _context.SaveChangesAsync();

        var categoryName = await _context.AssetCategories
            .AsNoTracking()
            .Where(c => c.CategoryId == assetType.CategoryId)
            .Select(c => c.Name)
            .FirstAsync();

        var assetCount = await _context.Assets.CountAsync(a => a.AssetTypeId == id);

        return new AssetTypeResponseDto
        {
            AssetTypeId = assetType.AssetTypeId,
            Name = assetType.Name,
            CategoryId = assetType.CategoryId,
            CategoryName = categoryName,
            AssetCount = assetCount
        };
    }

    public async Task DeleteAsync(int id)
    {
        var assetType = await _context.AssetTypes
            .Include(t => t.Assets)
            .Include(t => t.InventorySessions)
            .Include(t => t.MaintenanceTemplates)
            .FirstOrDefaultAsync(t => t.AssetTypeId == id);

        if (assetType == null)
            throw new KeyNotFoundException($"Asset type with id {id} not found.");

        var issues = new List<string>();
        if (assetType.Assets.Count > 0)
            issues.Add($"{assetType.Assets.Count} asset(s)");
        if (assetType.InventorySessions.Count > 0)
            issues.Add($"{assetType.InventorySessions.Count} inventory session(s)");
        if (assetType.MaintenanceTemplates.Count > 0)
            issues.Add($"{assetType.MaintenanceTemplates.Count} maintenance template(s)");

        if (issues.Count > 0)
            throw new InvalidOperationException(
                $"Cannot delete asset type '{assetType.Name}' because it is linked to {string.Join(", ", issues)}. Remove or reassign them first.");

        _context.AssetTypes.Remove(assetType);
        await _context.SaveChangesAsync();
    }
}
