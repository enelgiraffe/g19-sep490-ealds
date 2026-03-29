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

        var instances = await _context.AssetInstances
            .Where(ai => ai.InUseDate != null)
            .Where(ai => _context.DepreciationRecords.Any(r => r.AssetInstanceId == ai.AssetInstanceId))
            .ToListAsync();

        foreach (var instance in instances)
        {
            await ProcessInstance(instance, period);
        }

        await _context.SaveChangesAsync();
    }

    private async Task ProcessInstance(AssetInstance instance, DateOnly period)
    {
        var now = DateTime.UtcNow.AddHours(7);

        if (instance.InUseDate == null) return;

        var inUseDate = instance.InUseDate.Value.ToDateTime(TimeOnly.MinValue);

        if (now < inUseDate) return;

        var exists = await _context.DepreciationRecords
            .AnyAsync(x => x.AssetInstanceId == instance.AssetInstanceId && x.Period == period);

        if (exists) return;

        var last = await _context.DepreciationRecords
            .Include(x => x.Policy)
            .Where(x => x.AssetInstanceId == instance.AssetInstanceId)
            .OrderByDescending(x => x.Period)
            .ThenByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync();

        var policy = last?.Policy;

        if (policy == null) return;

        decimal baseValue = instance.OriginalPrice;

        var monthly = DepreciationFormula.CalculateStraightLine(
            instance.OriginalPrice,
            policy.SalvageValue,
            policy.UsefullLifeMonths);

        decimal accumulated = last?.AccumulatedDepreciation ?? 0;

        var newAccumulated = accumulated + monthly;

        if (baseValue - newAccumulated < policy.SalvageValue) return;

        var remaining = baseValue - newAccumulated;

        _context.DepreciationRecords.Add(new DepreciationRecord
        {
            AssetInstanceId = instance.AssetInstanceId,
            PolicyId = policy.PolicyId,
            Period = period,
            DepreciationAmount = monthly,
            OriginalValue = baseValue,
            AccumulatedDepreciation = newAccumulated,
            RemainingValue = instance.OriginalPrice - newAccumulated,
            CreateDate = DateTime.UtcNow.AddHours(7)
        });

        instance.CurrentValue = remaining;
    }
}
