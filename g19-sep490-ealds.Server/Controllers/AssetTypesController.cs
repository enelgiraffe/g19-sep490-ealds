using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetTypesController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public AssetTypesController(EaldsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/AssetTypes - List all asset types.
    /// Optional filters: categoryId, keyword (matches name).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetTypeResponseDto>>> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] string? keyword)
    {
        var query = _db.AssetTypes
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(kw));
        }

        var result = await query
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

        return Ok(result);
    }

    /// <summary>
    /// GET /api/AssetTypes/{id} - Get a single asset type with full detail.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetTypeDetailDto>> GetById(int id)
    {
        var assetType = await _db.AssetTypes
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
            return NotFound(new { message = $"Asset type with id {id} not found." });

        return Ok(assetType);
    }

    /// <summary>
    /// POST /api/AssetTypes - Create a new asset type.
    /// Name must be unique within the same category.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetTypeResponseDto>> Create([FromBody] CreateAssetTypeDto dto)
    {
        var categoryExists = await _db.AssetCategories.AnyAsync(c => c.CategoryId == dto.CategoryId);
        if (!categoryExists)
            return NotFound(new { message = $"Asset category with id {dto.CategoryId} not found." });

        var nameExists = await _db.AssetTypes.AnyAsync(t =>
            t.CategoryId == dto.CategoryId &&
            t.Name.ToLower() == dto.Name.Trim().ToLower());

        if (nameExists)
            return Conflict(new { message = $"Asset type '{dto.Name}' already exists in this category." });

        var assetType = new AssetType
        {
            CategoryId = dto.CategoryId,
            Name = dto.Name.Trim()
        };

        _db.AssetTypes.Add(assetType);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = assetType.AssetTypeId },
            new AssetTypeResponseDto
            {
                AssetTypeId = assetType.AssetTypeId,
                Name = assetType.Name,
                CategoryId = assetType.CategoryId,
                CategoryName = (await _db.AssetCategories
                    .AsNoTracking()
                    .Where(c => c.CategoryId == assetType.CategoryId)
                    .Select(c => c.Name)
                    .FirstAsync()),
                AssetCount = 0
            });
    }

    /// <summary>
    /// PUT /api/AssetTypes/{id} - Update an asset type's name and/or category.
    /// Name must remain unique within the target category.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetTypeResponseDto>> Update(int id, [FromBody] UpdateAssetTypeDto dto)
    {
        var assetType = await _db.AssetTypes.FindAsync(id);
        if (assetType == null)
            return NotFound(new { message = $"Asset type with id {id} not found." });

        var categoryExists = await _db.AssetCategories.AnyAsync(c => c.CategoryId == dto.CategoryId);
        if (!categoryExists)
            return NotFound(new { message = $"Asset category with id {dto.CategoryId} not found." });

        var nameExists = await _db.AssetTypes.AnyAsync(t =>
            t.CategoryId == dto.CategoryId &&
            t.Name.ToLower() == dto.Name.Trim().ToLower() &&
            t.AssetTypeId != id);

        if (nameExists)
            return Conflict(new { message = $"Asset type '{dto.Name}' already exists in this category." });

        assetType.CategoryId = dto.CategoryId;
        assetType.Name = dto.Name.Trim();
        await _db.SaveChangesAsync();

        var categoryName = await _db.AssetCategories
            .AsNoTracking()
            .Where(c => c.CategoryId == assetType.CategoryId)
            .Select(c => c.Name)
            .FirstAsync();

        var assetCount = await _db.Assets.CountAsync(a => a.AssetTypeId == id);

        return Ok(new AssetTypeResponseDto
        {
            AssetTypeId = assetType.AssetTypeId,
            Name = assetType.Name,
            CategoryId = assetType.CategoryId,
            CategoryName = categoryName,
            AssetCount = assetCount
        });
    }

    /// <summary>
    /// DELETE /api/AssetTypes/{id} - Delete an asset type.
    /// Blocked if it has linked Assets, InventorySessions, or MaintenanceTemplates.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var assetType = await _db.AssetTypes
            .Include(t => t.Assets)
            .Include(t => t.InventorySessions)
            .Include(t => t.MaintenanceTemplates)
            .FirstOrDefaultAsync(t => t.AssetTypeId == id);

        if (assetType == null)
            return NotFound(new { message = $"Asset type with id {id} not found." });

        var issues = new List<string>();
        if (assetType.Assets.Count > 0)
            issues.Add($"{assetType.Assets.Count} asset(s)");
        if (assetType.InventorySessions.Count > 0)
            issues.Add($"{assetType.InventorySessions.Count} inventory session(s)");
        if (assetType.MaintenanceTemplates.Count > 0)
            issues.Add($"{assetType.MaintenanceTemplates.Count} maintenance template(s)");

        if (issues.Count > 0)
            return Conflict(new
            {
                message = $"Cannot delete asset type '{assetType.Name}' because it is linked to {string.Join(", ", issues)}. Remove or reassign them first."
            });

        _db.AssetTypes.Remove(assetType);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
