using System;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.Allocation;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/handover")]
[Authorize]
public class HandoverRequestsController : ControllerBase
{
    private readonly IHandoverRequestService _service;

    public HandoverRequestsController(IHandoverRequestService service)
    {
        _service = service;
    }

    [HttpGet("department-assigned")]
    public async Task<IActionResult> GetDepartmentAssigned([FromQuery] int assetId)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetDepartmentAssignedAsync(userId, assetId));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentAllocationRequestDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var id = await _service.CreateAsync(userId, dto);
            return (object)new { assetRequestId = id };
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetListAsync(userId));
    }

    [HttpGet("orders/{orderId:int}")]
    public async Task<IActionResult> GetOrder(int orderId)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetOrderAsync(userId, orderId));
    }

    [HttpPost("orders/{orderId:int}/confirm")]
    public async Task<IActionResult> ConfirmOrder(int orderId)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            await _service.ConfirmOrderAsync(userId, orderId);
            return (object)new { assetAllocationOrderId = orderId, status = "confirmed" };
        });
    }

    [HttpGet("orders-summary")]
    public async Task<IActionResult> ListHandoverOrdersSummary()
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetOrdersSummaryAsync(userId));
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
