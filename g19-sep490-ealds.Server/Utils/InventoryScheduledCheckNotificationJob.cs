using g19_sep490_ealds.Server.Services.Interface;
using Quartz;

namespace g19_sep490_ealds.Server.Utils;

/// <summary>
/// Dispatches notifications when a scheduled inventory check window starts (dept heads).
/// </summary>
public class InventoryScheduledCheckNotificationJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventoryScheduledCheckNotificationJob> _logger;

    public InventoryScheduledCheckNotificationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<InventoryScheduledCheckNotificationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IInventoryNotificationService>();
            await svc.ProcessScheduledCheckArrivalsAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory scheduled-check notification job failed.");
        }
    }
}
