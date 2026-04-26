using g19_sep490_ealds.Server.DTOs.Assets;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _service;

    public AssetsController(IAssetService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/assets — Catalog assets (keyword, type, catalog status).
    /// Filters for warehouse, price, purchase date, and per-instance location belong on <c>GET /api/assetinstances</c>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetResponseDTO>>> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status,
        [FromQuery] int? assetTypeId,
        [FromQuery] bool warehouseStockOnly = false)
        => await ExecuteAsync(() => _service.GetAllAsync(User, keyword, status, assetTypeId, warehouseStockOnly));

    /// <summary>
    /// GET /api/assets/catalog-eligible-asset-type-ids
    /// </summary>
    [HttpGet("catalog-eligible-asset-type-ids")]
    public async Task<ActionResult<IReadOnlyList<int>>> GetCatalogEligibleAssetTypeIds([FromQuery] bool forAllocation)
        => await ExecuteAsync(() => _service.GetCatalogEligibleAssetTypeIdsAsync(User, forAllocation));

    /// <summary>
    /// GET /api/assets/code-prefixes
    /// </summary>
    [HttpGet("code-prefixes")]
    public async Task<ActionResult<IEnumerable<string>>> GetAssetCodePrefixes()
        => await ExecuteAsync(() => _service.GetAssetCodePrefixesAsync());

    /// <summary>
    /// GET /api/assets/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetDetailResponseDTO>> GetById(int id)
        => await ExecuteAsync(() => _service.GetByIdAsync(User, id));

    /// <summary>
    /// GET /api/assets/department/{departmentId}
    /// </summary>
    [HttpGet("department/{departmentId:int}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AssetInstanceResponseDTO>>> GetAssetsByDepartment(
        int departmentId,
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.GetAssetsByDepartmentAsync(User, userId, departmentId, keyword, status));
    }

    /// <summary>
    /// POST /api/assets
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetDetailResponseDTO>> Create([FromBody] CreateAssetDTO dto)
    {
        TryGetCurrentUserId(out var userId);
        try
        {
            var created = await _service.CreateAsync(User, userId > 0 ? userId : null, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.AssetId }, created);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// POST /api/assets/{id}/documents
    /// </summary>
    [HttpPost("{id:int}/documents")]
    [Authorize]
    public async Task<ActionResult<AssetDocumentDTO>> AddDocument(int id, [FromBody] AddAssetDocumentDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _service.AddDocumentAsync(userId, id, dto));
    }

    /// <summary>
    /// DELETE /api/assets/{id}/documents/{documentId}
    /// </summary>
    [HttpDelete("{id:int}/documents/{documentId:int}")]
    [Authorize]
    public async Task<IActionResult> RemoveDocument(int id, int documentId)
    {
        if (!TryGetCurrentUserId(out _)) return Unauthorized();
        try
        {
            await _service.RemoveDocumentAsync(id, documentId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// PUT /api/assets/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetDetailResponseDTO>> Update(int id, [FromBody] UpdateAssetDTO dto)
        => await ExecuteAsync(() => _service.UpdateAsync(id, dto));

    /// <summary>
    /// PUT /api/assets/{id}/status
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<ActionResult<AssetDetailResponseDTO>> ChangeStatus(int id, [FromBody] ChangeAssetStatusDTO dto)
        => await ExecuteAsync(() => _service.ChangeStatusAsync(id, dto));

    /// <summary>
    /// DELETE /api/assets/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<AssetResponseDTO>> Delete(
        int id,
        [FromQuery] AssetStatus? status,
        [FromBody] DeleteAssetDTO? dto)
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
