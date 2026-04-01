using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetDepreciationService : IAssetDepreciationService
{
    private readonly EaldsDbContext _context;

    public AssetDepreciationService(EaldsDbContext context)
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

        foreach (var instance in assets)
        {
            await ProcessInstance(instance, period);
        }

        await _context.SaveChangesAsync();
    }

    private async Task ProcessInstance(AssetInstance assetInstance, DateOnly period)
    {
        // chưa capitalize 
        if (assetInstance.AssetCapitalizations == null || !assetInstance.AssetCapitalizations.Any())
            return;

        var now = DateTime.UtcNow.AddHours(7);

        // chưa có ngày sử dụng
        if (assetInstance.InUseDate == null) return;

        var inUseDate = assetInstance.InUseDate.Value.ToDateTime(TimeOnly.MinValue);

        if (now < inUseDate) return;

        // kiểm tra đã có record tháng này chưa
        var exists = await _context.DepreciationRecords
            .AnyAsync(x => x.AssetInstanceId == assetInstance.AssetInstanceId && x.Period == period);

        if (exists) return;

        var last = await _context.DepreciationRecords
            .Include(x => x.Policy)
            .Where(x => x.AssetInstanceId == assetInstance.AssetInstanceId)
            .OrderByDescending(x => x.Period)
            .ThenByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync();

        var policy = last?.Policy;

        if (policy == null) return;

        decimal baseValue = assetInstance.OriginalPrice;

        var monthly = DepreciationFormula.CalculateStraightLine(
            assetInstance.OriginalPrice,
            policy.SalvageValue,
            policy.UsefullLifeMonths);

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
            OriginalValue = baseValue,
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