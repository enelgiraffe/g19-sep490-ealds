using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using g19_sep490_ealds.Server.Services.Interface;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/maintenance")]
public class MaintenanceRequestsController : ControllerBase
{
    private readonly IMaintenanceRequestService _service;

    public MaintenanceRequestsController(IMaintenanceRequestService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET danh sách yêu cầu bảo dưỡng
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpGet("list")]
    public async Task<IActionResult> GetList()
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetListAsync(userId));
    }

    [HttpPost]
    public async Task<IActionResult> RequestExecution([FromBody] MaintenanceRequestDTO dto)
    {
        if (dto == null) return BadRequest("Request body is required.");
        return await ExecuteAsync(() => _service.CreateAsync(dto));
    }

    /// <summary>
    /// DELETE đề xuất (chỉ nháp / đã nộp).
    /// </summary>
    /// <param name="assetRequestId"></param>
    /// <returns></returns>
    [HttpDelete("{assetRequestId:int}")]
    public async Task<IActionResult> DeleteMaintenanceRequest(int assetRequestId)
    {
        return await ExecuteNoContentAsync(() => _service.DeleteMaintenanceRequestAsync(assetRequestId));
    }

    /// <summary>
    /// BD bảo dưỡng
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartMaintenance(int id, [FromBody] MaintenanceStartDto dto)
    {
        if (dto == null) return BadRequest("Request body is required.");
        return await ExecuteAsync(() => _service.StartMaintenanceAsync(id, dto));
    }

    /// <summary>
    /// Hoàn thiện bảo dưỡng
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("tasks/{taskId}/complete")]
    public async Task<IActionResult> CompleteMaintenance(int taskId, [FromBody] MaintenanceCompleteDto dto)
    {
        if (dto == null) return BadRequest("Request body is required.");
        return await ExecuteAsync(() => _service.CompleteMaintenanceAsync(taskId, dto));
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

    private async Task<IActionResult> ExecuteNoContentAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}