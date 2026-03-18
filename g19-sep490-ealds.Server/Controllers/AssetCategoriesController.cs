using g19_sep490_ealds.Server.DTOs.AssetCategory;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetCategoriesController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public AssetCategoriesController(EaldsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/assetcategories - List all categories with optional keyword search
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetCategoryResponseDto>>> GetAll(
        [FromQuery] string? keyword)
    {
        var query = _context.AssetCategories
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(kw));
        }

        var categories = await query
            .OrderBy(c => c.Name)
            .Select(c => new AssetCategoryResponseDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                AssetTypeCount = c.AssetTypes.Count
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// GET /api/assetcategories/{id} - Get a single category with its asset types
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetCategoryDetailDto>> GetById(int id)
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
            return NotFound(new { message = $"Asset category with id {id} not found." });

        return Ok(category);
    }

    /// <summary>
    /// POST /api/assetcategories - Create a new asset category
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetCategoryResponseDto>> Create([FromBody] CreateAssetCategoryDto dto)
    {
        var nameExists = await _context.AssetCategories
            .AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower());

        if (nameExists)
            return Conflict(new { message = $"Category name '{dto.Name}' already exists." });

        var category = new AssetCategory
        {
            Name = dto.Name.Trim()
        };

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.CategoryId },
            new AssetCategoryResponseDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                AssetTypeCount = 0
            });
    }

    /// <summary>
    /// PUT /api/assetcategories/{id} - Update asset category name
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetCategoryResponseDto>> Update(int id, [FromBody] UpdateAssetCategoryDto dto)
    {
        var category = await _context.AssetCategories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = $"Asset category with id {id} not found." });

        var nameExists = await _context.AssetCategories
            .AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower() && c.CategoryId != id);

        if (nameExists)
            return Conflict(new { message = $"Category name '{dto.Name}' already exists." });

        category.Name = dto.Name.Trim();
        await _context.SaveChangesAsync();

        var assetTypeCount = await _context.AssetTypes
            .CountAsync(t => t.CategoryId == id);

        return Ok(new AssetCategoryResponseDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            AssetTypeCount = assetTypeCount
        });
    }

    /// <summary>
    /// DELETE /api/assetcategories/{id} - Delete a category (only if it has no asset types)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.AssetCategories
            .Include(c => c.AssetTypes)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = $"Asset category with id {id} not found." });

        if (category.AssetTypes.Count > 0)
            return Conflict(new
            {
                message = $"Cannot delete category '{category.Name}' because it has {category.AssetTypes.Count} asset type(s) linked to it. Remove or reassign them first."
            });

        _context.AssetCategories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
