using System.Security.Claims;
using g19_sep490_ealds.Server.DTOs.Profile;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly EaldsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly int _departmentHeadRoleId;

    public ProfileController(EaldsDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _departmentHeadRoleId = configuration.GetValue<int>("App:DepartmentHeadRoleId", 4);
    }

    private int AccountantRoleId =>
        _configuration.GetValue("App:AccountantRoleId", 3);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "Không tìm thấy người dùng." });

        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        var roleRows = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => new { ur.RoleId, Code = ur.Role != null ? ur.Role.Code : null })
            .ToListAsync();
        var acctId = AccountantRoleId;
        var codes = roleRows
            .Select(r => RoleCanonicalization.CanonicalizeRoleCode(r.RoleId, r.Code, acctId))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        // Nếu có nhiều role, ưu tiên DIRECTOR để màn hình / API giám đốc khớp JWT (tránh First() ngẫu nhiên).
        var profileRole = codes.Any(c => string.Equals(c, "DIRECTOR", StringComparison.OrdinalIgnoreCase))
            ? "DIRECTOR"
            : codes.FirstOrDefault() ?? string.Empty;

        var isDepartmentHead = await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == _departmentHeadRoleId);

        var profile = new UserProfileDto
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

        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null)
            return NotFound(new { message = "Không tìm thấy thông tin nhân viên." });

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
        var roleRowsUpd = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => new { ur.RoleId, Code = ur.Role != null ? ur.Role.Code : null })
            .ToListAsync();
        var acctIdUpd = AccountantRoleId;
        var codesList = roleRowsUpd
            .Select(r => RoleCanonicalization.CanonicalizeRoleCode(r.RoleId, r.Code, acctIdUpd))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var roleOut = codesList.Any(c => string.Equals(c, "DIRECTOR", StringComparison.OrdinalIgnoreCase))
            ? "DIRECTOR"
            : codesList.FirstOrDefault() ?? string.Empty;

        var isDepartmentHeadUpd = await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == _departmentHeadRoleId);

        var updatedProfile = new UserProfileDto
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
            Role = roleOut,
            IsDepartmentHead = isDepartmentHeadUpd
        };

        return Ok(updatedProfile);
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized();

        if (user.Password != request.CurrentPassword)
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

        user.Password = request.NewPassword;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đổi mật khẩu thành công." });
    }
}
