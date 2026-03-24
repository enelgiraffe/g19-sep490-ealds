using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MaintenanceTemplateController : ControllerBase
{
    private readonly IMaintenanceTemplateService _service;

    public MaintenanceTemplateController(IMaintenanceTemplateService service)
    {
        _service = service;
    }
    [HttpPost("add-template")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceTemplate>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> AddTemplateAsync([FromBody] TemplateCreateDTO create)
    {
        try
        {
            var response = await _service.CreateTemplateAsync(create);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("get-all")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceTemplate>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<IEnumerable<MaintenanceTemplate>>> GetAllTemplateAsync()
    {
        try
        {
            var response = await _service.GetAllTemplatesAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("find-id/{id}")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceTemplate>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> FindTemlateById(int id)
    {
        try
        {
            var response = await _service.FindTemplateByIdAsync(id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("update/{id}")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceTemplate>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> UpdateTemplate([FromBody] TemplateUpdateDTO update, int id)
    {
        try
        {
            var response = await _service.UpdatTemplateAsync(id, update);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("change-status/{id}")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceTemplate>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ToggleStatusAsync(int id)
    {
        try
        {
            var response = await _service.ToggleTemplateStatusAsync(id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("delete-permanent/{id}")]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceTemplate>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> HardDeleteTemplate(int id)
    {
        try
        {
            var response = await _service.HardDeleteTemplateAsync(id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}