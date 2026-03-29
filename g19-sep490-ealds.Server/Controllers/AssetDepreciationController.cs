using g19_sep490_ealds.Server.DTO.RequestDTO.AssetDepreciation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetDepreciationController : ControllerBase
{
    private readonly IAssetDepreciationService _service;

    public AssetDepreciationController(IAssetDepreciationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePolicyDTO dto)
    {
        var policy = new DepreciationPolicy
        {
            Name = dto.Name,
            Method = dto.Method,
            UsefullLifeMonths = dto.UsefullLifeMonths,
            SalvageValue = dto.SalvageValue,
            CreateDate = DateTime.Now,
            IsActive = true
        };

        _context.DepreciationPolicies.Add(policy);
        await _context.SaveChangesAsync();

        return Ok(policy);
    }
}