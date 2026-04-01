using g19_sep490_ealds.Server.Services.Interface;
using Quartz;

namespace g19_sep490_ealds.Server.Utils;

public class DepreciationJobs : IJob
{
    private readonly IServiceProvider _provider;
    private ILogger<DepreciationJobs> _logger;

    public DepreciationJobs(IServiceProvider provider, ILogger<DepreciationJobs> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            using var scope = _provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IAssetDepreciationService>();

            await service.RunMonthlyDepreciation();

            _logger.LogInformation($"[CRON] Depreciation job run at {DateTime.Now}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CRON ERROR] Depreciation job failed");
        }
    }
}