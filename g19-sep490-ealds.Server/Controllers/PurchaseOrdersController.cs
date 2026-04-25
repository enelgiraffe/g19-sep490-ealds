using System.Security.Claims;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Implementation;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize(Roles = "ACCOUNTANT")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrdersController(IPurchaseOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PurchaseOrderListResponseDto>> GetList(
        [FromQuery] int? procurementId,
        [FromQuery] int? supplierId,
        [FromQuery] int? status,
        [FromQuery] bool receivingEligible = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => await ExecuteAsync(() => _service.GetListAsync(procurementId, supplierId, status, receivingEligible, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(int id)
        => await ExecuteAsync(() => _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] PurchaseOrderCreateDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var id = await _service.CreateAsync(userId, dto);
            return (object)new { procurementId = id };
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] PurchaseOrderUpdateDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            await _service.UpdateAsync(userId, id, dto);
            return (object)new { procurementId = id };
        });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id, [FromBody] CancelPurchaseOrderDto? _ = null)
        => await ExecuteAsync(async () =>
        {
            await _service.CancelAsync(id);
            return (object)new { procurementId = id, status = PurchaseOrderService.StatusCancelled };
        });

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
        => await ExecuteAsync(async () => await _service.DeleteAsync(id));

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
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

    private async Task<ActionResult> ExecuteAsync(Func<Task> action)
    {
        try { await action(); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
