using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace g19_sep490_ealds.Server.Utils;

public class MaintenanceTaskJobs : IJob
{
    private readonly IMaintenanceScheduleService _service;
    private readonly ILogger<MaintenanceTaskJobs> _logger;
    private readonly IServiceScopeFactory _scope;

    public MaintenanceTaskJobs(
        IMaintenanceScheduleService service,
        ILogger<MaintenanceTaskJobs> logger,
        IServiceScopeFactory scope)
    {
        _service = service;
        _logger = logger;
        _scope = scope;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Maintenance Task Job started at {time}", DateTime.Now);

        try
        {
            await GenerateTasks();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while generating maintenance tasks");
        }

        _logger.LogInformation("Maintenance Task Job finished at {time}", DateTime.Now);
    }

    private async Task GenerateTasks()
    {
        using var scope = _scope.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EALDSDbcontext>();

        var schedules = await db.MaintenanceSchedules.Where(x => x.IsActive == true
                                                                 && x.NextDueDate != null
                                                                 && x.NextDueDate <= DateTime.UtcNow.AddHours(7)).ToListAsync();

        foreach (var schedule in schedules)
        {
            // tránh tạo task trùng
            var exist = await db.MaintenanceTasks.AnyAsync(x => x.ScheduleId == schedule.ScheduleId
                                                               && x.PlannedDate == schedule.NextDueDate.Value.Date
                                                               && x.Status != (int)MaintenanceTaskStatus.Completed);

            if (exist)
                continue;

            var task = new MaintenanceTask
            {
                ScheduleId = schedule.ScheduleId,
                AssetInstanceId = schedule.AssetInstanceId,
                PlannedDate = schedule.NextDueDate.Value,
                AssignTo = schedule.CreateBy, // tạm assign người tạo
                StatusEnum = MaintenanceTaskStatus.Pending,
                CreatDate = DateTime.Now,
                CreateBy = schedule.CreateBy
            };

            db.MaintenanceTasks.Add(task);

            schedule.NextDueDate = _service.CalculateNextDueDate(schedule);
        }

        await db.SaveChangesAsync();
    }
}
