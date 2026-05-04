using g19_sep490_ealds.Server.Services.Interface;
using Quartz;

namespace g19_sep490_ealds.Server.Utils;

public class DepreciationJobs : IJob
{
    private ILogger<DepreciationJobs> _logger;
    private readonly IAssetDepreciationService _service;

    public DepreciationJobs(ILogger<DepreciationJobs> logger, IAssetDepreciationService service)
    {
        _logger = logger;
        _service = service;
    }

    // Điểm vào Quartz: chạy job khấu hao tự động theo lịch.
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _service.RunMonthlyDepreciation();

            _logger.LogInformation("[CRON] Depreciation job run at {RunAtUtc}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CRON ERROR] Depreciation job failed");
        }
    }
}