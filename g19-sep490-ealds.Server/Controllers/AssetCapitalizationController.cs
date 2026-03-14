using g19_sep490_ealds.Server.DTO.RequestDTO;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetCapitalizationController : ControllerBase
{
    private readonly IAssetCapitalizationService _service;

    public AssetCapitalizationController(IAssetCapitalizationService service)
    {
        _service = service;
    }

    [HttpPut("change-status")]
    [ProducesResponseType(typeof(IEnumerable<Asset>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    //[Authorize(Roles = "Admin, Employee")]
    public async Task<IActionResult> Capitalize([FromBody] AssetCapitalizationRequestDTO request)
    {
        var userId = 1;

        var result = await _service.CapitalizeAssetAsync(request, userId);

        return Ok(result);
    }
}