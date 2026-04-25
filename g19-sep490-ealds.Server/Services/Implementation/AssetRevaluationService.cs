using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetRevaluationService : IAssetRevaluationService
{
    private readonly EaldsDbContext _context;
    private readonly IAssetDepreciationService _depreciationService;

    public AssetRevaluationService(
        EaldsDbContext context,
        IAssetDepreciationService depreciationService)
    {
        _context = context;
        _depreciationService = depreciationService;
    }
    public async Task RevaluateAsync(int assetInstanceId, decimal newValue)
    {
        if (newValue < 0)
            throw new Exception("Revaluation value cannot be negative");

        var instance = await _context.AssetInstances.FindAsync(assetInstanceId)
            ?? throw new Exception("Asset instance not found");

        var oldValue = instance.CurrentValue;
        if (oldValue == newValue)
            return;

        instance.CurrentValue = newValue;

        var effectiveAt = DateTime.UtcNow;
        _context.AssetRevaluations.Add(new AssetRevaluation
        {
            AssetInstanceId = assetInstanceId,
            OldValue = oldValue,
            NewValue = newValue,
            EffectiveDate = effectiveAt
        });

        await _context.SaveChangesAsync();

        // BR-28: đánh giá lại làm thay đổi base, cần tính lại từ kỳ hiệu lực.
        var localEffective = effectiveAt.AddHours(7);
        await _depreciationService.RecalculateFromPeriod(
            assetInstanceId,
            localEffective.Year,
            localEffective.Month);
    }
}
