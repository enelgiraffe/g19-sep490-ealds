
namespace g19_sep490_ealds.Server.Services.Interface;

public interface IMaintenanceTaskService
{
    Task StartTaskAsync(int taskId, int userId, int roleId);
    Task CompleteTaskAsync(int taskId, int userId, int roleId, CompleteTaskDTO dto);
    public Task<IEnumerable<MaintenanceTaskResponseDTO>> GetAllTemplatesAsync();
}
