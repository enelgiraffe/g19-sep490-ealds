using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTask;
using MediatR;

namespace g19_sep490_ealds.Server.Events.Event;

public class MaintenanceTaskCompletedEvent : INotification
{
    public int TaskId { get; }
    public int AssetInstanceId { get; }
    public int UserId { get; }
    public CompleteTaskDTO Data { get; }

    public MaintenanceTaskCompletedEvent(int taskId, int assetInstanceId, int userId, CompleteTaskDTO data)
    {
        TaskId = taskId;
        AssetInstanceId = assetInstanceId;
        UserId = userId;
        Data = data;
    }
}