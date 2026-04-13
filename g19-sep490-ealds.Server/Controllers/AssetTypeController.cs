using g19_sep490_ealds.Server.DTO.RequestDTO.AssetType;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetTypeController : ControllerBase
{
    private readonly IAssetTypeService _service;

    public AssetTypeController(IAssetTypeService service)
    {
        _service = service;
    }

    [HttpPost("add-type")]
    [ProducesResponseType(typeof(IEnumerable<AssetType>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> AddAssetType([FromBody] AssetTypeCreateDTO create)
    {
        try
        {
            var response = await _service.CreateAssetTypeAsync(create);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("get-all")]
    [ProducesResponseType(typeof(IEnumerable<AssetType>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<IEnumerable<AssetType>>> GetAllAssetTypec()
    {
        try
        {
            var response = await _service.GetAllAssetTypeAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("update/{id}")]
    [ProducesResponseType(typeof(IEnumerable<Asset>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    //[AllowAnonymous]
    public async Task<IActionResult> UpdateAssetType([FromBody] AssetTypeUpdateDTO update, int id)
    {
        try
        {
            var response = await _service.UpdateAssetTypeAsync(id, update);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("delete-permanent/{id}")]
    [ProducesResponseType(typeof(IEnumerable<AssetType>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    //[AllowAnonymous]
    public async Task<IActionResult> HardDeleteAssetType(int id)
    {
        try
        {
            var response = await _service.DeleteAssetTypeAsync(id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}