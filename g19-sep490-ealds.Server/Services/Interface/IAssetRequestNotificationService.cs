namespace g19_sep490_ealds.Server.Services.Interface;

/// <summary>
/// In-app notifications for asset request workflow (submit → approvers; decisions → sender).
/// </summary>
public interface IAssetRequestNotificationService
{
    /// <summary>
    /// Notifies users who act on the first workflow step for this request type (first WorkflowStep role).
    /// Falls back to accountants when workflow is missing or has no assignees.
    /// Allocation and handover always notify accountants on submit.
    /// Transfer notifies accountants on submit with a fixed title/body; directors are notified after accountant approval (status → waiting director).
    /// Repair (App:RepairRequestTypeId) notifies users with role code DIRECTOR first, then falls back like other types.
    /// </summary>
    Task NotifyFirstApproversAsync(int assetRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// After accountant approval, notifies users with role DIRECTOR for a transfer request (same title/body as submit notifications).
    /// </summary>
    Task NotifyTransferDirectorsPendingApprovalAsync(int assetRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the request creator (CreatedBy) that the request was approved or rejected.
    /// When <paramref name="comment"/> is non-empty, it is used as the notification body instead of the default summary.
    /// </summary>
    Task NotifySenderDecisionAsync(int assetRequestId, bool approved, int decidedByUserId, string? comment = null, CancellationToken cancellationToken = default);
}
