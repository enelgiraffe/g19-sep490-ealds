using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetDepreciationService : IAssetDepreciationService
{
    private readonly EALDSDbcontext _context;

    public AssetDepreciationService(EALDSDbcontext context)
    {
        _context = context;
    }

    public async Task RunMonthlyDepreciation()
    {
        var now = DateTime.UtcNow.AddHours(7);
        var period = new DateOnly(now.Year, now.Month, 1);

        var assets = _context.Assets
            .Where(a => a.DepreciationPolicyId != null && a.InUseDate != null)
            .ToList();

        foreach (var asset in assets)
        {
            await ProcessAsset(asset, period);
        }

        await _context.SaveChangesAsync();
    }

    private async Task ProcessAsset(Asset asset, DateOnly period)
    {
        var now = DateTime.UtcNow.AddHours(7);

        // chưa có ngày sử dụng
        if (asset.InUseDate == null) return;

        var inUseDate = asset.InUseDate.Value.ToDateTime(TimeOnly.MinValue);

        // chưa tới ngày sử dụng
        if (now < inUseDate) return;

        // kiểm tra đã có record tháng này chưa
        var exists = await _context.DrepreciationRecords
            .AnyAsync(x => x.AssetId == asset.AssetId && x.Period == period);

        if (exists) return;

        var policy = await _context.DepreciationPolicies
            .FirstOrDefaultAsync(p => p.PolicyId == asset.DepreciationPolicyId);

        if (policy == null) return;

        decimal baseValue = asset.OriginalPrice;

        var monthly = DepreciationFormula.CalculateStraightLine(
            asset.OriginalPrice,
            policy.SalvageValue,
            policy.UsefullLifeMonths);

        var last = await _context.DrepreciationRecords
            .Where(x => x.AssetId == asset.AssetId)
            .OrderByDescending(x => x.Period)
            .FirstOrDefaultAsync();

        decimal accumulated = last?.AccumulatedDepreciation ?? 0;

        var newAccumulated = accumulated + monthly;

        if (baseValue - newAccumulated < policy.SalvageValue) return;

        var remaining = baseValue - newAccumulated;

        _context.DrepreciationRecords.Add(new DrepreciationRecord
        {
            AssetId = asset.AssetId,
            PolicyId = policy.PolicyId,
            Period = period,
            DepreciationAmount = monthly,
            AccumulatedDepreciation = newAccumulated,
            RemainingValue = asset.OriginalPrice - newAccumulated,
            CreateDate = DateTime.UtcNow.AddHours(7)
        });

        asset.CurrentValue = remaining;
    }
}