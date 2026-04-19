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

    public async Task RunMonthlyDepreciation()
    {
        // Chạy một chu kỳ khấu hao cho tháng hiện tại.
        var now = DateTime.UtcNow + VietnamOffset;
        var period = new DateOnly(now.Year, now.Month, 1);
        await RunDepreciationForPeriod(period, null, now);
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

    public async Task RecalculateFromPeriod(int assetInstanceId, int year, int month)
    {
        // BR-28: tính lại khấu hao từ kỳ phát sinh nâng cấp/đánh giá lại.
        if (month is < 1 or > 12)
            throw new Exception("Month must be from 1 to 12");
        if (year < 2000 || year > 3000)
            throw new Exception("Year is out of valid range");

        var fromPeriod = new DateOnly(year, month, 1);
        var now = DateTime.UtcNow + VietnamOffset;
        var currentPeriod = new DateOnly(now.Year, now.Month, 1);
        if (fromPeriod > currentPeriod)
            throw new Exception("Recalculation period cannot be in the future");

        var records = await _context.DepreciationRecords
            .Where(x => x.AssetInstanceId == assetInstanceId && x.Period >= fromPeriod)
            .OrderBy(x => x.Period)
            .ThenBy(x => x.CreateDate)
            .ToListAsync();

        if (records.Any(x => x.IsPosted || x.IsLocked == true))
            throw new Exception("Cannot recalculate posted/locked depreciation records");

        if (records.Count > 0)
            _context.DepreciationRecords.RemoveRange(records);

        var instance = await _context.AssetInstances
            .Include(x => x.DepreciationPolicy)
            .Include(x => x.AssetCapitalizations)
            .FirstOrDefaultAsync(x => x.AssetInstanceId == assetInstanceId)
            ?? throw new Exception("Asset instance not found");

        for (var period = fromPeriod; period <= currentPeriod; period = period.AddMonths(1))
        {
            await ProcessInstance(instance, period, now);
        }

        await _context.SaveChangesAsync();
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

    public async Task UpdateDepreciation(int recordId, decimal newAmount)
    {
        // Cập nhật một kỳ được phép sửa và tính lại các kỳ sau.
        if (newAmount < 0)
            throw new Exception("Depreciation amount cannot be negative");

        var record = await _context.DepreciationRecords
            .FirstOrDefaultAsync(x => x.RecordId == recordId)
            ?? throw new Exception("Record not found");

        if (record.IsPosted)
            throw new Exception("Cannot modify posted record");
        if (record.IsLocked == true)
            throw new Exception("Cannot modify locked record");

        record.DepreciationAmount = newAmount;
        record.RemainingValue = Math.Max(record.OriginalValue - newAmount, 0);
        record.AccumulatedDepreciation = await RecalculateAccumulatedUntil(
            record.AssetInstanceId,
            record.Period,
            record.RecordId,
            newAmount);

        await RecalculateNextPeriods(record.AssetInstanceId, record.Period, record.AccumulatedDepreciation);

        await _context.SaveChangesAsync();
    }

    public async Task AssignPolicyAsync(int assetInstanceId, int policyId)
    {
        // Gán chính sách khấu hao đang active cho asset instance.
        var instance = await _context.AssetInstances
            .FirstOrDefaultAsync(x => x.AssetInstanceId == assetInstanceId)
            ?? throw new Exception("Asset instance not found");

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

        var latestRevaluation = await _context.AssetRevaluations
            .Where(x =>
                x.AssetInstanceId == assetInstance.AssetInstanceId &&
                x.EffectiveDate <= periodStart)
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
        var nextRecords = await _context.DepreciationRecords
            .Where(x => x.AssetInstanceId == assetInstanceId && x.Period > fromPeriod)
            .OrderBy(x => x.Period)
            .ThenBy(x => x.CreateDate)
            .ToListAsync();

        var rollingAccumulated = accumulatedAtFromPeriod;
        foreach (var item in nextRecords)
        {
            if (item.IsPosted || item.IsLocked == true)
                throw new Exception("Cannot recalculate because later periods are posted/locked");

            rollingAccumulated += item.DepreciationAmount;
            item.AccumulatedDepreciation = rollingAccumulated;
            item.RemainingValue = Math.Max(item.OriginalValue - item.DepreciationAmount, 0m);
        }
    }
}