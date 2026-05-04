using g19_sep490_ealds.Server.DTOs.Maintenance;
using g19_sep490_ealds.Server.DTOs.Transfers;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IMaintenanceRequestService
{
    Task<IEnumerable<TransferRequestListItemDTO>> GetListAsync(int userId);
    Task<MaintenanceRequestCreateResultDTO> CreateAsync(MaintenanceRequestDTO dto);
    Task DeleteMaintenanceRequestAsync(int assetRequestId);
    Task<MaintenanceStartResultDTO> StartMaintenanceAsync(int assetRequestId, MaintenanceStartDto dto);
    Task<MaintenanceCompleteResultDTO> CompleteMaintenanceAsync(int taskId, MaintenanceCompleteDto dto);
}
