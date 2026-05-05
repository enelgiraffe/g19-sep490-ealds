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
            // Lấy thời gian LẼ RA Job phải chạy (ScheduledFireTimeUtc) để phòng trường hợp Misfire
            // Ví dụ: Server sập lúc 23:59 ngày 31/01, boot lại lúc 00:05 ngày 01/02.
            // Nếu dùng UtcNow thì sẽ bị tính nhầm sang tháng 2 và bỏ sót tháng 1.
            var scheduledTime = context.ScheduledFireTimeUtc?.UtcDateTime;
            await _service.RunMonthlyDepreciation(scheduledTime);

            _logger.LogInformation("[CRON] Depreciation job run for scheduled time {ScheduledTime} at actual time {ActualTime}", 
                scheduledTime, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CRON ERROR] Depreciation job failed");
        }
    }
}