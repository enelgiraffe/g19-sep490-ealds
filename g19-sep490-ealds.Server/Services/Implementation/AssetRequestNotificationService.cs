using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetRequestNotificationService : IAssetRequestNotificationService
{
    private const int MaxTitleLength = 255;
    private const int RoleIdAccountant = 3;

    private readonly EaldsDbContext _db;
    private readonly ILogger<AssetRequestNotificationService> _logger;

    public AssetRequestNotificationService(EaldsDbContext db, ILogger<AssetRequestNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task NotifyFirstApproversAsync(int assetRequestId, CancellationToken cancellationToken = default)
    {
        var ar = await _db.AssetRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId, cancellationToken);
        if (ar == null)
            return;

        var requestType = await _db.RequestTypes.AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.RequestTypeId == ar.RequestTypeId, cancellationToken);

        var recipientIds = await ResolveFirstStepApproverUserIdsAsync(requestType?.WorkflowId, cancellationToken);
        recipientIds = await FilterExistingUserIdsAsync(recipientIds, cancellationToken);
        recipientIds = recipientIds.Where(id => id != ar.CreatedBy).Distinct().ToList();

        if (recipientIds.Count == 0)
        {
            _logger.LogWarning(
                "No approver users to notify for new request AssetRequestId={Id} (type {TypeId}).",
                assetRequestId, ar.RequestTypeId);
            return;
        }

        var title = TruncateTitle($"YC #{ar.AssetRequestId} cần xử lý");
        var content = TruncateContent($"Loại #{ar.RequestTypeId}. Người gửi: #{ar.CreatedBy}.");

        foreach (var userId in recipientIds)
        {
            _db.Notifications.Add(new Notification
            {
                Title = title,
                Content = content,
                RefId = ar.CreatedBy,
                UserId = userId,
                SentDate = DateTime.UtcNow,
                IsSend = true
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifySenderDecisionAsync(int assetRequestId, bool approved, int decidedByUserId, CancellationToken cancellationToken = default)
    {
        var ar = await _db.AssetRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId, cancellationToken);
        if (ar == null)
            return;

        var senderId = ar.CreatedBy;
        if (senderId <= 0 || !await _db.Users.AnyAsync(u => u.UserId == senderId, cancellationToken))
            return;

        var actorLabel = await GetActorShortLabelAsync(decidedByUserId, cancellationToken);
        var title = TruncateTitle($"YC #{ar.AssetRequestId}: {(approved ? "Đã duyệt" : "Từ chối")}");
        var approveVerb = actorLabel == "Giám đốc" ? "phê duyệt" : "đồng ý";
        // Prefix with Loại # so the client can open /requests on the right tab (same as new-request notifications).
        var body = string.IsNullOrEmpty(actorLabel)
            ? (approved ? $"Đã xử lý đồng ý YC #{ar.AssetRequestId}." : $"YC #{ar.AssetRequestId} bị từ chối.")
            : (approved
                ? $"{actorLabel} {approveVerb} YC #{ar.AssetRequestId}."
                : $"{actorLabel} từ chối YC #{ar.AssetRequestId}.");
        var content = TruncateContent($"Loại #{ar.RequestTypeId}. {body}");

        _db.Notifications.Add(new Notification
        {
            Title = title,
            Content = content,
            RefId = senderId,
            UserId = senderId,
            SentDate = DateTime.UtcNow,
            IsSend = true
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<int>> ResolveFirstStepApproverUserIdsAsync(int? workflowId, CancellationToken cancellationToken)
    {
        if (workflowId is > 0)
        {
            var firstRoleId = await _db.WorkflowSteps.AsNoTracking()
                .Where(s => s.WorkflowId == workflowId.Value)
                .OrderBy(s => s.StepOrder)
                .Select(s => (int?)s.RoleId)
                .FirstOrDefaultAsync(cancellationToken);

            if (firstRoleId is int rid && rid > 0)
            {
                var ids = await UserIdsForRolesAsync(new[] { rid }, cancellationToken);
                if (ids.Count > 0)
                    return ids;

                _logger.LogWarning(
                    "Workflow {WorkflowId} first step RoleId={RoleId} has no users; falling back to accountants.",
                    workflowId, rid);
            }
        }

        return await UserIdsForRolesAsync(await AccountantRoleIdsAsync(cancellationToken), cancellationToken);
    }

    private async Task<List<int>> AccountantRoleIdsAsync(CancellationToken cancellationToken)
    {
        return await _db.Roles.AsNoTracking()
            .Where(r =>
                r.RoleId == RoleIdAccountant ||
                (r.Code != null && r.Code.ToUpper() == "ACCOUNTANT"))
            .Select(r => r.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<List<int>> UserIdsForRolesAsync(IReadOnlyList<int> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
            return new List<int>();

        return await _db.UserRoles.AsNoTracking()
            .Where(ur => roleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
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

    private async Task<string?> GetActorShortLabelAsync(int userId, CancellationToken cancellationToken)
    {
        var code = await (
            from ur in _db.UserRoles.AsNoTracking()
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.RoleId
            where ur.UserId == userId && r.Code != null
            select r.Code
        ).FirstOrDefaultAsync(cancellationToken);

        return code?.Trim().ToUpperInvariant() switch
        {
            "ACCOUNTANT" => "Kế toán",
            "DIRECTOR" => "Giám đốc",
            _ => null
        };
    }

    private static string TruncateTitle(string title) =>
        title.Length > MaxTitleLength ? title[..MaxTitleLength] : title;

    private static string? TruncateContent(string content) =>
        content.Length > 100 ? content[..97] + "..." : content;
}
