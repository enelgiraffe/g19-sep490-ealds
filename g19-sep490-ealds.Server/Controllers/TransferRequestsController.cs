using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/transfer")]
[Authorize]
public class TransferRequestsController : ControllerBase
{
    private readonly ITransferRequestService _service;

    public TransferRequestsController(ITransferRequestService service)
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
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetListAsync(userId, User.IsInRole("ACCOUNTANT")));
    }

    [HttpGet("{id:int}/handover-records")]
    public async Task<ActionResult<IEnumerable<TransferHandoverRecordItemDto>>> GetHandoverRecords(int id)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetHandoverRecordsAsync(userId, User.IsInRole("ACCOUNTANT"), id));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransferRequest([FromBody] TransferRequestDTO dto)
    {
        if (dto == null) return BadRequest("Request body is required.");
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.CreateAsync(userId, dto));
    }

    [HttpPut("{assetRequestId:int}/draft")]
    public async Task<IActionResult> UpdateIncompleteTransferDraft(int assetRequestId, [FromBody] UpdateTransferDraftBody? body)
    {
        if (body == null) return BadRequest("Request body is required.");
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var id = await _service.UpdateDraftAsync(userId, assetRequestId, body);
            return new { assetRequestId = id };
        });
    }

    [HttpDelete("{assetRequestId:int}")]
    public async Task<IActionResult> DeleteTransferRequest(int assetRequestId)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        try
        {
            await _service.DeleteAsync(userId, assetRequestId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:int}/confirm-send")]
    public async Task<IActionResult> ConfirmSend(int id, [FromBody] TransferHandoverConfirmBody? body)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var isReady = await _service.ConfirmSendAsync(userId, User.IsInRole("ACCOUNTANT"), id, body);
            return new { message = "Xác nhận gửi thành công.", isReady };
        });
    }

    [HttpPost("{id:int}/confirm-receive")]
    public async Task<IActionResult> ConfirmReceive(int id, [FromBody] TransferHandoverConfirmBody? body)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var isReady = await _service.ConfirmReceiveAsync(userId, User.IsInRole("ACCOUNTANT"), id, body);
            return new { message = "Xác nhận nhận thành công.", isReady };
        });
    }
}
