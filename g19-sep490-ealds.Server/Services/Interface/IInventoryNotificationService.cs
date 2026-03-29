using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IInventoryNotificationService
{
    /// <summary>
    /// Quartz: sessions scheduled whose start window has begun — notify department heads once each.
    /// </summary>
    Task ProcessScheduledCheckArrivalsAsync(CancellationToken cancellationToken = default);

    Task NotifyDirectorsSessionAwaitingConfirmationAsync(InventorySession session, CancellationToken cancellationToken = default);

    Task NotifyAfterDirectorApprovalAsync(InventorySession session, bool hasQuantityOrUserMismatch, CancellationToken cancellationToken = default);

    Task NotifyDepartmentHeadsRecheckRequestedAsync(InventorySession session, CancellationToken cancellationToken = default);
}
