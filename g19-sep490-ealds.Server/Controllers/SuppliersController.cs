using g19_sep490_ealds.Server.DTOs.Suppliers;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _service;

    public SuppliersController(ISupplierService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliers([FromQuery] string? keyword)
        => await ExecuteAsync(() => _service.GetAllAsync(keyword));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSupplier(int id)
        => await ExecuteAsync(() => _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDTO dto)
        => await ExecuteAsync(() => _service.CreateAsync(dto));

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDTO dto)
    {
        try
        {
            await _service.UpdateAsync(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        try
        {
            var msg = await _service.DeleteAsync(id);
            return msg is null ? NoContent() : Ok(new { message = msg });
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
