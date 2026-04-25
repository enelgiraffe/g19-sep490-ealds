using System;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.BudgetAllocation;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ACCOUNTANT")]
public class BudgetAllocationsController : ControllerBase
{
    private readonly IBudgetAllocationService _service;

    public BudgetAllocationsController(IBudgetAllocationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int? departmentId,
        [FromQuery] string? status)
        => await ExecuteAsync(() => _service.GetListAsync(departmentId, status));

    [HttpGet("asset-instance-options")]
    public async Task<IActionResult> GetAssetInstanceOptions(
        [FromQuery] int categoryId,
        [FromQuery] int departmentId,
        [FromQuery] string mode,
        [FromQuery] string? search)
        => await ExecuteAsync(() => _service.GetAssetInstanceOptionsAsync(categoryId, departmentId, mode, search));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetAllocationDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var row = await _service.CreateAsync(userId, dto);
            return (object)Created($"/api/BudgetAllocations/{row.Id}", row);
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId) && userId > 0;
    }

    private async Task<ActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
