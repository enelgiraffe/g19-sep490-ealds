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
        schedule.IntervalMonths = null;
        schedule.IntervalHours = null;
        if (!intervalUnit.HasValue || !intervalValue.HasValue || intervalValue.Value <= 0)
            return;
        var v = intervalValue.Value;
        switch (intervalUnit.Value)
        {
            case MaintenanceRepeatIntervalUnit.Day:
                schedule.IntervalHours = v * 24;
                break;
            case MaintenanceRepeatIntervalUnit.Week:
                schedule.IntervalHours = v * 24 * 7;
                break;
            case MaintenanceRepeatIntervalUnit.Month:
                schedule.IntervalMonths = v;
                break;
            case MaintenanceRepeatIntervalUnit.Year:
                schedule.IntervalMonths = v * 12;
                break;
        }
    }

    internal static (int? Value, MaintenanceRepeatIntervalUnit? Unit) ReadRepeatIntervalFromEntity(MaintenanceSchedule entity)
    {
        if (entity.IntervalMonths is int months && months > 0)
        {
            if (months % 12 == 0)
                return (months / 12, MaintenanceRepeatIntervalUnit.Year);
            return (months, MaintenanceRepeatIntervalUnit.Month);
        }

        if (entity.IntervalHours is int hours && hours > 0)
        {
            const int weekHours = 24 * 7;
            if (hours % weekHours == 0)
                return (hours / weekHours, MaintenanceRepeatIntervalUnit.Week);
            if (hours % 24 == 0)
                return (hours / 24, MaintenanceRepeatIntervalUnit.Day);
        }

        return (null, null);
    }

    public MaintenanceSchedule CreateToEntity(ScheduleCreateDTO create)
    {
        MaintenanceSchedule schedule = new MaintenanceSchedule();
        schedule.AssetId = create.AssetId;
        schedule.TemplateId = create.TemplateId;
        schedule.Content = string.IsNullOrWhiteSpace(create.Content) ? null : create.Content.Trim();
        schedule.ScheduleTypeEnum = create.ScheduleType;
        ApplyRepeatIntervalToEntity(schedule, create.IntervalValue, create.IntervalUnit);
        schedule.StartDate = create.StartDate;
        schedule.EndDate = create.EndDate;
        schedule.IsActive = create.IsActive;
        schedule.CreateBy = create.CreateBy;
        schedule.CreateDate = DateTime.UtcNow.AddHours(7);
        return schedule;
    }

    public MaintenanceSchedule DeleteToEntity(ScheduleDeleteDTO delete)
    {
        MaintenanceSchedule schedule = new MaintenanceSchedule();
        schedule.ScheduleId = delete.ScheduleId;
        schedule.AssetId = delete.AssetId;
        schedule.TemplateId = delete.TemplateId;
        schedule.Content = string.IsNullOrWhiteSpace(delete.Content) ? null : delete.Content.Trim();
        schedule.ScheduleTypeEnum = delete.ScheduleType;
        ApplyRepeatIntervalToEntity(schedule, delete.IntervalValue, delete.IntervalUnit);
        schedule.StartDate = delete.StartDate;
        schedule.EndDate = delete.EndDate;
        schedule.IsActive = delete.IsActive;
        schedule.CreateBy = delete.CreateBy;
        schedule.CreateDate = DateTime.UtcNow.AddHours(7);
        return schedule;
    }

    public MaintenanceScheduleResponseDTO EntityToResponse(MaintenanceSchedule entity)
    {
        MaintenanceScheduleResponseDTO response = new MaintenanceScheduleResponseDTO();
        response.ScheduleId = entity.ScheduleId;
        response.AssetId = entity.AssetId;
        response.TemplateId = entity.TemplateId;
        response.Content = entity.Content;
        response.ScheduleType = entity.ScheduleTypeEnum;
        var (iv, iu) = ReadRepeatIntervalFromEntity(entity);
        response.IntervalValue = iv;
        response.IntervalUnit = iu;
        response.StartDate = entity.StartDate;
        response.EndDate = entity.EndDate;
        response.NextDueDate = entity.NextDueDate;
        response.IsActive = entity.IsActive;
        response.CreateBy = entity.CreateBy;
        response.CreateDate = DateTime.UtcNow.AddHours(7);
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
        schedule.TemplateId = update.TemplateId;
        schedule.Content = string.IsNullOrWhiteSpace(update.Content) ? null : update.Content.Trim();
        schedule.ScheduleTypeEnum = update.ScheduleType;
        ApplyRepeatIntervalToEntity(schedule, update.IntervalValue, update.IntervalUnit);
        schedule.StartDate = update.StartDate;
        schedule.EndDate = update.EndDate;
        schedule.IsActive = update.IsActive;
        schedule.CreateBy = update.CreateBy;
        schedule.CreateDate = DateTime.UtcNow.AddHours(7);
        return schedule;
    }
}