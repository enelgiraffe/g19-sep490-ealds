using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/director")]
public class DirectorViewController : ControllerBase
{
    private readonly EaldsDbContext _db;
    public DirectorViewController(EaldsDbContext db) => _db = db;

    [HttpGet("view")]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.AssetRequests.AsNoTracking();
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x=>x.CreateDate).Skip((page-1)*pageSize).Take(pageSize)
            .Select(ar => new { ar.AssetRequestId, ar.Title, ar.Status, ar.RequestTypeId, ar.UserId, ar.CreateDate })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total/(double)pageSize) });
    }
}
