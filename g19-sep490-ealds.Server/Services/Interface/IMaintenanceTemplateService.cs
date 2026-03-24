using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IMaintenanceTemplateService
{
    Task<IEnumerable<MaintenanceTemplateResponseDTO>> GetAllTemplatesAsync();
    Task<IEnumerable<MaintenanceTemplateResponseDTO>> SearchTemplateByKeyAsync(string name);
    Task<MaintenanceTemplateResponseDTO> UpdatTemplateAsync(int id, TemplateUpdateDTO update);
    Task<MaintenanceTemplateResponseDTO> CreateTemplateAsync(TemplateCreateDTO create);
    Task<MaintenanceTemplateResponseDTO> ToggleTemplateStatusAsync(int id);
    Task<bool> HardDeleteTemplateAsync(int id);
    Task<MaintenanceTemplateResponseDTO> FindTemplateByIdAsync(int id);
}