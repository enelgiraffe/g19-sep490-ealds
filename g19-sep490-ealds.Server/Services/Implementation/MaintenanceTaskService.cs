using g19_sep490_ealds.Server.Events.Command;
using g19_sep490_ealds.Server.Events.Event;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class MaintenanceTaskService : IMaintenanceTaskService
{
    private readonly EaldsDbContext _context;
    private readonly IMediator _mediator;

    public MaintenanceTaskService(EaldsDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task StartTaskAsync(int taskId, int userId, int roleId)
    {
        var task = await _context.MaintenanceTasks
            .Include(x => x.AssetInstance)
            .FirstOrDefaultAsync(x => x.TaskId == taskId)
            ?? throw new Exception("Task not found");

        if (task.Status != (int)MaintenanceTaskStatus.Pending)
            throw new Exception("Task must be Pending");

        if (task.AssetInstance == null)
            throw new Exception("Asset instance not found");

        if (task.AssignTo != userId)
            throw new Exception("You are not assigned to this task");

        task.Status = (int)MaintenanceTaskStatus.InProgress;

        var assetInstance = task.AssetInstance;
        var oldStatus = assetInstance.Status;

        assetInstance.Status = (int)AssetStatus.UnderMaintenance;

        await _context.SaveChangesAsync();

        await _mediator.Publish(new AssetStatusChangedEvent(
            assetInstance.AssetInstanceId,
            oldStatus,
            assetInstance.Status,
            userId,
            roleId
        ));
    }

    public async Task CompleteTaskAsync(int taskId, int userId, int roleId, CompleteTaskDTO dto)
    {
        var task = await _context.MaintenanceTasks
            .Include(x => x.AssetInstance)
            .FirstOrDefaultAsync(x => x.TaskId == taskId)
            ?? throw new Exception("Task not found");

        if (task.Status != (int)MaintenanceTaskStatus.InProgress)
            throw new Exception("Task must be InProgress");

        if (task.AssignTo != userId)
            throw new Exception("You are not assigned to this task");

        task.Status = (int)MaintenanceTaskStatus.Completed;

        var asset = task.AssetInstance;
        var oldStatus = asset.Status;

        asset.Status = (int)AssetStatus.Active;

        await _context.SaveChangesAsync();

        await _mediator.Publish(
            new MaintenanceTaskCompletedEvent(
                task.TaskId,
                task.AssetInstanceId,
                userId,
                dto
            )
        );

        await _mediator.Publish(new AssetStatusChangedEvent(
            asset.AssetInstanceId,
            oldStatus,
            asset.Status,
            userId,
            roleId
        ));
    }

    public Task<IEnumerable<MaintenanceTaskResponseDTO>> GetAllTemplatesAsync()
    {
        throw new NotImplementedException();
    }
}
