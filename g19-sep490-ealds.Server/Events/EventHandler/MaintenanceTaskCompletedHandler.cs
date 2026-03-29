using g19_sep490_ealds.Server.Events.Event;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;

namespace g19_sep490_ealds.Server.Events.EventHandler;

public class MaintenanceTaskCompletedHandler : INotificationHandler<MaintenanceTaskCompletedEvent>
{
    private readonly EALDSDbcontext _context;

    public MaintenanceTaskCompletedHandler(EALDSDbcontext context)
    {
        _context = context;
    }

    public async Task Handle(MaintenanceTaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        var data = notification.Data;

        var record = new MaintenanceRecord
        {
            TaskId = notification.TaskId,
            AssetInstanceId = notification.AssetInstanceId,
            ExecutionDate = DateTime.UtcNow,
            TotalCost = data.TotalCost,
            StatusEnum = MaintenanceRecordStatus.Completed,

            WorkPerformed = data.WorkPerformed,
            ConditionBefore = data.ConditionBefore,
            ConditionAfter = data.ConditionAfter,
            //TechnicalNote = data.TechnicalNote
        };

        _context.MaintenanceRecords.Add(record);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
