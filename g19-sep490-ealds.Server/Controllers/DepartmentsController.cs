using System.Security.Claims;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,admin")]
public class DepartmentsController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public DepartmentsController(EaldsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDTO>>> GetDepartments([FromQuery] string? keyword)
    {
        var query = _context.Departments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(d =>
                d.Code.ToLower().Contains(kw) ||
                d.Name.ToLower().Contains(kw));
        }

        var list = await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDTO
            {
                DepartmentId = d.DepartmentId,
                Code = d.Code,
                Name = d.Name,
                Status = d.Status,
                CreateDate = d.CreateDate,
                UpdateDate = d.UpdateDate
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDTO>> GetDepartment(int id)
    {
        var row = await _context.Departments.AsNoTracking()
            .Where(d => d.DepartmentId == id)
            .Select(d => new DepartmentDTO
            {
                DepartmentId = d.DepartmentId,
                Code = d.Code,
                Name = d.Name,
                Status = d.Status,
                CreateDate = d.CreateDate,
                UpdateDate = d.UpdateDate
            })
            .FirstOrDefaultAsync();

        if (row == null)
            return NotFound();

        return Ok(row);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDTO>> CreateDepartment([FromBody] CreateDepartmentDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();

        if (await _context.Departments.AnyAsync(d => d.Code.ToLower() == code.ToLower()))
            return BadRequest("Đã tồn tại phòng ban với mã này.");

        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var entity = new Department
        {
            Code = code,
            Name = name,
            Status = dto.Status,
            CreateDate = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.Departments.Add(entity);
        await _context.SaveChangesAsync();

        var result = new DepartmentDTO
        {
            DepartmentId = entity.DepartmentId,
            Code = entity.Code,
            Name = entity.Name,
            Status = entity.Status,
            CreateDate = entity.CreateDate,
            UpdateDate = entity.UpdateDate
        };

        return CreatedAtAction(nameof(GetDepartment), new { id = entity.DepartmentId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = await _context.Departments.FindAsync(id);
        if (entity == null)
            return NotFound();

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();

        if (await _context.Departments.AnyAsync(d =>
                d.DepartmentId != id && d.Code.ToLower() == code.ToLower()))
            return BadRequest("Đã tồn tại phòng ban khác với mã này.");

        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        entity.Code = code;
        entity.Name = name;
        entity.Status = dto.Status;
        entity.UpdateDate = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var entity = await _context.Departments.FindAsync(id);
        if (entity == null)
            return NotFound();

        var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
        var hasLocations = await _context.AssetLocations.AnyAsync(a => a.DepartmentId == id);
        var hasSessions = await _context.InventorySessions.AnyAsync(s => s.DepartmentId == id);

        if (hasEmployees || hasLocations || hasSessions)
        {
            entity.Status = 0;
            if (TryGetCurrentUserId(out var userId))
            {
                entity.UpdateDate = DateTime.UtcNow;
                entity.UpdatedBy = userId;
            }
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message =
                    "Phòng ban đang được sử dụng (nhân viên, vị trí tài sản hoặc phiên kiểm kê). Đã chuyển trạng thái sang không hoạt động thay vì xóa."
            });
        }

        _context.Departments.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId) && userId > 0;
    }
}
