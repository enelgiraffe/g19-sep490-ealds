using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using g19_sep490_ealds.Server.Services.Interface;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/disposal")]
public class DisposalRequestsController : ControllerBase
{
    private readonly IDisposalRequestService _service;

    public DisposalRequestsController(IDisposalRequestService service)
    {
        _service = service;
    }

    private bool TryGetCurrentUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private async Task<ActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransferRequestListItemDTO>>> GetList()
    {
        return await ExecuteAsync(() => _service.GetListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CreateDisposalRequest([FromBody] AssetDisposalRequestDTO dto)
    {
        if (dto == null) return BadRequest("Request body is required.");
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var (assetRequestId, diposalId) = await _service.CreateAsync(userId, dto);
            return new { assetRequestId, diposalId };
        });
    }
}
