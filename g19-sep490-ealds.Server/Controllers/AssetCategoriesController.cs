using g19_sep490_ealds.Server.DTOs.AssetCategory;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetCategoriesController : ControllerBase
{
    private readonly IAssetCategoryService _service;

    public AssetCategoriesController(IAssetCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? keyword)
        => await ExecuteAsync(() => _service.GetAllAsync(keyword));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => await ExecuteAsync(() => _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetCategoryDto dto)
        => await ExecuteAsync(() => _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAssetCategoryDto dto)
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
