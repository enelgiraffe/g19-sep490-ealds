using g19_sep490_ealds.Server.DTO.RequestDTO.AssetDepreciation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetDepreciationController : ControllerBase
{
    private readonly IAssetDepreciationService _service;
    private readonly IAssetRevaluationService _serviceRe;
    private readonly EALDSDbcontext _context;

    public AssetDepreciationController(
        IAssetDepreciationService service,
        EALDSDbcontext context,
        IAssetRevaluationService serviceRe)
    {
        _service = service;
        _context = context;
        _serviceRe = serviceRe;
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

    [HttpPost("revaluation")]
    public async Task<IActionResult> Revaluate([FromBody] RevaluationDTO dto)
    {

        await _serviceRe.RevaluateAsync(dto.AssetInstanceId, dto.NewValue);

        return Ok("Revaluation success");
    }

    //[HttpPut("assign-policy")]
    //public async Task<IActionResult> AssignPolicy([FromBody] AssignPolicyDTO dto)
    //{
    //    await _service.AssignPolicyAsync(dto.AssetInstanceId, dto.PolicyId);
    //    return Ok("Policy assigned");
    //}

    //chạy thủ công khấu hao
    [HttpPost("run")]
    public async Task<IActionResult> RunDepreciation()
    {
        await _service.RunMonthlyDepreciation();
        return Ok("Depreciation executed");
    }
}