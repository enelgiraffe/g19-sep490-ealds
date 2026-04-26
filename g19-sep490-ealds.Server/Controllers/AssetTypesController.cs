using g19_sep490_ealds.Server.DTOs.AssetTypes;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetTypesController : ControllerBase
{
    private readonly IAssetTypeService _service;

    public AssetTypesController(IAssetTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] string? keyword)
        => await ExecuteAsync(() => _service.GetAllAsync(categoryId, keyword));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => await ExecuteAsync(() => _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetTypeDto dto)
        => await ExecuteAsync(() => _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAssetTypeDto dto)
        => await ExecuteAsync(() => _service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
