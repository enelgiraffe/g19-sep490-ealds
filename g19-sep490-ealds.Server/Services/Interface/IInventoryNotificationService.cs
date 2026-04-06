using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IInventoryNotificationService
{
    /// <summary>
    /// Quartz: sessions in the "Đến lịch" window (scheduled, start ≤ now ≤ end) — notify department heads once per UTC day per session until started.
    /// </summary>
    Task ProcessScheduledCheckArrivalsAsync(CancellationToken cancellationToken = default);

    Task NotifyDirectorsSessionAwaitingConfirmationAsync(InventorySession session, CancellationToken cancellationToken = default);

    Task NotifyAfterDirectorApprovalAsync(InventorySession session, bool hasQuantityOrUserMismatch, CancellationToken cancellationToken = default);

    Task NotifyDepartmentHeadsRecheckRequestedAsync(InventorySession session, CancellationToken cancellationToken = default);
}
