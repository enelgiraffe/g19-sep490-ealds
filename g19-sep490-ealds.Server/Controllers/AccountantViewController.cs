using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/accountant")]
public class AccountantViewController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public AccountantViewController(EaldsDbContext db) => _db = db;

    [HttpGet("view")]
    public async Task<IActionResult> Get([FromQuery] string? requestTypeIds, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.AssetRequests
            .AsNoTracking()
            .Where(ar => ar.Status != -1) // accountant should not see drafts
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(requestTypeIds))
        {
            var ids = requestTypeIds.Split(',').Select(s => { int.TryParse(s.Trim(), out var v); return v; }).Where(v => v>0).ToArray();
            if (ids.Length>0) query = query.Where(ar => ids.Contains(ar.RequestTypeId));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x=>x.CreateDate).Skip((page-1)*pageSize).Take(pageSize)
            .Select(ar => new
            {
                ar.AssetRequestId,
                ar.Title,
                ar.Status,
                ar.RequestTypeId,
                ar.UserId,
                ar.CreateDate,
                ar.ProposedData,
                ar.AllocationTargetDepartmentId,
                TargetDepartmentName = _db.Departments
                    .Where(d => d.DepartmentId == ar.AllocationTargetDepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                AssetAllocationOrderId = _db.AssetAllocationOrders
                    .Where(o => o.AssetRequestId == ar.AssetRequestId)
                    .Select(o => (int?)o.AssetAllocationOrderId)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total/(double)pageSize) });
    }
}
