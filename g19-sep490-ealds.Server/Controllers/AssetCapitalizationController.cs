using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTO.RequestDTO;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<IActionResult> Capitalize([FromBody] AssetCapitalizationRequestDTO request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            return Unauthorized("Invalid user identity.");
        }
        try
        {
            var result = await _service.CapitalizeAssetAsync(request, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("capitalize-purchase-request")]
    [ProducesResponseType(typeof(IEnumerable<Asset>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<IActionResult> CapitalizePurchaseRequest([FromBody] AssetCapitalizationFromRequestDTO request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            return Unauthorized("Invalid user identity.");
        }
        try
        {
            var result = await _service.CapitalizeFromPurchaseRequestAsync(request, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("capitalize-purchase-request-lines")]
    [ProducesResponseType(typeof(CapitalizePurchaseRequestLinesResponseDTO), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [Authorize(Roles = "ACCOUNTANT")]
    public async Task<IActionResult> CapitalizePurchaseRequestLines([FromBody] CapitalizePurchaseRequestLinesDTO request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            return Unauthorized("Invalid user identity.");
        }
        try
        {
            var result = await _service.CapitalizePurchaseRequestLinesAsync(request, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}