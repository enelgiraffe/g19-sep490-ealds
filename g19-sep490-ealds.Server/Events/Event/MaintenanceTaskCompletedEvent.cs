using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTask;
using MediatR;

namespace g19_sep490_ealds.Server.Events.Event;

public class MaintenanceTaskCompletedEvent : INotification
{
    public int TaskId { get; }
    public int AssetId { get; }
    public int UserId { get; }
    public CompleteTaskDTO Data { get; }

    public MaintenanceTaskCompletedEvent(int taskId, int assetId, int userId, CompleteTaskDTO data)
    {
        TaskId = taskId;
        AssetId = assetId;
        UserId = userId;
        Data = data;
    }
}