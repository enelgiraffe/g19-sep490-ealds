using System;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/disposal/execution")]
public class DisposalExecutionController : ControllerBase
{
    private readonly IDisposalExecutionService _service;

    public DisposalExecutionController(IDisposalExecutionService service)
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

    [HttpGet("by-request/{assetRequestId:int}")]
    [Authorize(Roles = "ACCOUNTANT,DIRECTOR,DEPARTMENT_HEAD,ADMIN")]
    public async Task<IActionResult> GetByAssetRequest(int assetRequestId)
    {
        return await ExecuteAsync(() => _service.GetByAssetRequestAsync(assetRequestId));
    }

    [HttpPut("by-request/{assetRequestId:int}")]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<IActionResult> SaveDraft(int assetRequestId, [FromBody] SaveDisposalExecutionDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.SaveDraftAsync(userId, assetRequestId, dto));
    }

    [HttpPost("by-request/{assetRequestId:int}/finalize")]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<IActionResult> Finalize(int assetRequestId, [FromBody] FinalizeDisposalExecutionDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.FinalizeAsync(userId, assetRequestId));
    }

    [HttpPost("by-request/{assetRequestId:int}/record-appraisal")]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<IActionResult> RecordAppraisal(int assetRequestId, [FromBody] RecordDisposalAppraisalDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.RecordAppraisalAsync(userId, assetRequestId, dto));
    }
}
