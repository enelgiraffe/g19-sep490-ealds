using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceSchedule;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace g19_sep490_ealds.Server.Mappers.Implementation;

public class MaintenanceScheduleMapper : IMaintenanceScheduleMapper
{
    public MaintenanceSchedule CreateToEntity(ScheduleCreateDTO create)
    {
        MaintenanceSchedule schedule = new MaintenanceSchedule();
        schedule.AssetId = create.AssetId;
        schedule.TemplateId = create.TemplateId;
        schedule.ScheduleTypeEnum = create.ScheduleType;
        schedule.IntervalValue = create.IntervalValue;
        schedule.IntervalUnitEnum = create.IntervalUnit;
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
        schedule.ScheduleTypeEnum = delete.ScheduleType;
        schedule.IntervalValue = delete.IntervalValue;
        schedule.IntervalUnitEnum = delete.IntervalUnit;
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
        response.ScheduleType = entity.ScheduleTypeEnum;
        response.IntervalValue = entity.IntervalValue;
        response.IntervalUnit = entity.IntervalUnitEnum;
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
        schedule.ScheduleTypeEnum = update.ScheduleType;
        schedule.IntervalValue = update.IntervalValue;
        schedule.IntervalUnitEnum = update.IntervalUnit;
        schedule.StartDate = update.StartDate;
        schedule.EndDate = update.EndDate;
        schedule.IsActive = update.IsActive;
        schedule.CreateBy = update.CreateBy;
        schedule.CreateDate = DateTime.UtcNow.AddHours(7);
        return schedule;
    }
}