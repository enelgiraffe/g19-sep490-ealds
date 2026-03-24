using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceSchedule;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Mappers;

public interface IMaintenanceRecordMapper
{
    // Entity(DTO) => Response
    MaintenanceRecordResponseDTO EntityToResponse(MaintenanceRecord entity);
    IEnumerable<MaintenanceRecordResponseDTO> ListEntityToResponse(IEnumerable<MaintenanceRecord> entities);
}