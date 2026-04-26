using g19_sep490_ealds.Server.DTOs.Departments;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class DepartmentService : IDepartmentService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(EaldsDbContext context, ILogger<DepartmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<DepartmentDTO>> GetAllAsync(string? keyword)
    {
        var query = _context.Departments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(d =>
                d.Code.ToLower().Contains(kw) ||
                d.Name.ToLower().Contains(kw));
        }

        return await query
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
    }

    public async Task<DepartmentDTO> GetByIdAsync(int id)
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
            throw new KeyNotFoundException($"Không tìm thấy phòng ban với id {id}.");

        return row;
    }

    public async Task<DepartmentDTO> CreateAsync(int userId, CreateDepartmentDTO dto)
    {
        var code = dto.Code.Trim();
        var name = dto.Name.Trim();

        if (await _context.Departments.AnyAsync(d => d.Code.ToLower() == code.ToLower()))
            throw new InvalidOperationException("Đã tồn tại phòng ban với mã này.");

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

        return new DepartmentDTO
        {
            DepartmentId = entity.DepartmentId,
            Code = entity.Code,
            Name = entity.Name,
            Status = entity.Status,
            CreateDate = entity.CreateDate,
            UpdateDate = entity.UpdateDate
        };
    }

    public async Task UpdateAsync(int userId, int id, UpdateDepartmentDTO dto)
    {
        var entity = await _context.Departments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy phòng ban với id {id}.");

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();

        if (await _context.Departments.AnyAsync(d =>
                d.DepartmentId != id && d.Code.ToLower() == code.ToLower()))
            throw new InvalidOperationException("Đã tồn tại phòng ban khác với mã này.");

        entity.Code = code;
        entity.Name = name;
        entity.Status = dto.Status;
        entity.UpdateDate = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        await _context.SaveChangesAsync();
    }

    public async Task<string?> DeleteAsync(int userId, int id)
    {
        var entity = await _context.Departments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy phòng ban với id {id}.");

        var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
        var hasLocations = await _context.AssetLocations.AnyAsync(a => a.DepartmentId == id);
        var hasSessions = await _context.InventorySessions.AnyAsync(s => s.DepartmentId == id);

        if (hasEmployees || hasLocations || hasSessions)
        {
            entity.Status = 0;
            entity.UpdateDate = DateTime.UtcNow;
            entity.UpdatedBy = userId;
            await _context.SaveChangesAsync();
            return "Phòng ban đang được sử dụng (nhân viên, vị trí tài sản hoặc phiên kiểm kê). Đã chuyển trạng thái sang không hoạt động thay vì xóa.";
        }

        _context.Departments.Remove(entity);
        await _context.SaveChangesAsync();
        return null;
    }
}
