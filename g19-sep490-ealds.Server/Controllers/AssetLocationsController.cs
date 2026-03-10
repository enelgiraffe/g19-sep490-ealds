using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetLocationsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public AssetLocationsController(EaldsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/AssetLocations - List departments for transfer dropdowns.
    /// Frontend uses it as "location" options (locationId + displayName).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var list = await _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.DepartmentId)
            .Select(d => new
            {
                locationId = d.DepartmentId,
                displayName = d.Name
            })
            .ToListAsync();
        return Ok(list);
    }
}
