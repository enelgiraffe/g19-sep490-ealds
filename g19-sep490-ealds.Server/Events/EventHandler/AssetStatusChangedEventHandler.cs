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
        var log = new AssetLifeCycle
        {
            AssetInstanceId = notification.AssetInstanceId,

            // Vì đây là thay đổi status
            ActionType = (int)AssetLifeActionType.StatusChanged,

            RelatedEntityType = 2, // 2 = AssetInstance
            RelatedEntityId = notification.AssetInstanceId,

            ActorUserId = notification.ActorUserId,
            ActorRoleId = notification.ActorRoleId,

            Description =
                $"Status changed from {(AssetStatus)notification.OldStatus} " +
                $"to {(AssetStatus)notification.NewStatus}",

            OccurredAt = DateTime.UtcNow
        };

        _context.AssetLifeCycles.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
