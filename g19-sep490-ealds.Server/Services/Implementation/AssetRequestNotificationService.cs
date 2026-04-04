using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetRequestNotificationService : IAssetRequestNotificationService
{
    private const int MaxTitleLength = 255;
    private const int RoleIdAccountant = 3;

    private readonly EaldsDbContext _db;
    private readonly ILogger<AssetRequestNotificationService> _logger;
    private readonly int _allocationRequestTypeId;
    private readonly int _handoverRequestTypeId;
    private readonly int _repairRequestTypeId;

    public AssetRequestNotificationService(
        EaldsDbContext db,
        ILogger<AssetRequestNotificationService> logger,
        IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _allocationRequestTypeId = configuration.GetValue<int>("App:AllocationRequestTypeId", 6);
        _handoverRequestTypeId = configuration.GetValue<int>("App:HandoverRequestTypeId", 7);
        _repairRequestTypeId = configuration.GetValue<int>("App:RepairRequestTypeId", 4);
    }

    public async Task NotifyFirstApproversAsync(int assetRequestId, CancellationToken cancellationToken = default)
    {
        var ar = await _db.AssetRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId, cancellationToken);
        if (ar == null)
            return;

        var requestType = await _db.RequestTypes.AsNoTracking()
            .Include(rt => rt.Workflow)
            .FirstOrDefaultAsync(rt => rt.RequestTypeId == ar.RequestTypeId, cancellationToken);

        // Cấp phát / thu hồi: bước xử lý đầu là kế toán.
        // Sửa chữa: ưu tiên thông báo Giám đốc (không phụ thuộc RoleId bước 1 workflow — thường là kế toán).
        List<int> recipientIds;
        if (IsAllocationOrHandoverRequestType(ar.RequestTypeId))
            recipientIds = await UserIdsForRolesAsync(await AccountantRoleIdsAsync(cancellationToken), cancellationToken);
        else if (ar.RequestTypeId == _repairRequestTypeId)
        {
            recipientIds = await DirectorUserIdsAsync(cancellationToken);
            if (recipientIds.Count == 0)
            {
                _logger.LogWarning(
                    "No users with DIRECTOR role for repair request AssetRequestId={Id}; using workflow first step / fallback.",
                    assetRequestId);
                recipientIds = await ResolveFirstStepApproverUserIdsAsync(requestType?.WorkflowId, cancellationToken);
            }
        }
        else
            recipientIds = await ResolveFirstStepApproverUserIdsAsync(requestType?.WorkflowId, cancellationToken);

        recipientIds = await FilterExistingUserIdsAsync(recipientIds, cancellationToken);
        recipientIds = recipientIds.Where(id => id != ar.CreatedBy).Distinct().ToList();

        if (recipientIds.Count == 0)
        {
            _logger.LogWarning(
                "No approver users to notify for new request AssetRequestId={Id} (type {TypeId}).",
                assetRequestId, ar.RequestTypeId);
            return;
        }

        if (IsAllocationOrHandoverRequestType(ar.RequestTypeId))
        {
            _logger.LogInformation(
                "Notifying accountants for allocation/handover request AssetRequestId={Id} RequestTypeId={TypeId} (recipients={Count}).",
                assetRequestId, ar.RequestTypeId, recipientIds.Count);
        }

        var typeLabel = GetRequestTypeDisplayLabel(ar.RequestTypeId, requestType?.Workflow?.Name);
        var requestTitle = await BuildNotificationRequestTitleAsync(ar, cancellationToken);
        var title = TruncateTitle($"{typeLabel}: {requestTitle}");
        var content = TruncateContent(
            $"Người gửi: #{ar.CreatedBy}.");

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

    public async Task NotifySenderDecisionAsync(int assetRequestId, bool approved, int decidedByUserId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var ar = await _db.AssetRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId, cancellationToken);
        if (ar == null)
            return;

        var senderId = ar.CreatedBy;
        if (senderId <= 0 || !await _db.Users.AnyAsync(u => u.UserId == senderId, cancellationToken))
            return;

        var requestType = await _db.RequestTypes.AsNoTracking()
            .Include(rt => rt.Workflow)
            .FirstOrDefaultAsync(rt => rt.RequestTypeId == ar.RequestTypeId, cancellationToken);

        var typeLabel = GetRequestTypeDisplayLabel(ar.RequestTypeId, requestType?.Workflow?.Name);
        var requestTitle = await BuildNotificationRequestTitleAsync(ar, cancellationToken);
        var actorLabel = await GetActorShortLabelAsync(decidedByUserId, cancellationToken);
        var title = TruncateTitle(
            $"{typeLabel}: {requestTitle} (YC #{ar.AssetRequestId}) — {(approved ? "Đã duyệt" : "Từ chối")}");
        var approveVerb = actorLabel == "Giám đốc" ? "phê duyệt" : "đồng ý";
        var defaultBody = string.IsNullOrEmpty(actorLabel)
            ? (approved ? "Đã xử lý đồng ý." : "Yêu cầu bị từ chối.")
            : (approved
                ? $"{actorLabel} {approveVerb}."
                : $"{actorLabel} từ chối.");
        var note = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        var bodyPart = note ?? defaultBody;
        // Giữ "Loại #…" ở đầu để client suy ra tab / màn hình đúng (repair → /repairs).
        var content = TruncateContent($"{bodyPart}");

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

    private async Task<List<int>> DirectorUserIdsAsync(CancellationToken cancellationToken)
    {
        var roleIds = await _db.Roles.AsNoTracking()
            .Where(r => r.Code != null && r.Code.Trim().ToUpper() == "DIRECTOR")
            .Select(r => r.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return new List<int>();

        return await UserIdsForRolesAsync(roleIds, cancellationToken);
    }

    private async Task<string> BuildNotificationRequestTitleAsync(AssetRequest ar, CancellationToken cancellationToken)
    {
        var baseTitle = string.IsNullOrWhiteSpace(ar.Title) ? "Không tiêu đề" : ar.Title.Trim();

        if (ar.RequestTypeId != _repairRequestTypeId || !ar.AssetInstanceId.HasValue || ar.AssetInstanceId.Value <= 0)
            return baseTitle;

        var instanceCode = await _db.AssetInstances.AsNoTracking()
            .Where(i => i.AssetInstanceId == ar.AssetInstanceId.Value)
            .Select(i => i.InstanceCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(instanceCode))
            return baseTitle;

        if (baseTitle.StartsWith("Repair request for instance", StringComparison.OrdinalIgnoreCase))
            return $"Cá thể {instanceCode}";

        return $"{baseTitle} ({instanceCode})";
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

    private bool IsAllocationOrHandoverRequestType(int requestTypeId) =>
        requestTypeId == _allocationRequestTypeId || requestTypeId == _handoverRequestTypeId;

    /// <summary>Labels aligned with client request tabs (RequestTypeId 1–7); unknown ids use workflow name from DB.</summary>
    private static string GetRequestTypeDisplayLabel(int requestTypeId, string? workflowNameFallback) =>
        requestTypeId switch
        {
            1 => "Yêu cầu mua",
            2 => "Yêu cầu bảo dưỡng",
            3 => "Yêu cầu điều chuyển",
            4 => "Yêu cầu sửa chữa",
            5 => "Yêu cầu thanh lý",
            6 => "Yêu cầu cấp phát",
            7 => "Yêu cầu thu hồi",
            _ => string.IsNullOrWhiteSpace(workflowNameFallback)
                ? $"Yêu cầu (loại {requestTypeId})"
                : workflowNameFallback.Trim()
        };

    private static string TruncateTitle(string title) =>
        title.Length > MaxTitleLength ? title[..MaxTitleLength] : title;

    private static string? TruncateContent(string content) =>
        content.Length > 100 ? content[..97] + "..." : content;
}
