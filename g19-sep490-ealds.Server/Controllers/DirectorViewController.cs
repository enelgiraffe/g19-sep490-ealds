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

    /// <summary>
    /// Danh sách tổng hợp yêu cầu cho màn giám đốc.
    /// Có thể lọc theo trạng thái, loại yêu cầu, người tạo và phân trang.
    /// </summary>
    [HttpGet("view")]
    public async Task<IActionResult> Get(
        [FromQuery] int? status,
        [FromQuery] int? requestTypeId,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 50;

        var query = _db.AssetRequests
            .AsNoTracking()
            .Include(x => x.Asset)
                .ThenInclude(a => a.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(x => x.User)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (requestTypeId.HasValue)
        {
            query = query.Where(x => x.RequestTypeId == requestTypeId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreateDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ar => new
            {
                // Trường cũ – để frontend hiện tại vẫn dùng được
                ar.AssetRequestId,
                ar.Title,
                ar.Status,
                ar.RequestTypeId,
                ar.UserId,
                ar.CreateDate,

                // Mở rộng thêm thông tin phục vụ các tab bảo dưỡng / sửa chữa / thanh lý…
                ar.Description,
                ar.ProposedData,
                AssetId = ar.AssetId,
                AssetCode = ar.Asset != null ? ar.Asset.Code : null,
                AssetName = ar.Asset != null ? ar.Asset.Name : null,
                AssetQuantity = ar.Asset != null ? (int?)ar.Asset.Quantity : null,
                CurrentDepartmentName = ar.Asset != null
                    ? ar.Asset.AssetLocations
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Department != null ? al.Department.Name : null)
                        .FirstOrDefault()
                    : null,
                CreatorEmail = ar.User != null ? ar.User.Email : null
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }
}
