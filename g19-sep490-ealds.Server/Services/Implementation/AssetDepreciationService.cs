using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetDepreciationService : IAssetDepreciationService
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private readonly EaldsDbContext _context;

    public AssetDepreciationService(EaldsDbContext context)
    {
        _context = context;
    }

    public async Task RunMonthlyDepreciation(DateTime? scheduledFireTimeUtc = null)
    {
        // Chạy một chu kỳ khấu hao cho tháng hiện tại (hoặc tháng dự kiến chạy nếu bị misfire).
        var now = (scheduledFireTimeUtc ?? DateTime.UtcNow) + VietnamOffset;
        var period = new DateOnly(now.Year, now.Month, 1);
        await RunDepreciationForPeriod(period, null, DateTime.UtcNow + VietnamOffset);
    }

    public async Task RunManualDepreciation(int? assetInstanceId, int? year, int? month)
    {
        // Chạy test khấu hao thủ công theo tài sản/kỳ chỉ định.
        var now = DateTime.UtcNow + VietnamOffset;
        var runYear = year ?? now.Year;
        var runMonth = month ?? now.Month;

        if (runMonth is < 1 or > 12)
            throw new Exception("Month must be from 1 to 12");
        if (runYear < 2000 || runYear > 3000)
            throw new Exception("Year is out of valid range");

        var period = new DateOnly(runYear, runMonth, 1);
        await RunDepreciationForPeriod(period, assetInstanceId, now);
    }

    private async Task RunDepreciationForPeriod(DateOnly period, int? assetInstanceId, DateTime runAtLocal)
    {
        var assetsQuery = _context.AssetInstances
            .Include(a => a.DepreciationPolicy)
            .Include(a => a.AssetCapitalizations)
            .Where(a =>
                a.DepreciationPolicyId != null &&
                a.InUseDate != null &&
                a.Status != (int)AssetStatus.Disposed &&
                a.Status != (int)AssetStatus.Liquidated &&
                a.Status != (int)AssetStatus.Lost);
        if (assetInstanceId.HasValue)
            assetsQuery = assetsQuery.Where(x => x.AssetInstanceId == assetInstanceId.Value);

        var assets = await assetsQuery.ToListAsync();

        foreach (var instance in assets)
        {
            await ProcessInstance(instance, period, runAtLocal);
        }

        await _context.SaveChangesAsync();
    }

    private async Task ProcessInstance(AssetInstance assetInstance, DateOnly period, DateTime runAtLocal)
    {
        // Chỉ khấu hao tài sản đã được capitalize.
        if (assetInstance.AssetCapitalizations == null || !assetInstance.AssetCapitalizations.Any())
            return;

        if (assetInstance.InUseDate == null)
            return;

        var inUseDate = assetInstance.InUseDate.Value;
        if (period < new DateOnly(inUseDate.Year, inUseDate.Month, 1))
            return;

        // Idempotent theo kỳ.
        var exists = await _context.DepreciationRecords
            .AnyAsync(x => x.AssetInstanceId == assetInstance.AssetInstanceId && x.Period == period);
        if (exists)
            return;

        var last = await _context.DepreciationRecords
            .Where(x => x.AssetInstanceId == assetInstance.AssetInstanceId)
            .OrderByDescending(x => x.Period)
            .ThenByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync();

        var policy = assetInstance.DepreciationPolicy;
        if (policy == null || !policy.IsActive || policy.UsefullLifeMonths <= 0)
            return;

        // Tính mức khấu hao tháng và chặn kỳ cuối không thấp hơn giá trị thu hồi.
        var carryingAtPeriodStart = await ResolveCarryingValueAtPeriodStart(assetInstance, period, last);
        var accumulated = last?.AccumulatedDepreciation ?? 0m;
        var remainingMonths = Math.Max(1, policy.UsefullLifeMonths - GetElapsedMonths(period, inUseDate));

        var monthly = DepreciationFormula.CalculateStraightLine(
            carryingAtPeriodStart,
            policy.SalvageValue,
            remainingMonths);

        var amount = DepreciationFormula.ClampFinalPeriodAmount(
            carryingAtPeriodStart,
            policy.SalvageValue,
            monthly);
        if (amount <= 0)
            return;

        var newAccumulated = accumulated + amount;
        var remaining = carryingAtPeriodStart - amount;

        _context.DepreciationRecords.Add(new DepreciationRecord
        {
            AssetInstanceId = assetInstance.AssetInstanceId,
            PolicyId = policy.PolicyId,
            Period = period,
            DepreciationAmount = amount,
            OriginalValue = carryingAtPeriodStart,
            AccumulatedDepreciation = newAccumulated,
            RemainingValue = remaining,
            CreateDate = runAtLocal
        });

        assetInstance.CurrentValue = remaining;
    }

    public async Task AssignPolicyAsync(int assetInstanceId, int policyId)
    {
        // Gán chính sách khấu hao đang active cho asset instance.
        var instance = await _context.AssetInstances
            .FirstOrDefaultAsync(x => x.AssetInstanceId == assetInstanceId)
            ?? throw new Exception("Asset instance not found");

        if (instance.DepreciationPolicyId.HasValue)
        {
            if (instance.DepreciationPolicyId.Value == policyId)
                return;
            throw new Exception("Depreciation policy is already assigned and cannot be changed");
        }

        var policy = await _context.DepreciationPolicies
            .FirstOrDefaultAsync(x => x.PolicyId == policyId && x.IsActive);
        if (policy == null)
            throw new Exception("Depreciation policy not found or inactive");

        instance.DepreciationPolicyId = policy.PolicyId;
        await _context.SaveChangesAsync();
    }

    private async Task<decimal> ResolveCarryingValueAtPeriodStart(
        AssetInstance assetInstance,
        DateOnly period,
        DepreciationRecord? lastRecord)
    {
        // Giá trị đầu kỳ lấy từ kỳ trước, ưu tiên giá đánh giá lại gần nhất còn hiệu lực.
        var opening = lastRecord?.RemainingValue ?? assetInstance.OriginalPrice;
        var periodStart = period.ToDateTime(TimeOnly.MinValue);

        var periodEnd = period.AddMonths(1).ToDateTime(TimeOnly.MinValue);
        var latestRevaluation = await _context.AssetRevaluations
            .Where(x =>
                x.AssetInstanceId == assetInstance.AssetInstanceId &&
                x.EffectiveDate >= periodStart &&
                x.EffectiveDate < periodEnd)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync();

        if (latestRevaluation != null)
            opening = latestRevaluation.NewValue;

        return opening;
    }

    private static int GetElapsedMonths(DateOnly period, DateOnly inUseDate)
    {
        // Tính số tháng đã đi qua từ tháng bắt đầu sử dụng đến kỳ cần tính.
        var monthDiff = (period.Year - inUseDate.Year) * 12 + (period.Month - inUseDate.Month);
        return Math.Max(monthDiff, 0);
    }

    private async Task<decimal> RecalculateAccumulatedUntil(
        int assetInstanceId,
        DateOnly period,
        int currentRecordId,
        decimal currentAmount)
    {
        // Khấu hao lũy kế kỳ hiện tại = lũy kế kỳ trước + mức khấu hao kỳ này.
        var previous = await _context.DepreciationRecords
            .Where(x =>
                x.AssetInstanceId == assetInstanceId &&
                x.Period < period &&
                x.RecordId != currentRecordId)
            .OrderByDescending(x => x.Period)
            .ThenByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync();

        return (previous?.AccumulatedDepreciation ?? 0m) + currentAmount;
    }

    private async Task RecalculateNextPeriods(int assetInstanceId, DateOnly fromPeriod, decimal accumulatedAtFromPeriod)
    {
        // Lan truyền lại giá trị lũy kế/còn lại cho các kỳ sau còn được chỉnh sửa.
        var fromRecord = await _context.DepreciationRecords
            .Where(x => x.AssetInstanceId == assetInstanceId && x.Period == fromPeriod)
            .OrderByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync()
            ?? throw new Exception("Source depreciation period not found");

        var nextRecords = await _context.DepreciationRecords
            .Where(x => x.AssetInstanceId == assetInstanceId && x.Period > fromPeriod)
            .OrderBy(x => x.Period)
            .ThenBy(x => x.CreateDate)
            .ToListAsync();

        var rollingAccumulated = accumulatedAtFromPeriod;
        var openingValue = fromRecord.RemainingValue;
        foreach (var item in nextRecords)
        {
            if (item.IsPosted || item.IsLocked == true)
                throw new Exception("Cannot recalculate because later periods are posted/locked");

            var periodStart = item.Period.ToDateTime(TimeOnly.MinValue);
            var periodEnd = item.Period.AddMonths(1).ToDateTime(TimeOnly.MinValue);
            var periodRevaluation = await _context.AssetRevaluations
                .Where(x =>
                    x.AssetInstanceId == assetInstanceId &&
                    x.EffectiveDate >= periodStart &&
                    x.EffectiveDate < periodEnd)
                .OrderByDescending(x => x.EffectiveDate)
                .FirstOrDefaultAsync();
            if (periodRevaluation != null)
                openingValue = periodRevaluation.NewValue;

            var salvage = await _context.DepreciationPolicies
                .Where(p => p.PolicyId == item.PolicyId)
                .Select(p => (decimal?)p.SalvageValue)
                .FirstOrDefaultAsync() ?? 0m;
            var adjustedAmount = DepreciationFormula.ClampFinalPeriodAmount(
                openingValue,
                salvage,
                item.DepreciationAmount);

            item.OriginalValue = openingValue;
            item.DepreciationAmount = adjustedAmount;
            rollingAccumulated += item.DepreciationAmount;
            item.AccumulatedDepreciation = rollingAccumulated;
            item.RemainingValue = openingValue - item.DepreciationAmount;
            openingValue = item.RemainingValue;
        }

        var instance = await _context.AssetInstances
            .FirstOrDefaultAsync(x => x.AssetInstanceId == assetInstanceId);
        if (instance != null)
            instance.CurrentValue = openingValue;
    }
}
