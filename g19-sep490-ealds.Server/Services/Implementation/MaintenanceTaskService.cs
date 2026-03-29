using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTask;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
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
        .Include(x => x.Asset)
        .FirstOrDefaultAsync(x => x.TaskId == taskId)
        ?? throw new Exception("Task not found");

        if (task.StatusEnum != MaintenanceTaskStatus.Pending)
            throw new Exception("Task must be Pending");

        if (task.Asset == null)
            throw new Exception("Asset not found");

        if (task.AssignTo != userId)
            throw new Exception("You are not assigned to this task");
        // update task
        task.StatusEnum = MaintenanceTaskStatus.InProgress;

        // update asset
        var asset = task.Asset;
        var oldStatus = asset.Status;

        asset.Status = (int)AssetStatus.UnderMaintenance;

        await _context.SaveChangesAsync();

        // publish event
        await _mediator.Publish(new AssetStatusChangedEvent(
            asset.AssetId,
            oldStatus,
            asset.Status,
            userId,
            roleId
        ));
    }

    public async Task CompleteTaskAsync(int taskId, int userId, int roleId, CompleteTaskDTO dto)
    {
        var task = await _context.MaintenanceTasks
        .Include(x => x.Asset)
        .FirstOrDefaultAsync(x => x.TaskId == taskId)
        ?? throw new Exception("Task not found");

        if (task.StatusEnum != MaintenanceTaskStatus.InProgress)
            throw new Exception("Task must be InProgress");

        if (task.AssignTo != userId)
            throw new Exception("You are not assigned to this task");

        // update task
        task.StatusEnum = MaintenanceTaskStatus.Completed;

        var asset = task.Asset;
        var oldStatus = asset.Status;

        asset.Status = (int)AssetStatus.Active;

        await _context.SaveChangesAsync();

        // publish event k�m data user nh?p
        await _mediator.Publish(
            new MaintenanceTaskCompletedEvent(
                task.TaskId,
                task.AssetId,
                userId,
                dto
            )
        );

        await _mediator.Publish(new AssetStatusChangedEvent(
            asset.AssetId,
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
