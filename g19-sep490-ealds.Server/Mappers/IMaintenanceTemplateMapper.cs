using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Mappers;

public interface IMaintenanceTemplateMapper
{
    // request => Entity(DTO)

    MaintenanceTemplate CreateToEntity(TemplateCreateDTO create);
    MaintenanceTemplate UpdateToEntity(TemplateUpdateDTO update);
    MaintenanceTemplate DeleteToEntity(TemplateDeleteDTO delete);

    // Entity(DTO) => Response
    MaintenanceTemplateResponseDTO EntityToResponse(MaintenanceTemplate entity);
    IEnumerable<MaintenanceTemplateResponseDTO> ListEntityToResponse(IEnumerable<MaintenanceTemplate> entities);
}
