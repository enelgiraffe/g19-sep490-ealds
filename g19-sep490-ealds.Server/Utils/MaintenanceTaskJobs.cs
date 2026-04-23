using g19_sep490_ealds.Server.Models;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace g19_sep490_ealds.Server.Utils;

public class MaintenanceTaskJobs : IJob
{
    private readonly ILogger<MaintenanceTaskJobs> _logger;
    private readonly IServiceScopeFactory _scope;
    private const int DepartmentHeadRoleId = 4;
    private const int ReminderLeadDays = 1;

    public MaintenanceTaskJobs(
        ILogger<MaintenanceTaskJobs> logger,
        IServiceScopeFactory scope)
    {
        _logger = logger;
        _scope = scope;
    }

    // Điểm vào Quartz: nhắc lịch bảo trì sắp đến hạn cho trưởng ban.
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Maintenance reminder job started at {time}", DateTime.Now);

        try
        {
            await SendRemindersAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending maintenance reminders");
        }

        _logger.LogInformation("Maintenance reminder job finished at {time}", DateTime.Now);
    }

    // Quét lịch còn hiệu lực, gửi nhắc trước hạn cho trưởng ban theo phòng ban hiện tại của tài sản.
    private async Task SendRemindersAsync(CancellationToken ct)
    {
        using var scope = _scope.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EaldsDbContext>();

        var nowLocal = DateTime.UtcNow.AddHours(7);
        var todayLocal = nowLocal.Date;
        var dueSoonEnd = todayLocal.AddDays(ReminderLeadDays);

        var utcDayStart = DateTime.UtcNow.Date;
        var utcDayEnd = utcDayStart.AddDays(1);

        // 1. Tắt các schedule đã hết hạn
        await DeactivateExpiredSchedules(db, nowLocal, ct);

        // 2. Lấy schedule sắp đến hạn
        var schedules = await GetSchedulesDueSoon(db, todayLocal, dueSoonEnd, ct);

        // 3. Lấy role trưởng phòng
        var departmentHeadRoleIds = await GetDepartmentHeadRoleIds(db, ct);
        if (!departmentHeadRoleIds.Any())
        {
            _logger.LogWarning("No department-head role found; skip reminders.");
            return;
        }

        // 4. Cache user tồn tại
        var existingUserSet = (await db.Users.AsNoTracking()
            .Select(u => u.UserId)
            .ToListAsync(ct)).ToHashSet();

        // 5. Loop từng schedule
        foreach (var schedule in schedules)
        {
            var recipients = await GetRecipients(db, schedule, departmentHeadRoleIds, ct);

            if (!recipients.Any())
            {
                _logger.LogWarning("Schedule {ScheduleId} has no recipients.", schedule.ScheduleId);
                continue;
            }

            foreach (var userId in recipients)
            {
                if (!existingUserSet.Contains(userId))
                    continue;

                var alreadySent = await AlreadySentToday(
                    db, userId, schedule.ScheduleId, utcDayStart, utcDayEnd, ct);

                if (alreadySent)
                    continue;

                db.Notifications.Add(CreateNotification(schedule, userId));
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    private async Task DeactivateExpiredSchedules(EaldsDbContext db, DateTime nowLocal, CancellationToken ct)
    {
        var expired = await db.MaintenanceSchedules
            .Where(x => x.IsActive && x.EndDate.HasValue && x.EndDate.Value < nowLocal)
            .ToListAsync(ct);

        foreach (var e in expired)
            e.IsActive = false;

        if (expired.Any())
            await db.SaveChangesAsync(ct);
    }
    private async Task<List<MaintenanceSchedule>> GetSchedulesDueSoon(EaldsDbContext db, DateTime today, DateTime dueSoonEnd, CancellationToken ct)
    {
        return await db.MaintenanceSchedules
            .Include(x => x.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(x => x.Asset)
            .Where(x => x.IsActive
                        && x.NextDueDate.HasValue
                        && x.NextDueDate.Value.Date >= today
                        && x.NextDueDate.Value.Date <= dueSoonEnd)
            .ToListAsync(ct);
    }
    private async Task<List<int>> GetDepartmentHeadRoleIds(EaldsDbContext db, CancellationToken ct)
    {
        return await db.Roles.AsNoTracking()
            .Where(r =>
                r.RoleId == DepartmentHeadRoleId ||
                (r.Code != null && (
                    r.Code.ToUpper() == "DEPARTMENT_HEAD" ||
                    r.Code.ToUpper() == "DEPARTMENTHEAD" ||
                    r.Code.ToUpper() == "DEPT_HEAD" ||
                    r.Code.ToUpper() == "TRUONG_PHONG" ||
                    r.Code.ToUpper() == "TRUONGPHONG")))
            .Select(r => r.RoleId)
            .Distinct()
            .ToListAsync(ct);
    }
    private async Task<List<int>> GetRecipients(EaldsDbContext db, MaintenanceSchedule schedule, List<int> roleIds, CancellationToken ct)
    {
        var departmentIds = await ResolveCurrentDepartmentIdsAsync(db, schedule, ct);

        if (!departmentIds.Any())
            return new List<int>();

        var candidateUserIds = await db.Employees.AsNoTracking()
            .Where(e => e.UserId.HasValue && departmentIds.Contains(e.DepartmentId))
            .Select(e => e.UserId!.Value)
            .Distinct()
            .ToListAsync(ct);

        if (!candidateUserIds.Any())
            return new List<int>();

        return await db.UserRoles.AsNoTracking()
            .Where(ur => candidateUserIds.Contains(ur.UserId)
                      && roleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task<bool> AlreadySentToday(EaldsDbContext db, int userId, int scheduleId, DateTime utcStart, DateTime utcEnd, CancellationToken ct)
    {
        var title = $"Nhắc lịch bảo dưỡng: LS-{scheduleId}";

        return await db.Notifications.AsNoTracking().AnyAsync(n =>
            n.UserId == userId
            && n.Title == title
            && n.SentDate >= utcStart
            && n.SentDate < utcEnd,
            ct);
    }

    private Notification CreateNotification(MaintenanceSchedule schedule, int userId)
    {
        var dueDate = schedule.NextDueDate!.Value;

        var assetName =
            schedule.AssetInstance?.Asset?.Name
            ?? schedule.Asset?.Name
            ?? $"Lịch {schedule.ScheduleId}";

        var title = $"Nhắc lịch bảo dưỡng: LS-{schedule.ScheduleId}";
        var content = $"Tài sản {assetName} sắp đến hạn bảo dưỡng vào ngày {dueDate:dd/MM/yyyy}. Vui lòng tạo đơn bảo dưỡng.";

        return new Notification
        {
            Title = title.Length > 255 ? title[..255] : title,
            Content = content.Length > 100 ? content[..97] + "..." : content,
            UserId = userId,
            SentDate = DateTime.UtcNow,
            IsSend = true
        };
    }

    private static async Task<List<int>> ResolveCurrentDepartmentIdsAsync(
        EaldsDbContext db,
        MaintenanceSchedule schedule,
        CancellationToken cancellationToken)
    {
        if (schedule.AssetInstanceId.HasValue && schedule.AssetInstanceId.Value > 0)
        {
            return await db.AssetLocations.AsNoTracking()
                .Where(al => al.AssetInstanceId == schedule.AssetInstanceId.Value && al.IsCurrent)
                .Select(al => al.DepartmentId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        if (schedule.AssetId.HasValue && schedule.AssetId.Value > 0)
        {
            return await db.AssetLocations.AsNoTracking()
                .Where(al => al.IsCurrent && al.AssetInstance.AssetId == schedule.AssetId.Value)
                .Select(al => al.DepartmentId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        return new List<int>();
    }
}