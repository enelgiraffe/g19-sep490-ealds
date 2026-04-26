using g19_sep490_ealds.Server.DTOs.Users;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class UsersService : IUsersService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<UsersService> _logger;

    public UsersService(EaldsDbContext context, ILogger<UsersService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDTO>> GetUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.EmployeeUsers)
                .ThenInclude(e => e.Department)
            .ToListAsync();

        var roleRows = await _context.UserRoles
            .Join(_context.Roles, ur => ur.RoleId, r => r.RoleId,
                (ur, r) => new { ur.UserId, r.RoleId, r.Name })
            .ToListAsync();

        var rolesByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    RoleIds = g.Select(x => x.RoleId).Distinct().ToList(),
                    RoleNames = g.Select(x => x.Name).Distinct().ToList()
                });

        return users.Select(u =>
        {
            var roleData = rolesByUser.ContainsKey(u.UserId)
                ? rolesByUser[u.UserId]
                : new { RoleIds = new List<int>(), RoleNames = new List<string>() };

            return new UserDTO
            {
                UserId = u.UserId,
                Email = u.Email,
                Status = u.Status,
                EmployeeCode = u.EmployeeUsers.Select(e => e.Code).FirstOrDefault(),
                FullName = u.EmployeeUsers.Select(e => e.Name).FirstOrDefault(),
                DepartmentId = u.EmployeeUsers.Select(e => (int?)e.DepartmentId).FirstOrDefault(),
                DepartmentName = u.EmployeeUsers.Select(e => e.Department.Name).FirstOrDefault(),
                Phone = u.EmployeeUsers.Select(e => e.Phone).FirstOrDefault(),
                ImageUrl = u.EmployeeUsers.Select(e => e.ImageUrl).FirstOrDefault(),
                RoleIds = roleData.RoleIds,
                Roles = roleData.RoleNames
            };
        }).ToList();
    }

    public async Task<UserDTO> GetUserAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.EmployeeUsers)
                .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy người dùng với Id = {id}.");

        var roleRows = await _context.UserRoles
            .Where(ur => ur.UserId == id)
            .Join(_context.Roles, ur => ur.RoleId, r => r.RoleId,
                (ur, r) => new { r.RoleId, r.Name })
            .ToListAsync();

        return new UserDTO
        {
            UserId = user.UserId,
            Email = user.Email,
            Status = user.Status,
            EmployeeCode = user.EmployeeUsers.Select(e => e.Code).FirstOrDefault(),
            FullName = user.EmployeeUsers.Select(e => e.Name).FirstOrDefault(),
            DepartmentId = user.EmployeeUsers.Select(e => (int?)e.DepartmentId).FirstOrDefault(),
            DepartmentName = user.EmployeeUsers.Select(e => e.Department.Name).FirstOrDefault(),
            Phone = user.EmployeeUsers.Select(e => e.Phone).FirstOrDefault(),
            ImageUrl = user.EmployeeUsers.Select(e => e.ImageUrl).FirstOrDefault(),
            RoleIds = roleRows.Select(x => x.RoleId).Distinct().ToList(),
            Roles = roleRows.Select(x => x.Name).Distinct().ToList()
        };
    }

    public async Task<UserDTO> CreateUserAsync(CreateUserDTO dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("A user with this email already exists.");

        if (await _context.Employees.AnyAsync(e => e.Code == dto.EmployeeCode))
            throw new InvalidOperationException("An employee with this code already exists.");

        var departmentActive = await _context.Departments.AnyAsync(d =>
            d.DepartmentId == dto.DepartmentId && d.Status == 1);
        if (!departmentActive)
            throw new InvalidOperationException("Department not found or inactive.");

        var user = new User
        {
            Email = dto.Email,
            Password = dto.Password,
            Status = dto.Status
        };

        var validRoleIds = new List<int>();
        if (dto.RoleIds != null && dto.RoleIds.Any())
        {
            validRoleIds = await _context.Roles
                .Where(r => dto.RoleIds.Contains(r.RoleId))
                .Select(r => r.RoleId)
                .ToListAsync();
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var employee = new Employee
        {
            UserId = user.UserId,
            DepartmentId = dto.DepartmentId,
            Name = dto.FullName,
            Code = dto.EmployeeCode,
            Phone = dto.Phone,
            Status = dto.Status,
            CreateDate = DateTime.UtcNow,
            CreatedBy = user.UserId,
            ImageUrl = null
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        foreach (var roleId in validRoleIds)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO UserRole (UserId, RoleId) VALUES ({user.UserId}, {roleId})");
        }

        var departmentName = await _context.Departments
            .Where(d => d.DepartmentId == employee.DepartmentId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync();

        var roleNames = await _context.Roles
            .Where(r => validRoleIds.Contains(r.RoleId))
            .Select(r => r.Name)
            .ToListAsync();

        return new UserDTO
        {
            UserId = user.UserId,
            Email = user.Email,
            Status = user.Status,
            EmployeeCode = employee.Code,
            FullName = employee.Name,
            DepartmentId = employee.DepartmentId,
            DepartmentName = departmentName,
            Phone = employee.Phone,
            ImageUrl = null,
            RoleIds = validRoleIds,
            Roles = roleNames
        };
    }

    public async Task UpdateUserAsync(int id, UpdateUserDTO dto)
    {
        var user = await _context.Users
            .Include(u => u.EmployeeUsers)
            .FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy người dùng với Id = {id}.");

        var employee = user.EmployeeUsers.FirstOrDefault()
            ?? throw new InvalidOperationException("Employee profile not found for this user.");

        var departmentOk = dto.DepartmentId == employee.DepartmentId
            ? await _context.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId)
            : await _context.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId && d.Status == 1);
        if (!departmentOk)
            throw new InvalidOperationException("Department not found or inactive.");

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không thể thay đổi email.");

        user.Status = dto.Status;
        employee.Name = dto.FullName;
        employee.Phone = dto.Phone;
        employee.DepartmentId = dto.DepartmentId;
        employee.Status = dto.Status;
        employee.UpdateDate = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Users.AnyAsync(e => e.UserId == id))
                throw new KeyNotFoundException($"Không tìm thấy người dùng với Id = {id}.");
            throw;
        }

        if (dto.RoleIds != null)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM UserRole WHERE UserId = {id}");

            var validRoleIds = await _context.Roles
                .Where(r => dto.RoleIds.Contains(r.RoleId))
                .Select(r => r.RoleId)
                .ToListAsync();

            foreach (var roleId in validRoleIds)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO UserRole (UserId, RoleId) VALUES ({id}, {roleId})");
            }
        }
    }

    public async Task DeactivateUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy người dùng với Id = {id}.");

        user.Status = 0;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Users.AnyAsync(e => e.UserId == id))
                throw new KeyNotFoundException($"Không tìm thấy người dùng với Id = {id}.");
            throw;
        }
    }

    public async Task ManageUserRolesAsync(int id, AssignRoleDTO dto)
    {
        _ = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy người dùng với Id = {id}.");

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM UserRole WHERE UserId = {id}");

        if (dto.RoleIds != null && dto.RoleIds.Any())
        {
            var validRoleIds = await _context.Roles
                .Where(r => dto.RoleIds.Contains(r.RoleId))
                .Select(r => r.RoleId)
                .ToListAsync();

            foreach (var roleId in validRoleIds)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO UserRole (UserId, RoleId) VALUES ({id}, {roleId})");
            }
        }
    }

    public async Task<UserMetadataDTO> GetMetadataAsync()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleOptionDTO { RoleId = r.RoleId, Name = r.Name })
            .ToListAsync();

        var departments = await _context.Departments
            .Where(d => d.Status == 1)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionDTO { DepartmentId = d.DepartmentId, Name = d.Name })
            .ToListAsync();

        return new UserMetadataDTO { Roles = roles, Departments = departments };
    }

    public async Task<IEnumerable<RoleOptionDTO>> GetRolesAsync()
    {
        return await _context.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleOptionDTO { RoleId = r.RoleId, Name = r.Name })
            .ToListAsync();
    }

    public async Task<IEnumerable<DepartmentOptionDTO>> GetDepartmentsAsync()
    {
        return await _context.Departments
            .Where(d => d.Status == 1)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionDTO { DepartmentId = d.DepartmentId, Name = d.Name })
            .ToListAsync();
    }

    public async Task AdminChangePasswordAsync(int id, AdminChangePasswordDTO dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy người dùng với Id = {id}.");

        if (string.Equals(user.Password, dto.NewPassword, StringComparison.Ordinal))
            throw new InvalidOperationException("Mật khẩu mới không được trùng với mật khẩu cũ.");

        user.Password = dto.NewPassword;
        await _context.SaveChangesAsync();
    }
}
