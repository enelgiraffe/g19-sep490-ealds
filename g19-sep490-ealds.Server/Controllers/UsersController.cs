using g19_sep490_ealds.Server.DTOs.Users;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
        => await ExecuteAsync(() => _usersService.GetUsersAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
        => await ExecuteAsync(() => _usersService.GetUserAsync(id));

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return await ExecuteAsync(() => _usersService.CreateUserAsync(dto));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return await ExecuteAsync(async () =>
        {
            await _usersService.UpdateUserAsync(id, dto);
            return (object)new { message = "Cập nhật người dùng thành công." };
        });
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id)
        => await ExecuteAsync(async () =>
        {
            await _usersService.DeactivateUserAsync(id);
            return (object)new { message = "Tài khoản đã được vô hiệu hóa." };
        });

    [HttpPost("{id:int}/roles")]
    public async Task<IActionResult> ManageUserRoles(int id, [FromBody] AssignRoleDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return await ExecuteAsync(async () =>
        {
            await _usersService.ManageUserRolesAsync(id, dto);
            return (object)new { message = "Roles updated successfully" };
        });
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetMetadata()
        => await ExecuteAsync(() => _usersService.GetMetadataAsync());

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
        => await ExecuteAsync(() => _usersService.GetRolesAsync());

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
        => await ExecuteAsync(() => _usersService.GetDepartmentsAsync());

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> AdminChangePassword(int id, [FromBody] AdminChangePasswordDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return await ExecuteAsync(async () =>
        {
            await _usersService.AdminChangePasswordAsync(id, dto);
            return (object)new { message = "Mật khẩu đã được thay đổi thành công." };
        });
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
