using System.Security.Claims;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/supplier-invoices")]
[Authorize(Roles = "ACCOUNTANT")]
public class SupplierInvoicesController : ControllerBase
{
    private readonly ISupplierInvoiceService _service;

    public SupplierInvoicesController(ISupplierInvoiceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<SupplierInvoiceListResponseDto>> GetList(
        [FromQuery] string? invoiceNumber,
        [FromQuery] int? supplierId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => await ExecuteAsync(() => _service.GetListAsync(invoiceNumber, supplierId, dateFrom, dateTo, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierInvoiceDetailDto>> GetById(int id)
        => await ExecuteAsync(() => _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] SupplierInvoiceCreateDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var id = await _service.CreateAsync(userId, dto);
            return (object)new { supplierInvoiceId = id };
        });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () => await _service.CancelAsync(id));
    }

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
