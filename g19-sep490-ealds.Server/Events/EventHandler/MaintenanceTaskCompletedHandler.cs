using g19_sep490_ealds.Server.Events.Event;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;

namespace g19_sep490_ealds.Server.Events.EventHandler;

public class MaintenanceTaskCompletedHandler : INotificationHandler<MaintenanceTaskCompletedEvent>
{  
    private readonly EaldsDbContext _context;

    public MaintenanceTaskCompletedHandler(EaldsDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MaintenanceTaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        var data = notification.Data;

        var workPerformed = data.WorkPerformed;
        if (!string.IsNullOrWhiteSpace(data.TechnicalNote))
            workPerformed = $"{workPerformed}\n\n{data.TechnicalNote}";

        var record = new MaintenanceRecord
        {
            TaskId = notification.TaskId,
            AssetInstanceId = notification.AssetInstanceId,
            ExecutionDate = DateTime.UtcNow,
            TotalCost = data.TotalCost,
            Status = (int)MaintenanceRecordStatus.Completed,
            WorkPerformed = workPerformed,
            ConditionBefore = data.ConditionBefore,
            ConditionAfter = data.ConditionAfter,
        };

        _context.MaintenanceRecords.Add(record);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
