using g19_sep490_ealds.Server.DTOs.Warehouse;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehouseAssetsController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public WarehouseAssetsController(EaldsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists warehouses for dropdowns. Uses <see cref="Warehouse"/>
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var items = await _context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new
            {
                warehouseId = w.WarehouseId,
                name = w.Name,
                description = w.Location,
                canDelete = !w.AssetInstances.Any(),
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateWarehouseDto dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest(new { message = "Tên kho là bắt buộc." });

        var loc = dto.Location?.Trim();
        var duplicate = await _context.Warehouses
            .AsNoTracking()
            .AnyAsync(w => w.Name.ToLower() == name.ToLower());
        if (duplicate)
            return BadRequest(new { message = "Đã tồn tại kho có tên này." });

        var entity = new Warehouse
        {
            Name = name,
            Location = string.IsNullOrEmpty(loc) ? null : loc
        };
        _context.Warehouses.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            warehouseId = entity.WarehouseId,
            name = entity.Name,
            description = entity.Location
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> Update(int id, [FromBody] CreateWarehouseDto dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest(new { message = "Tên kho là bắt buộc." });

        var entity = await _context.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == id);
        if (entity == null)
            return NotFound(new { message = "Không tìm thấy kho." });

        var loc = dto.Location?.Trim();
        var duplicate = await _context.Warehouses
            .AsNoTracking()
            .AnyAsync(w => w.WarehouseId != id && w.Name.ToLower() == name.ToLower());
        if (duplicate)
            return BadRequest(new { message = "Đã tồn tại kho có tên này." });

        entity.Name = name;
        entity.Location = string.IsNullOrEmpty(loc) ? null : loc;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            warehouseId = entity.WarehouseId,
            name = entity.Name,
            description = entity.Location
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == id);
        if (entity == null)
            return NotFound(new { message = "Không tìm thấy kho." });

        var hasInstances = await _context.AssetInstances.AsNoTracking().AnyAsync(a => a.WarehouseId == id);
        if (hasInstances)
            return BadRequest(new
            {
                message =
                    "Không thể xóa kho vì còn bản ghi tài sản (cá thể) gắn với kho này.",
            });

        _context.Warehouses.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

