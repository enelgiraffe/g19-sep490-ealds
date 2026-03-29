using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Mappers.Implementation;

public class MaintenanceTemplateMapper : IMaintenanceTemplateMapper
{
    private static string RepeatIntervalUnitToString(MaintenanceRepeatIntervalUnit unit) => unit.ToString();

    public MaintenanceTemplate CreateToEntity(TemplateCreateDTO create)
    {
        MaintenanceTemplate template = new MaintenanceTemplate();
        template.AssetTypeId = create.AssetTypeId;
        template.Name = create.Name;
        template.Content = create.Content;
        template.FrequencyType = (int)create.FrequencyType;
        template.RepeatIntervalValue = create.RepeatIntervalValue;
        template.RepeatIntervalUnit = RepeatIntervalUnitToString(create.RepeatIntervalUnit);
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
        template.FrequencyType = (int)delete.FrequencyType;
        template.RepeatIntervalValue = delete.RepeatIntervalValue;
        template.RepeatIntervalUnit = RepeatIntervalUnitToString(delete.RepeatIntervalUnit);
        template.IsActive = delete.IsActive;
        return template;
    }

    public MaintenanceTemplateResponseDTO EntityToResponse(MaintenanceTemplate entity)
    {
        MaintenanceTemplateResponseDTO response = new MaintenanceTemplateResponseDTO();
        response.TemplateId = entity.TemplateId;
        response.AssetTypeId = entity.AssetTypeId;
        response.Name = entity.Name;
        response.Content = entity.Content;
        response.FrequencyType = (MaintenanceFrequencyType)entity.FrequencyType;
        response.RepeatIntervalValue = entity.RepeatIntervalValue;
        response.RepeatIntervalUnit = Enum.TryParse<MaintenanceRepeatIntervalUnit>(entity.RepeatIntervalUnit, true, out var ru)
            ? ru
            : MaintenanceRepeatIntervalUnit.Month;
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
        template.FrequencyType = (int)update.FrequencyType;
        template.RepeatIntervalValue = update.RepeatIntervalValue;
        template.RepeatIntervalUnit = RepeatIntervalUnitToString(update.RepeatIntervalUnit);
        return template;
    }
}
