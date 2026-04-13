using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RepairRecordController : ControllerBase
{
    private readonly IRepairRecordService _service;

    public RepairRecordController(IRepairRecordService service)
    {
        _service = service;
    }

    [HttpGet("asset/{assetId}")]
    public async Task<IActionResult> GetHistoryByAsset(int assetId)
    {
        var result = await _service.GetHistoryByAssetAsync(assetId);
        return Ok(result);
    }

    [HttpGet("instance/{instanceId}")]
    public async Task<IActionResult> GetHistoryByInstance(int instanceId)
    {
        var result = await _service.GetHistoryByInstanceAsync(instanceId);
        return Ok(result);
    }
}
