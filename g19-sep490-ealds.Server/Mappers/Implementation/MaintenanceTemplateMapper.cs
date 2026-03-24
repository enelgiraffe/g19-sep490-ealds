using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;

namespace g19_sep490_ealds.Server.Mappers.Implementation;

public class MaintenanceTemplateMapper : IMaintenanceTemplateMapper
{
    public MaintenanceTemplate CreateToEntity(TemplateCreateDTO create)
    {
        MaintenanceTemplate template = new MaintenanceTemplate();
        template.AssetTypeId = create.AssetTypeId;
        template.Name = create.Name;
        template.Content = create.Content;
        template.FrequencyTypeEnum = create.FrequencyType;
        template.RepeatIntervalValue = create.RepeatIntervalValue;
        template.RepeatIntervalUnitEnum = create.RepeatIntervalUnit;
        template.IsActive = true;
        return template;
    }

    public MaintenanceTemplate DeleteToEntity(TemplateDeleteDTO delete)
    {
        MaintenanceTemplate template = new MaintenanceTemplate();
        template.TemplateId = delete.TemplateId;
        template.AssetTypeId = delete.AssetTypeId;
        template.Name = delete.Name;
        template.Content = delete.Content;
        template.FrequencyTypeEnum = delete.FrequencyType;
        template.RepeatIntervalValue = delete.RepeatIntervalValue;
        template.RepeatIntervalUnitEnum = delete.RepeatIntervalUnit;
        template.IsActive = delete.IsActive;
        return template;
    }

    public MaintenanceTemplateResponseDTO EntityToResponse(MaintenanceTemplate entity)
    {
        MaintenanceTemplateResponseDTO response = new MaintenanceTemplateResponseDTO();
        response.AssetTypeId = entity.AssetTypeId;
        response.Name = entity.Name;
        response.Content = entity.Content;
        response.FrequencyType = entity.FrequencyTypeEnum;
        response.RepeatIntervalValue = entity.RepeatIntervalValue;
        response.RepeatIntervalUnit = entity.RepeatIntervalUnitEnum;
        response.IsActive = entity.IsActive;
        return response;
    }

    public IEnumerable<MaintenanceTemplateResponseDTO> ListEntityToResponse(IEnumerable<MaintenanceTemplate> entities)
    {
        return entities.Select(x => EntityToResponse(x)).ToList();
    }

    public MaintenanceTemplate UpdateToEntity(TemplateUpdateDTO update)
    {
        MaintenanceTemplate template = new MaintenanceTemplate();
        template.AssetTypeId = update.AssetTypeId;
        template.Name = update.Name;
        template.Content = update.Content;
        template.FrequencyTypeEnum = update.FrequencyType;
        template.RepeatIntervalValue = update.RepeatIntervalValue;
        template.RepeatIntervalUnitEnum = update.RepeatIntervalUnit;
        return template;
    }
}