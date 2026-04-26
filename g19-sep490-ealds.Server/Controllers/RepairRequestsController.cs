using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using g19_sep490_ealds.Server.Services.Interface;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/repair")]
public class RepairRequestsController : ControllerBase
{
    private readonly IRepairRequestService _service;

    public RepairRequestsController(IRepairRequestService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetListAsync(userId));
    }

    [Authorize]
    [HttpGet("damaged-pending")]
    public async Task<IActionResult> GetDamagedPending()
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetDamagedPendingAsync(userId));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRepairRequest([FromBody] RepairRequestDTO dto)
    {
        if (dto == null) return BadRequest("Request body is required.");
        return await ExecuteAsync(() => _service.CreateAsync(dto));
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartRepair(int id, [FromBody] RepairStartDto dto)
    {
        if (dto == null) return BadRequest("Request body is required.");
        return await ExecuteAsync(() => _service.StartRepairAsync(id, dto));
    }

    [HttpPost("tasks/{taskId}/complete")]
    public async Task<IActionResult> CompleteRepair(int taskId, [FromBody] RepairCompleteDto dto)
    {
        if (dto == null) return BadRequest("Request body is required.");
        return await ExecuteAsync(() => _service.CompleteRepairAsync(taskId, dto));
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
