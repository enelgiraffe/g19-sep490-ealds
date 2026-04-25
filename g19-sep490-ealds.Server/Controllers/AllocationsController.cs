using System;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Allocations")]
public class AllocationsController : ControllerBase
{
    private readonly IAllocationsService _service;

    public AllocationsController(IAllocationsService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => await ExecuteAsync(() => _service.GetSummaryAsync());

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
        => await ExecuteAsync(() => _service.GetTransactionsAsync());

    [HttpPost("allocate")]
    public async Task<IActionResult> Allocate([FromBody] CreateAllocationRequestDTO dto)
        => await ExecuteAsync(async () =>
        {
            var id = await _service.AllocateAsync(dto);
            return (object)new { id };
        });

    [HttpPost("recall")]
    public async Task<IActionResult> Recall([FromBody] CreateAllocationRequestDTO dto)
        => await ExecuteAsync(async () =>
        {
            var id = await _service.RecallAsync(dto);
            return (object)new { id };
        });

    [HttpPut("transactions/{id}/approve")]
    public async Task<IActionResult> ApproveTransaction(int id)
        => await ExecuteAsync(async () =>
        {
            await _service.ApproveTransactionAsync(id);
            return (object)new { status = "success" };
        });

    [HttpDelete("transactions/{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
        => await ExecuteAsync(async () =>
        {
            await _service.DeleteTransactionAsync(id);
            return (object)new { status = "success" };
        });

    private async Task<ActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
