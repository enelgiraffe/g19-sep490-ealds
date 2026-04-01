namespace g19_sep490_ealds.Server.Services.Interface;

/// <summary>
/// In-app notifications for asset request workflow (submit → approvers; decisions → sender).
/// </summary>
public interface IAssetRequestNotificationService
{
    /// <summary>
    /// Notifies users who act on the first workflow step for this request type (first WorkflowStep role).
    /// Falls back to accountants when workflow is missing or has no assignees.
    /// </summary>
    Task NotifyFirstApproversAsync(int assetRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the request creator (CreatedBy) that the request was approved or rejected.
    /// </summary>
    Task NotifySenderDecisionAsync(int assetRequestId, bool approved, int decidedByUserId, CancellationToken cancellationToken = default);
}
