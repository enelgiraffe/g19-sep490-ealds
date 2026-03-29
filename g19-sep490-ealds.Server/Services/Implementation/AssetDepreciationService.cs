using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

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

        var assets = await _context.AssetInstances
                .Include(a => a.DepreciationRecords)
                .Include(a => a.DepreciationPolicy)
                .Include(a => a.AssetCapitalizations)
                .Where(a => a.DepreciationPolicyId != null && a.InUseDate != null)
                .ToListAsync();

        foreach (var asset in assets)
        {
            await ProcessAsset(asset, period);
        }

        await _context.SaveChangesAsync();
    }

    private async Task ProcessAsset(AssetInstance assetInstance, DateOnly period)
    {
        // chưa capitalize 
        if (assetInstance.AssetCapitalizations == null || !assetInstance.AssetCapitalizations.Any())
            return;

        var now = DateTime.UtcNow.AddHours(7);

        // chưa có ngày sử dụng
        if (assetInstance.InUseDate == null) return;

        var inUseDate = assetInstance.InUseDate.Value.ToDateTime(TimeOnly.MinValue);

        // chưa tới ngày sử dụng
        if (now < inUseDate) return;

        // kiểm tra đã có record tháng này chưa
        var exists = await _context.DepreciationRecords
            .AnyAsync(x => x.AssetInstanceId == assetInstance.AssetInstanceId && x.Period == period);

        if (exists) return;

        if (assetInstance.Status == (int)AssetStatus.Disposed)
            return;

        var policy = await _context.DepreciationPolicies
            .FirstOrDefaultAsync(p => p.PolicyId == assetInstance.DepreciationPolicyId);

        if (policy == null) return;

        decimal baseValue = assetInstance.OriginalPrice;

        var monthly = DepreciationFormula.CalculateStraightLine(
            assetInstance.OriginalPrice,
            policy.SalvageValue,
            policy.UsefullLifeMonths);

        var last = await _context.DepreciationRecords
            .Where(x => x.AssetInstanceId == assetInstance.AssetInstanceId)
            .OrderByDescending(x => x.Period)
            .FirstOrDefaultAsync();

        decimal accumulated = last?.AccumulatedDepreciation ?? 0;

        var newAccumulated = accumulated + monthly;

        if (baseValue - newAccumulated < policy.SalvageValue) return;

        var remaining = baseValue - newAccumulated;

        _context.DepreciationRecords.Add(new DepreciationRecord
        {
            AssetInstanceId = assetInstance.AssetInstanceId,
            PolicyId = policy.PolicyId,
            Period = period,
            DepreciationAmount = monthly,
            AccumulatedDepreciation = newAccumulated,
            RemainingValue = assetInstance.OriginalPrice - newAccumulated,
            CreateDate = DateTime.UtcNow.AddHours(7)
        });

        assetInstance.CurrentValue = remaining;
    }

    public async Task UpdateDepreciation(int recordId, decimal newAmount)
    {
        var record = await _context.DepreciationRecords.FindAsync(recordId)
            ?? throw new Exception("Record not found");

        if (record.IsPosted)
            throw new Exception("Cannot modify posted record");

        record.DepreciationAmount = newAmount;

        await _context.SaveChangesAsync();
    }
}