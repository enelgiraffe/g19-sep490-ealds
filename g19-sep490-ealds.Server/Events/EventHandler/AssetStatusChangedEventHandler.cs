using g19_sep490_ealds.Server.Events.Command;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;

namespace g19_sep490_ealds.Server.Events.EventHandler;

public class AssetStatusChangedEventHandler : INotificationHandler<AssetStatusChangedEvent>
{
    private readonly EaldsDbContext _context;

    public AssetStatusChangedEventHandler(EaldsDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AssetStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var oldLabel = GetAssetStatusViLabel((AssetStatus)notification.OldStatus);
        var newLabel = GetAssetStatusViLabel((AssetStatus)notification.NewStatus);

        var log = new AssetLifeCycle
        {
            AssetInstanceId = notification.AssetInstanceId,
            ActionType = (int)AssetLifeActionType.StatusChanged,
            RelatedEntityType = 2,
            RelatedEntityId = notification.AssetInstanceId,
            ActorUserId = notification.ActorUserId,
            ActorRoleId = notification.ActorRoleId,
            Description = $"{oldLabel} → {newLabel}",
            OccurredAt = DateTime.UtcNow
        };

        _context.AssetLifeCycles.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string GetAssetStatusViLabel(AssetStatus status) => status switch
    {
        AssetStatus.Available => "Sẵn sàng sử dụng",
        AssetStatus.InUse => "Đang sử dụng",
        AssetStatus.InMaintenance => "Đang bảo trì / bảo dưỡng",
        AssetStatus.Disposed => "Đã thanh lý",
        AssetStatus.Lost => "Bị mất",
        AssetStatus.Liquidated => "Đã thanh lý (bán)",
        AssetStatus.Capitalized => "Đã vốn hóa",
        AssetStatus.Damaged => "Hư hỏng",
        AssetStatus.InRepair => "Đang sửa chữa",
        _ => status.ToString()
    };
}
