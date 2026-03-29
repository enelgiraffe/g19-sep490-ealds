using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetRevaluationService : IAssetRevaluationService
{
    private readonly EALDSDbcontext _context;

    public AssetRevaluationService(EALDSDbcontext context)
    {
        _context = context;
    }
    public async Task RevaluateAsync(int assetInstanceId, decimal newValue)
    {
        var instance = await _context.AssetInstances.FindAsync(assetInstanceId)
            ?? throw new Exception("Asset instance not found");

        var oldValue = instance.CurrentValue;

        instance.CurrentValue = newValue;

        _context.AssetRevaluations.Add(new AssetRevaluation
        {
            AssetInstanceId = assetInstanceId,
            OldValue = oldValue,
            NewValue = newValue,
            EffectiveDate = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();
    }
}
