using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class InventoryNotificationService : IInventoryNotificationService
{
    private const string ScheduledArrivalTitle = "Đến lịch kiểm kê";
    private const int MaxTitleLength = 255;

    /// <summary>
    /// Seeded <c>Role</c> rows (see DB): ADMIN=1, DIRECTOR=2, ACCOUNTANT=3, DEPARTMENT_HEAD=4.
    /// Resolving users by <c>RoleId</c> avoids EF failing to translate custom string helpers in SQL.
    /// </summary>
    private const int RoleIdDirector = 2;
    private const int RoleIdAccountant = 3;
    private const int RoleIdDepartmentHead = 4;

    private readonly EaldsDbContext _db;
    private readonly ILogger<InventoryNotificationService> _logger;

    public InventoryNotificationService(EaldsDbContext db, ILogger<InventoryNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessScheduledCheckArrivalsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sessions = await _db.InventorySessions
            .Include(s => s.Department)
            .Where(s =>
                s.Status == (int)InventorySessionStatus.Scheduled &&
                s.StartDate <= now &&
                s.EndDate >= now)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            var headUserIds = await FilterExistingUserIdsAsync(
                await GetDepartmentHeadUserIdsAsync(session.DepartmentId, cancellationToken),
                cancellationToken);
            if (headUserIds.Count == 0)
            {
                _logger.LogWarning(
                    "Scheduled inventory session {Code} (SessionId={SessionId}) has no department head user to notify.",
                    session.Code, session.SessionId);
                continue;
            }

            foreach (var userId in headUserIds)
            {
                var already = await _db.Notifications.AnyAsync(
                    n => n.RefId == session.SessionId &&
                         n.UserId == userId &&
                         n.Title == ScheduledArrivalTitle,
                    cancellationToken);
                if (already)
                    continue;

                var deptName = session.Department?.Name ?? "?";
                _db.Notifications.Add(new Notification
                {
                    Title = ScheduledArrivalTitle,
                    Content = TruncateContent($"KK {session.Code} ({deptName}). Đã đến ngày kiểm kê trong khung lịch."),
                    RefId = session.SessionId,
                    UserId = userId,
                    SentDate = DateTime.UtcNow,
                    IsSend = true
                });
            }
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyDirectorsSessionAwaitingConfirmationAsync(InventorySession session, CancellationToken cancellationToken = default)
    {
        var directorIds = await FilterExistingUserIdsAsync(await GetDirectorUserIdsAsync(cancellationToken), cancellationToken);
        if (directorIds.Count == 0)
        {
            _logger.LogWarning("No director users found to notify for completed session {Code}.", session.Code);
            return;
        }

        foreach (var userId in directorIds)
        {
            _db.Notifications.Add(new Notification
            {
                Title = TruncateTitle($"Chờ xác nhận kiểm kê: {session.Code}"),
                Content = TruncateContent($"Trưởng phòng đã hoàn thành phiên {session.Code}. Vui lòng xác nhận."),
                RefId = session.SessionId,
                UserId = userId,
                SentDate = DateTime.UtcNow,
                IsSend = true
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyAfterDirectorApprovalAsync(
        InventorySession session,
        bool hasQuantityOrUserMismatch,
        CancellationToken cancellationToken = default)
    {
        var headIds = await FilterExistingUserIdsAsync(
            await GetDepartmentHeadUserIdsAsync(session.DepartmentId, cancellationToken),
            cancellationToken);
        foreach (var userId in headIds)
        {
            _db.Notifications.Add(new Notification
            {
                Title = TruncateTitle($"Đã xác nhận kiểm kê: {session.Code}"),
                Content = TruncateContent(
                    hasQuantityOrUserMismatch
                        ? $"GD đã xác nhận {session.Code}. Có chênh lệch — kế toán xử lý."
                        : $"GD đã xác nhận {session.Code}. Không có chênh lệch SL/người PT."),
                RefId = session.SessionId,
                UserId = userId,
                SentDate = DateTime.UtcNow,
                IsSend = true
            });
        }

        if (hasQuantityOrUserMismatch)
        {
            var accountantIds = await FilterExistingUserIdsAsync(await GetAccountantUserIdsAsync(cancellationToken), cancellationToken);
            foreach (var userId in accountantIds)
            {
                _db.Notifications.Add(new Notification
                {
                    Title = TruncateTitle($"Xử lý chênh lệch kiểm kê: {session.Code}"),
                    Content = TruncateContent($"GD xác nhận {session.Code}. Có chênh lệch — xử lý trên sổ."),
                    RefId = session.SessionId,
                    UserId = userId,
                    SentDate = DateTime.UtcNow,
                    IsSend = true
                });
            }
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyDepartmentHeadsRecheckRequestedAsync(InventorySession session, CancellationToken cancellationToken = default)
    {
        var headIds = await FilterExistingUserIdsAsync(
            await GetDepartmentHeadUserIdsAsync(session.DepartmentId, cancellationToken),
            cancellationToken);
        foreach (var userId in headIds)
        {
            _db.Notifications.Add(new Notification
            {
                Title = TruncateTitle($"Kiểm kê lại: {session.Code}"),
                Content = TruncateContent($"GD yêu cầu kiểm kê lại phiên {session.Code}."),
                RefId = session.SessionId,
                UserId = userId,
                SentDate = DateTime.UtcNow,
                IsSend = true
            });
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Department heads: users with department-head role who belong to <paramref name="departmentId"/> via <see cref="Employee"/>.
    /// </summary>
    private async Task<List<int>> GetDepartmentHeadUserIdsAsync(int departmentId, CancellationToken cancellationToken)
    {
        var headRoleIds = await GetDepartmentHeadRoleIdsAsync(cancellationToken);
        if (headRoleIds.Count == 0)
        {
            _logger.LogWarning("No Role row found for department head (expected id {RoleId} or Code DEPARTMENT_HEAD).", RoleIdDepartmentHead);
            return new List<int>();
        }

        var userIdsWithHeadRole = await _db.UserRoles.AsNoTracking()
            .Where(ur => headRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userIdsWithHeadRole.Count == 0)
            return new List<int>();

        return await _db.Employees.AsNoTracking()
            .Where(e => e.DepartmentId == departmentId && e.UserId != null && userIdsWithHeadRole.Contains(e.UserId.Value))
            .Select(e => e.UserId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>Role ids for &quot;Trưởng ban&quot; / DEPARTMENT_HEAD — translatable SQL.</summary>
    private async Task<List<int>> GetDepartmentHeadRoleIdsAsync(CancellationToken cancellationToken)
    {
        return await _db.Roles.AsNoTracking()
            .Where(r =>
                r.RoleId == RoleIdDepartmentHead ||
                (r.Code != null && (r.Code.ToUpper() == "DEPARTMENT_HEAD" || r.Code.ToUpper() == "DEPARTMENTHEAD")))
            .Select(r => r.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<List<int>> GetDirectorUserIdsAsync(CancellationToken cancellationToken)
    {
        var roleIds = await GetDirectorRoleIdsAsync(cancellationToken);
        if (roleIds.Count == 0)
        {
            _logger.LogWarning("No Role row found for director (expected id {RoleId} or Code DIRECTOR).", RoleIdDirector);
            return new List<int>();
        }

        return await _db.UserRoles.AsNoTracking()
            .Where(ur => roleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<List<int>> GetDirectorRoleIdsAsync(CancellationToken cancellationToken)
    {
        return await _db.Roles.AsNoTracking()
            .Where(r =>
                r.RoleId == RoleIdDirector ||
                (r.Code != null && r.Code.ToUpper() == "DIRECTOR"))
            .Select(r => r.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<List<int>> GetAccountantUserIdsAsync(CancellationToken cancellationToken)
    {
        var roleIds = await GetAccountantRoleIdsAsync(cancellationToken);
        if (roleIds.Count == 0)
        {
            _logger.LogWarning("No Role row found for accountant (expected id {RoleId} or Code ACCOUNTANT).", RoleIdAccountant);
            return new List<int>();
        }

        return await _db.UserRoles.AsNoTracking()
            .Where(ur => roleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<List<int>> GetAccountantRoleIdsAsync(CancellationToken cancellationToken)
    {
        return await _db.Roles.AsNoTracking()
            .Where(r =>
                r.RoleId == RoleIdAccountant ||
                (r.Code != null && r.Code.ToUpper() == "ACCOUNTANT"))
            .Select(r => r.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<List<int>> FilterExistingUserIdsAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new List<int>();

        var distinct = userIds.Distinct().ToList();
        return await _db.Users.AsNoTracking()
            .Where(u => distinct.Contains(u.UserId))
            .Select(u => u.UserId)
            .ToListAsync(cancellationToken);
    }

    private static string TruncateTitle(string title) =>
        title.Length > MaxTitleLength ? title[..MaxTitleLength] : title;

    private static string? TruncateContent(string content) =>
        content.Length > 100 ? content[..97] + "..." : content;
}
