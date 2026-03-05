using MediatR;

namespace g19_sep490_ealds.Server.Events.Command;

public class AssetStatusChangedEvent : INotification
{
    public int AssetId { get; }
    public int OldStatus { get; }
    public int NewStatus { get; }
    public int ActorUserId { get; }
    public int ActorRoleId { get; }

    public AssetStatusChangedEvent(
        int assetId,
        int oldStatus,
        int newStatus,
        int actorUserId,
        int actorRoleId)
    {
        AssetId = assetId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ActorUserId = actorUserId;
        ActorRoleId = actorRoleId;
    }
}
