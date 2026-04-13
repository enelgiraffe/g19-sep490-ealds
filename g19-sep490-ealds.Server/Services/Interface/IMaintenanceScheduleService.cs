using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceSchedule;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;
//using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IMaintenanceScheduleService
{
    Task<MaintenanceScheduleResponseDTO> CreateScheduleAsync(ScheduleCreateDTO create);
    Task<IEnumerable<MaintenanceScheduleResponseDTO>> GetScheduleByAssetAsync(int assetId);
    Task<IEnumerable<MaintenanceScheduleResponseDTO>> GetScheduleByInstanceAsync(int assetInstanceId);
    Task<bool> ToggleScheduleAsync(int scheduleId);
    DateTime CalculateNextDueDate(MaintenanceSchedule schedule);
}