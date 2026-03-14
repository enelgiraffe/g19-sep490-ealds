using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IMaintenanceTemplateService
{
    public Task<IEnumerable<MaintenanceTemplateResponseDTO>> GetAllTemplatesAsync();
    public Task<IEnumerable<MaintenanceTemplateResponseDTO>> SearchTemplateByKeyAsync(string name);
    public Task<MaintenanceTemplateResponseDTO> UpdatTemplateAsync(int id, TemplateUpdateDTO update);
    public Task<MaintenanceTemplateResponseDTO> CreateTemplateAsync(TemplateCreateDTO create);
    public Task<MaintenanceTemplateResponseDTO> ToggleTemplateStatusAsync(int id);
    public Task<bool> HardDeleteTemplateAsync(int id);
    public Task<MaintenanceTemplateResponseDTO> FindTemplateByIdAsync(int id);
}