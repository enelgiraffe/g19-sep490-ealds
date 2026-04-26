using g19_sep490_ealds.Server.DTOs.AssetRequests;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/purchase")]
public class AssetRequestsController : ControllerBase
{
    private readonly IAssetRequestService _service;

    public AssetRequestsController(IAssetRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssetRequestListItemDTO>>> GetList([FromQuery] int? requestTypeId)
        => await ExecuteAsync(() => _service.GetPurchaseListAsync(requestTypeId ?? 1));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetRequestDetailDTO>> GetById(int id)
        => await ExecuteAsync(() => _service.GetPurchaseByIdAsync(id));

    [HttpGet("{id:int}/lines")]
    public async Task<ActionResult<List<AssetRequestPurchaseLineResponseDTO>>> GetPurchaseLines(int id)
        => await ExecuteAsync(() => _service.GetPurchaseLinesAsync(id));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] AssetRequestDTO dto)
        => await ExecuteAsync(async () =>
        {
            var id = await _service.CreateAsync(dto);
            return (object)new { assetRequestId = id };
        });

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] AssetRequestDTO dto)
        => await ExecuteAsync(async () =>
        {
            var resultId = await _service.UpdateAsync(id, dto);
            return (object)new { assetRequestId = resultId };
        });

    [HttpPost("{id:int}/revert-to-draft")]
    public async Task<ActionResult> RevertToDraft(int id, [FromBody] RevertToDraftDTO dto)
        => await ExecuteAsync(async () =>
        {
            await _service.RevertToDraftAsync(id, dto?.UserId ?? 0);
            return (object)new { assetRequestId = id, status = -1 };
        });

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteDraft(int id, [FromBody] RevertToDraftDTO dto)
        => await ExecuteAsync(async () => await _service.DeleteDraftAsync(id, dto?.UserId ?? 0));

    // ── Generic routes (different base path) ──────────────────────────────────

    [HttpGet]
    [Route("/api/Assets/Requests/{id:int}")]
    public async Task<ActionResult<AssetRequestFullDetailDTO>> GetDetails(int id)
        => await ExecuteAsync(() => _service.GetDetailsAsync(id));

    [HttpGet]
    [Route("/api/Assets/Requests")]
    public async Task<ActionResult<AssetRequestPagedResultDTO>> List(
        [FromQuery] int? status,
        [FromQuery] int? requestTypeId,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => await ExecuteAsync(() => _service.ListAsync(status, requestTypeId, userId, page, pageSize));

    // ── Helpers ───────────────────────────────────────────────────────────────

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
