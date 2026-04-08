using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public UsersController(EaldsDbContext context)
    {
        _context = context;
    }

    // GET: api/Users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.EmployeeUsers)
                .ThenInclude(e => e.Department)
            .ToListAsync();

        var roleRows = await _context.UserRoles
            .Join(
                _context.Roles,
                ur => ur.RoleId,
                r => r.RoleId,
                (ur, r) => new { ur.UserId, r.RoleId, r.Name }
            )
            .ToListAsync();

        var rolesByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    RoleIds = g.Select(x => x.RoleId).Distinct().ToList(),
                    RoleNames = g.Select(x => x.Name).Distinct().ToList()
                }
            );

        var data = users.Select(u =>
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

        return Ok(data);
    }

    // GET: api/Users/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.EmployeeUsers)
                .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        var roleRows = await _context.UserRoles
            .Where(ur => ur.UserId == id)
            .Join(
                _context.Roles,
                ur => ur.RoleId,
                r => r.RoleId,
                (ur, r) => new { r.RoleId, r.Name }
            )
            .ToListAsync();

        var userDTO = new UserDTO
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

        return Ok(userDTO);
    }

    // POST: api/Users
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest("A user with this email already exists.");
        }

        if (await _context.Employees.AnyAsync(e => e.Code == dto.EmployeeCode))
        {
            return BadRequest("An employee with this code already exists.");
        }

        var departmentActive = await _context.Departments.AnyAsync(d =>
            d.DepartmentId == dto.DepartmentId && d.Status == 1);
        if (!departmentActive)
        {
            return BadRequest("Department not found or inactive.");
        }

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

        var userDTO = new UserDTO
        {
            UserId = user.UserId,
            Email = user.Email,
            Status = user.Status,
            EmployeeCode = employee.Code,
            FullName = employee.Name,
            DepartmentId = employee.DepartmentId,
            DepartmentName = await _context.Departments
                .Where(d => d.DepartmentId == employee.DepartmentId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(),
            Phone = employee.Phone,
            ImageUrl = null,
            RoleIds = validRoleIds,
            Roles = await _context.Roles
                .Where(r => validRoleIds.Contains(r.RoleId))
                .Select(r => r.Name)
                .ToListAsync()
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDTO);
    }

    // PUT: api/Users/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users
            .Include(u => u.EmployeeUsers)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        var employee = user.EmployeeUsers.FirstOrDefault();
        if (employee == null)
        {
            return BadRequest("Employee profile not found for this user.");
        }

        var departmentOk = dto.DepartmentId == employee.DepartmentId
            ? await _context.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId)
            : await _context.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId && d.Status == 1);
        if (!departmentOk)
        {
            return BadRequest("Department not found or inactive.");
        }

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var duplicatedEmail = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id);
            if (duplicatedEmail)
            {
                return BadRequest("A user with this email already exists.");
            }
        }

        user.Email = dto.Email;
        user.Status = dto.Status;
        employee.Name = dto.FullName;
        employee.Phone = dto.Phone;
        employee.DepartmentId = dto.DepartmentId;
        employee.Status = dto.Status;
        employee.UpdateDate = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
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
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // PUT: api/Users/5/deactivate
    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Status = 0; // Assuming 0 implies deactivated

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Users/5/roles
    [HttpPost("{id:int}/roles")]
    public async Task<IActionResult> ManageUserRoles(int id, [FromBody] AssignRoleDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM UserRole WHERE UserId = {id}");

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

        return Ok(new { message = "Roles updated successfully" });
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetMetadata()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleOptionDTO
            {
                RoleId = r.RoleId,
                Name = r.Name
            })
            .ToListAsync();

        var departments = await _context.Departments
            .Where(d => d.Status == 1)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionDTO
            {
                DepartmentId = d.DepartmentId,
                Name = d.Name
            })
            .ToListAsync();

        return Ok(new UserMetadataDTO
        {
            Roles = roles,
            Departments = departments
        });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleOptionDTO
            {
                RoleId = r.RoleId,
                Name = r.Name
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var departments = await _context.Departments
            .Where(d => d.Status == 1)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionDTO
            {
                DepartmentId = d.DepartmentId,
                Name = d.Name
            })
            .ToListAsync();

        return Ok(departments);
    }

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> AdminChangePassword(int id, [FromBody] AdminChangePasswordDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null)
        {
            return NotFound();
        }

        user.Password = dto.NewPassword;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// User deletion is disabled; use status/deactivate to revoke access.
    /// </summary>
    [HttpDelete("{id:int}")]
    public IActionResult DeleteUser(int id)
    {
        _ = id;
        return StatusCode(StatusCodes.Status403Forbidden, "Không được phép xóa người dùng.");
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.UserId == id);
    }
}
