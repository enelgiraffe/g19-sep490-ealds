using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceSchedule;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Mappers.Implementation;

public class MaintenanceScheduleMapper : IMaintenanceScheduleMapper
{
    internal static void ApplyRepeatIntervalToEntity(
        MaintenanceSchedule schedule,
        int? intervalValue,
        MaintenanceRepeatIntervalUnit? intervalUnit)
    {
        schedule.IntervalValue = null;
        schedule.IntervalUnit = null;
        if (!intervalUnit.HasValue || !intervalValue.HasValue || intervalValue.Value <= 0)
            return;
        schedule.IntervalValue = intervalValue.Value;
        schedule.IntervalUnit = (int)intervalUnit.Value;
    }

    internal static (int? Value, MaintenanceRepeatIntervalUnit? Unit) ReadRepeatIntervalFromEntity(MaintenanceSchedule entity)
    {
        if (entity.IntervalValue is int v && v > 0 && entity.IntervalUnit is int u)
            return (v, (MaintenanceRepeatIntervalUnit)u);
        return (null, null);
    }

    public MaintenanceSchedule CreateToEntity(ScheduleCreateDTO create)
    {
        MaintenanceSchedule schedule = new MaintenanceSchedule();
        schedule.AssetId = create.AssetId;
        schedule.TemplateId = create.TemplateId ?? 0;
        schedule.Content = string.IsNullOrWhiteSpace(create.Content) ? null : create.Content.Trim();
        schedule.ScheduleType = (int)create.ScheduleType;
        ApplyRepeatIntervalToEntity(schedule, create.IntervalValue, create.IntervalUnit);
        schedule.StartDate = create.StartDate;
        schedule.EndDate = create.EndDate;
        schedule.IsActive = create.IsActive ?? true;
        schedule.CreateBy = create.CreateBy;
        schedule.CreateDate = DateTime.UtcNow.AddHours(7);
        return schedule;
    }

    public MaintenanceSchedule DeleteToEntity(ScheduleDeleteDTO delete)
    {
        MaintenanceSchedule schedule = new MaintenanceSchedule();
        schedule.ScheduleId = delete.ScheduleId;
        schedule.AssetId = delete.AssetId;
        schedule.TemplateId = delete.TemplateId ?? 0;
        schedule.Content = string.IsNullOrWhiteSpace(delete.Content) ? null : delete.Content.Trim();
        schedule.ScheduleType = (int)delete.ScheduleType;
        ApplyRepeatIntervalToEntity(schedule, delete.IntervalValue, delete.IntervalUnit);
        schedule.StartDate = delete.StartDate;
        schedule.EndDate = delete.EndDate;
        schedule.IsActive = delete.IsActive ?? true;
        schedule.CreateBy = delete.CreateBy;
        schedule.CreateDate = DateTime.UtcNow.AddHours(7);
        return schedule;
    }

    public MaintenanceScheduleResponseDTO EntityToResponse(MaintenanceSchedule entity)
    {
        MaintenanceScheduleResponseDTO response = new MaintenanceScheduleResponseDTO();
        response.ScheduleId = entity.ScheduleId;
        response.AssetId = entity.AssetId ?? 0;
        response.AssetInstanceId = entity.AssetInstanceId;
        response.InstanceCode = entity.AssetInstance?.InstanceCode;
        response.TemplateId = entity.TemplateId;
        response.Content = entity.Content;
        response.TemplateName = entity.Template?.Name;
        response.ScheduleType = (ScheduleType)entity.ScheduleType;
        var (iv, iu) = ReadRepeatIntervalFromEntity(entity);
        response.IntervalValue = iv;
        response.IntervalUnit = iu;
        response.StartDate = entity.StartDate;
        response.EndDate = entity.EndDate;
        response.NextDueDate = entity.NextDueDate;
        response.IsActive = entity.IsActive;
        response.CreateBy = entity.CreateBy;
        response.CreateDate = entity.CreateDate;
        return response;
    }

    public IEnumerable<MaintenanceScheduleResponseDTO> ListEntityToResponse(IEnumerable<MaintenanceSchedule> entities)
    {
        return entities.Select(x => EntityToResponse(x)).ToList();
    }

    public MaintenanceSchedule UpdateToEntity(ScheduleUpdateDTO update)
    {
        MaintenanceSchedule schedule = new MaintenanceSchedule();
        schedule.AssetId = update.AssetId;
        schedule.TemplateId = update.TemplateId ?? 0;
        schedule.Content = string.IsNullOrWhiteSpace(update.Content) ? null : update.Content.Trim();
        schedule.ScheduleType = (int)update.ScheduleType;
        ApplyRepeatIntervalToEntity(schedule, update.IntervalValue, update.IntervalUnit);
        schedule.StartDate = update.StartDate;
        schedule.EndDate = update.EndDate;
        schedule.IsActive = update.IsActive ?? true;
        schedule.CreateBy = update.CreateBy;
        schedule.CreateDate = DateTime.UtcNow.AddHours(7);
        return schedule;
    }
}
