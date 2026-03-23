using System;
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
            .Include(u => u.Roles)
            .Select(u => new UserDTO
            {
                UserId = u.UserId,
                Email = u.Email,
                Status = u.Status,
                Roles = u.Roles.Select(r => r.Name).ToList()
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/Users/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        var userDTO = new UserDTO
        {
            UserId = user.UserId,
            Email = user.Email,
            Status = user.Status,
            Roles = user.Roles.Select(r => r.Name).ToList()
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

        var user = new User
        {
            Email = dto.Email,
            Password = dto.Password, // Caution: Storing in plain text as per current request/system state assumption. Should add hashing logic in real impl.
            Status = dto.Status
        };

        if (dto.RoleIds != null && dto.RoleIds.Any())
        {
            var roles = await _context.Roles.Where(r => dto.RoleIds.Contains(r.RoleId)).ToListAsync();
            foreach (var role in roles)
            {
                user.Roles.Add(role);
            }
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDTO = new UserDTO
        {
            UserId = user.UserId,
            Email = user.Email,
            Status = user.Status,
            Roles = user.Roles.Select(r => r.Name).ToList()
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDTO);
    }

    // PUT: api/Users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        user.Status = dto.Status;

        // Update Roles
        if (dto.RoleIds != null)
        {
            user.Roles.Clear();
            var roles = await _context.Roles.Where(r => dto.RoleIds.Contains(r.RoleId)).ToListAsync();
            foreach (var role in roles)
            {
                user.Roles.Add(role);
            }
        }

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

    // PUT: api/Users/5/deactivate
    [HttpPut("{id}/deactivate")]
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
    [HttpPost("{id}/roles")]
    public async Task<IActionResult> ManageUserRoles(int id, [FromBody] AssignRoleDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        user.Roles.Clear();
        if (dto.RoleIds != null && dto.RoleIds.Any())
        {
            var roles = await _context.Roles.Where(r => dto.RoleIds.Contains(r.RoleId)).ToListAsync();
            foreach (var role in roles)
            {
                user.Roles.Add(role);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Roles updated successfully" });
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.UserId == id);
    }
}
