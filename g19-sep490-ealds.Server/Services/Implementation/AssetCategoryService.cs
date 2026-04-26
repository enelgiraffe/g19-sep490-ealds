using g19_sep490_ealds.Server.DTOs.AssetCategory;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetCategoryService : IAssetCategoryService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<AssetCategoryService> _logger;

    public AssetCategoryService(EaldsDbContext context, ILogger<AssetCategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<AssetCategoryResponseDto>> GetAllAsync(string? keyword)
    {
        var query = _context.AssetCategories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(kw));
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new AssetCategoryResponseDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                AssetTypeCount = c.AssetTypes.Count
            })
            .ToListAsync();
    }

    public async Task<AssetCategoryDetailDto> GetByIdAsync(int id)
    {
        var category = await _context.AssetCategories
            .AsNoTracking()
            .Where(c => c.CategoryId == id)
            .Select(c => new AssetCategoryDetailDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                AssetTypeCount = c.AssetTypes.Count,
                AssetTypes = c.AssetTypes
                    .OrderBy(t => t.Name)
                    .Select(t => new AssetTypeInCategoryDto
                    {
                        AssetTypeId = t.AssetTypeId,
                        Name = t.Name,
                        AssetCount = t.Assets.Count
                    })
            })
            .FirstOrDefaultAsync();

        if (category == null)
            throw new KeyNotFoundException($"Asset category with id {id} not found.");

        return category;
    }

    public async Task<AssetCategoryResponseDto> CreateAsync(CreateAssetCategoryDto dto)
    {
        var nameExists = await _context.AssetCategories
            .AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower());

        if (nameExists)
            throw new InvalidOperationException($"Category name '{dto.Name}' already exists.");

        var category = new AssetCategory { Name = dto.Name.Trim() };
        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        return new AssetCategoryResponseDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            AssetTypeCount = 0
        };
    }

    public async Task<AssetCategoryResponseDto> UpdateAsync(int id, UpdateAssetCategoryDto dto)
    {
        var category = await _context.AssetCategories.FindAsync(id);
        if (category == null)
            throw new KeyNotFoundException($"Asset category with id {id} not found.");

        var nameExists = await _context.AssetCategories
            .AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower() && c.CategoryId != id);

        if (nameExists)
            throw new InvalidOperationException($"Category name '{dto.Name}' already exists.");

        category.Name = dto.Name.Trim();
        await _context.SaveChangesAsync();

        var assetTypeCount = await _context.AssetTypes.CountAsync(t => t.CategoryId == id);

        return new AssetCategoryResponseDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            AssetTypeCount = assetTypeCount
        };
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.AssetCategories
            .Include(c => c.AssetTypes)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            throw new KeyNotFoundException($"Asset category with id {id} not found.");

        if (category.AssetTypes.Count > 0)
            throw new InvalidOperationException(
                $"Cannot delete category '{category.Name}' because it has {category.AssetTypes.Count} asset type(s) linked to it. Remove or reassign them first.");

        _context.AssetCategories.Remove(category);
        await _context.SaveChangesAsync();
    }
}
