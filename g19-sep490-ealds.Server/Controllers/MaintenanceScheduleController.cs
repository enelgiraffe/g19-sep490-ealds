using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceSchedule;
using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MaintenanceScheduleController : ControllerBase
{
    private readonly IMaintenanceScheduleService _service;

    public MaintenanceScheduleController(IMaintenanceScheduleService service)
    {
        _service = service;
    }

    [HttpPost("add-schedule")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceSchedule>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> AddScheduleAsync([FromBody] ScheduleCreateDTO create)
    {
        try
        {
            var response = await _service.CreateScheduleAsync(create);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("find-by/{assetId}")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceSchedule>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetScheduleByAsset(int assetId)
    {
        try
        {
            var response = await _service.GetScheduleByAssetAsync(assetId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("find-by-instance/{instanceId}")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceSchedule>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetScheduleByInstance(int instanceId)
    {
        try
        {
            var response = await _service.GetScheduleByInstanceAsync(instanceId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("change-status/{id}")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceSchedule>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ToggleStatusAsync(int id)
    {
        try
        {
            var response = await _service.ToggleScheduleAsync(id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}