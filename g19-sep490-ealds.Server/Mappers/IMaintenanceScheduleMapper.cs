using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Mappers;

public interface IMaintenanceScheduleMapper
{
    // request => Entity(DTO)

    MaintenanceSchedule CreateToEntity(ScheduleCreateDTO create);
    MaintenanceSchedule UpdateToEntity(ScheduleUpdateDTO update);
    MaintenanceSchedule DeleteToEntity(ScheduleDeleteDTO delete);

    // Entity(DTO) => Response
    MaintenanceScheduleResponseDTO EntityToResponse(MaintenanceSchedule entity);
    IEnumerable<MaintenanceScheduleResponseDTO> ListEntityToResponse(IEnumerable<MaintenanceSchedule> entities);
}
