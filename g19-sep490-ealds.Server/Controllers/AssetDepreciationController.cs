using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetDepreciationController : ControllerBase
{
    private readonly IAssetDepreciationService _service;
    private readonly EaldsDbContext _context;

    public AssetDepreciationController(
        IAssetDepreciationService service,
        EaldsDbContext context)
    {
        _service = service;
        _context = context;
    }

    /// <summary>
    /// GET Lịch sử khấu hao của một cá thể
    /// </summary>
    /// <param name="instanceId"></param>
    /// <returns></returns>
    [HttpGet("instance/{instanceId:int}")]
    public async Task<IActionResult> GetByInstanceId(int instanceId)
    {
        var records = await _context.DepreciationRecords
            .AsNoTracking()
            .Where(r => r.AssetInstanceId == instanceId)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.CreateDate)
            .Select(r => new
            {
                r.RecordId,
                r.AssetInstanceId,
                r.Period,
                r.DepreciationAmount,
                r.OriginalValue,
                r.RemainingValue,
                r.AccumulatedDepreciation,
                r.CreateDate,
                r.IsPosted
            })
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>
    /// GET Danh sách chính sách khấu hao đang active
    /// </summary>
    /// <returns></returns>
    [HttpGet("policies")]
    public async Task<IActionResult> GetPolicies()
    {
        var policies = await _context.DepreciationPolicies
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.PolicyId,
                p.Name,
                p.Method,
                p.UsefullLifeMonths,
                p.SalvageValue,
            })
            .ToListAsync();
        return Ok(policies);
    }

    /// <summary>
    /// Tạo policy
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Create(CreatePolicyDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Policy name is required");
        if (dto.UsefullLifeMonths <= 0)
            return BadRequest("Useful life months must be greater than 0");
        if (dto.SalvageValue < 0)
            return BadRequest("Salvage value cannot be negative");

        // Create a new depreciation policy.
        var policy = new DepreciationPolicy
        {
            Name = dto.Name.Trim(),
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

    /// <summary>
    /// Gán Policy
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPut("assign-policy")]
    public async Task<IActionResult> AssignPolicy([FromBody] AssignPolicyDTO dto)
    {
        // Assign policy to target asset instance.
        await _service.AssignPolicyAsync(dto.AssetInstanceId, dto.PolicyId);
        return Ok("Policy assigned");
    }


    /// <summary>
    /// Chạy khấu hao thủ công
    /// </summary>
    /// <returns></returns>
    [HttpPost("run")]
    public async Task<IActionResult> RunDepreciation()
    {
        await _service.RunMonthlyDepreciation();
        return Ok("Depreciation executed");
    }

    /// <summary>
    /// Chạy khấu hao 1 assetInstance
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("manual-run")]
    public async Task<IActionResult> RunManualDepreciation([FromBody] ManualDepreciationRunDTO dto)
    {
        // Chạy khấu hao thủ công để test theo kỳ/tài sản tùy chọn.
        await _service.RunManualDepreciation(dto.AssetInstanceId, dto.Year, dto.Month);
        return Ok("Manual depreciation executed");
    }
}