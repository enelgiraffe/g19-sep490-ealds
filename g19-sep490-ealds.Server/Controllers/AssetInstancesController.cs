using g19_sep490_ealds.Server.DTOs.Assets;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace g19_sep490_ealds.Server.Controllers;

/// <summary>
/// Physical asset rows: warehouse, valuation, location, custodian, depreciation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AssetInstancesController : ControllerBase
{
    private readonly IAssetInstanceService _service;

    public AssetInstancesController(IAssetInstanceService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/assetinstances
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetInstanceResponseDTO>>> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status,
        [FromQuery] int? assetTypeId,
        [FromQuery] int? warehouseId,
        [FromQuery] int? currentDepartmentId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] bool forTransferSelection = false)
        => await ExecuteAsync(() => _service.GetAllAsync(User, keyword, status, assetTypeId, warehouseId, currentDepartmentId, minPrice, maxPrice, fromDate, toDate, forTransferSelection));

    /// <summary>
    /// GET /api/assetinstances/instance-code-prefixes
    /// </summary>
    [HttpGet("instance-code-prefixes")]
    public async Task<ActionResult<IEnumerable<string>>> GetInstanceCodePrefixes()
        => await ExecuteAsync(() => _service.GetInstanceCodePrefixesAsync());

    /// <summary>
    /// GET /api/assetinstances/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetInstanceResponseDTO>> GetById(int id)
        => await ExecuteAsync(() => _service.GetByIdAsync(User, id));

    /// <summary>
    /// POST /api/assetinstances
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetInstanceResponseDTO>> Create([FromBody] CreateAssetInstanceDTO dto)
    {
        TryGetCurrentUserId(out var userId);
        try
        {
            var created = await _service.CreateAsync(User, userId > 0 ? userId : null, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.AssetInstanceId }, created);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// PUT /api/assetinstances/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetInstanceResponseDTO>> Update(int id, [FromBody] UpdateAssetInstanceDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.UpdateAsync(User, userId, id, dto));
    }

    /// <summary>
    /// PUT /api/assetinstances/{id}/status
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<ActionResult<AssetInstanceResponseDTO>> ChangeStatus(int id, [FromBody] ChangeAssetInstanceStatusDTO dto)
        => await ExecuteAsync(() => _service.ChangeStatusAsync(id, dto));

    /// <summary>
    /// DELETE /api/assetinstances/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<AssetInstanceResponseDTO>> Delete(
        int id,
        [FromQuery] AssetStatus? status,
        [FromBody] DeleteAssetInstanceDTO? dto)
        => await ExecuteAsync(() => _service.DeleteAsync(id, status, dto));

    // ── Controller utilities ──────────────────────────────────────────────────

    private bool TryGetCurrentUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out userId) || userId <= 0)
        {
            userId = 0;
            return false;
        }
        return true;
    }

    private async Task<ActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
