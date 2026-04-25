using System.Security.Claims;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/goods-receipts")]
[Authorize(Roles = "ACCOUNTANT")]
public class GoodsReceiptsController : ControllerBase
{
    private readonly IGoodsReceiptService _service;

    public GoodsReceiptsController(IGoodsReceiptService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<GoodsReceiptListResponseDto>> GetList(
        [FromQuery] int? goodsReceiptId,
        [FromQuery] int? procurementId,
        [FromQuery] int? supplierId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => await ExecuteAsync(() => _service.GetListAsync(goodsReceiptId, procurementId, supplierId, dateFrom, dateTo, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoodsReceiptDetailDto>> GetById(int id)
        => await ExecuteAsync(() => _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] GoodsReceiptCreateDto dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            var id = await _service.CreateAsync(userId, dto);
            return (object)new { goodsReceiptId = id };
        });
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
}
