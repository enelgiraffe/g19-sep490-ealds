using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetTypesController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public AssetTypesController(EaldsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/assettypes - Danh sách loại tài sản cho drop-down filter, create form, v.v.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetTypeResponseDTO>>> GetAll()
    {
        var items = await _context.AssetTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new AssetTypeResponseDTO
            {
                AssetTypeId = t.AssetTypeId,
                Name = t.Name
            })
            .ToListAsync();

        return Ok(items);
    }
}

