using g19_sep490_ealds.Server.DTO.RequestDTO.AssetDepreciation;
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
    private readonly IAssetRevaluationService _serviceRe;
    private readonly EaldsDbContext _context;

    public AssetDepreciationController(
        IAssetDepreciationService service,
        EaldsDbContext context,
        IAssetRevaluationService serviceRe)
    {
        _service = service;
        _context = context;
        _serviceRe = serviceRe;
    }

    /// <summary>GET /api/AssetDepreciation/instance/{instanceId} — Lịch sử khấu hao của một cá thể, sắp xếp mới nhất trước.</summary>
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

    /// <summary>GET /api/AssetDepreciation/policies — Danh sách chính sách khấu hao đang active.</summary>
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

    [HttpPost("revaluation")]
    public async Task<IActionResult> Revaluate([FromBody] RevaluationDTO dto)
    {
        // Update carrying value and write revaluation log.
        await _serviceRe.RevaluateAsync(dto.AssetInstanceId, dto.NewValue);

        return Ok("Revaluation success");
    }

    [HttpPut("assign-policy")]
    public async Task<IActionResult> AssignPolicy([FromBody] AssignPolicyDTO dto)
    {
        // Assign policy to target asset instance.
        await _service.AssignPolicyAsync(dto.AssetInstanceId, dto.PolicyId);
        return Ok("Policy assigned");
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateDepreciation([FromBody] UpdateDepreciationDTO dto)
    {
        // Manual adjust one depreciation record.
        await _service.UpdateDepreciation(dto.RecordId, dto.NewAmount);
        return Ok("Depreciation updated");
    }

    // Trigger monthly depreciation manually.
    [HttpPost("run")]
    public async Task<IActionResult> RunDepreciation()
    {
        await _service.RunMonthlyDepreciation();
        return Ok("Depreciation executed");
    }

    [HttpPost("manual-run")]
    public async Task<IActionResult> RunManualDepreciation([FromBody] ManualDepreciationRunDTO dto)
    {
        // Chạy khấu hao thủ công để test theo kỳ/tài sản tùy chọn.
        await _service.RunManualDepreciation(dto.AssetInstanceId, dto.Year, dto.Month);
        return Ok("Manual depreciation executed");
    }

    [HttpPost("manual-recalculate")]
    public async Task<IActionResult> RecalculateFromPeriod([FromBody] ManualDepreciationRunDTO dto)
    {
        if (!dto.AssetInstanceId.HasValue || !dto.Year.HasValue || !dto.Month.HasValue)
            return BadRequest("AssetInstanceId, Year, Month are required");

        // Chạy tính lại khấu hao từ kỳ chỉ định để test BR-28.
        await _service.RecalculateFromPeriod(dto.AssetInstanceId.Value, dto.Year.Value, dto.Month.Value);
        return Ok("Depreciation recalculated from period");
    }
}