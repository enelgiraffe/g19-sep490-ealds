using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MaintenanceRecordController : ControllerBase
{
    private readonly IMaintenanceRecordService _service;

    public MaintenanceRecordController(IMaintenanceRecordService service)
    {
        _service = service;
    }

    [HttpGet("assetInstance/{assetInstanceId}")]
    public async Task<IActionResult> GetByAsset(int assetInstance)
    {
        var result = await _service.GetRecordsByAssetAsync(assetInstance);
        return Ok(result);
    }
}
