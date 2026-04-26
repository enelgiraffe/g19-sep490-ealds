using g19_sep490_ealds.Server.DTOs.Profile;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class ProfileService : IProfileService
{
    private readonly EaldsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly int _departmentHeadRoleId;

    private int AccountantRoleId => _configuration.GetValue("App:AccountantRoleId", 3);

    public ProfileService(EaldsDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        var (profileRole, isDepartmentHead) = await GetRoleInfoAsync(userId);

        return new UserProfileDto
        {
            Id = user.UserId,
            Email = user.Email,
            Name = employee?.Name ?? user.Email,
            EmployeeCode = employee?.Code,
            Phone = employee?.Phone,
            Address = employee?.Address,
            Dob = employee?.Dob,
            Gender = employee?.Gender,
            ImageUrl = employee?.ImageUrl,
            DepartmentName = employee?.Department?.Name,
            DepartmentId = employee?.DepartmentId,
            Role = profileRole,
            IsDepartmentHead = isDepartmentHead
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy thông tin nhân viên.");

        employee.Name = request.Name;
        employee.Phone = request.Phone;
        employee.Address = request.Address;
        employee.Dob = request.Dob;
        employee.Gender = request.Gender;
        employee.ImageUrl = request.ImageUrl;
        employee.UpdateDate = DateTime.UtcNow;
        employee.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        var (profileRole, isDepartmentHead) = await GetRoleInfoAsync(userId);

        return new UserProfileDto
        {
            Id = userId,
            Email = user!.Email,
            Name = employee.Name,
            EmployeeCode = employee.Code,
            Phone = employee.Phone,
            Address = employee.Address,
            Dob = employee.Dob,
            Gender = employee.Gender,
            ImageUrl = employee.ImageUrl,
            DepartmentName = employee.Department?.Name,
            DepartmentId = employee.DepartmentId,
            Role = profileRole,
            IsDepartmentHead = isDepartmentHead
        };
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (user.Password != request.CurrentPassword)
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");

        if (string.Equals(user.Password, request.NewPassword, StringComparison.Ordinal))
            throw new InvalidOperationException("Mật khẩu mới không được trùng với mật khẩu cũ.");

        user.Password = request.NewPassword;
        await _context.SaveChangesAsync();
    }

    private async Task<(string role, bool isDepartmentHead)> GetRoleInfoAsync(int userId)
    {
        var acctId = AccountantRoleId;
        var roleRows = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => new { ur.RoleId, Code = ur.Role != null ? ur.Role.Code : null })
            .ToListAsync();
        var codes = roleRows
            .Select(r => RoleCanonicalization.CanonicalizeRoleCode(r.RoleId, r.Code, acctId))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Prioritize DIRECTOR so the profile role matches the JWT claim when user holds multiple roles.
        var profileRole = codes.Any(c => string.Equals(c, "DIRECTOR", StringComparison.OrdinalIgnoreCase))
            ? "DIRECTOR"
            : codes.FirstOrDefault() ?? string.Empty;

        var isDepartmentHead = await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == _departmentHeadRoleId);

        return (profileRole, isDepartmentHead);
    }
}
