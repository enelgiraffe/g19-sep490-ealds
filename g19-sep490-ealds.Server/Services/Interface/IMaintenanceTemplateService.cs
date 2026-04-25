
namespace g19_sep490_ealds.Server.Services.Interface;

public interface IMaintenanceTemplateService
{
    Task<IEnumerable<MaintenanceTemplateResponseDTO>> GetAllTemplatesAsync();
    Task<IEnumerable<MaintenanceTemplateResponseDTO>> SearchTemplateByKeyAsync(string name);
    Task<MaintenanceTemplateResponseDTO> UpdatTemplateAsync(int id, TemplateUpdateDTO update);
    Task<MaintenanceTemplateResponseDTO> CreateTemplateAsync(TemplateCreateDTO create, int? actorUserId = null);
    Task<MaintenanceTemplateResponseDTO> ToggleTemplateStatusAsync(int id);
    Task<bool> HardDeleteTemplateAsync(int id);
    Task<MaintenanceTemplateResponseDTO> FindTemplateByIdAsync(int id);

    /// <summary>Gán l?ch b?o du?ng t? các quy d?nh dang ho?t d?ng cho lo?i tài s?n khi t?o cá th? m?i.</summary>
    Task EnsureSchedulesForNewInstanceAsync(int assetInstanceId, int? actorUserId = null);
}
