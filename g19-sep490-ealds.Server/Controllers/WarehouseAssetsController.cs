using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseAssetsController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public WarehouseAssetsController(EaldsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var items = await _context.WarehouseAssets
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new
            {
                warehouseId = w.WarehouseId,
                name = w.Name,
                description = w.Description
            })
            .ToListAsync();

        return Ok(items);
    }
}

