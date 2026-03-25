using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceSchedule;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class MaintenanceScheduleService : IMaintenanceScheduleService
{
    private readonly IMaintenanceScheduleMapper _mapper;
    private readonly EaldsDbContext _context;

    public MaintenanceScheduleService(IMaintenanceScheduleMapper mapper, EaldsDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<MaintenanceScheduleResponseDTO> CreateScheduleAsync(ScheduleCreateDTO create)
    {
        var asset = await _context.Assets.FindAsync(create.AssetId)
        ?? throw new Exception("Asset not found");
        var hasTemplate = create.TemplateId.HasValue && create.TemplateId.Value > 0;
        var hasContent = !string.IsNullOrWhiteSpace(create.Content);
        if (!hasTemplate && !hasContent)
            throw new Exception("Vui lòng nh?p n?i dung b?o d??ng ho?c ch?n m?u quy ??nh.");

        if (hasTemplate)
        {
            var template = await _context.MaintenanceTemplates.FindAsync(create.TemplateId!.Value)
                ?? throw new Exception("Template not found");
        }

        var schedule = _mapper.CreateToEntity(create);
        schedule.NextDueDate = CalculateNextDueDate(schedule);
        await _context.MaintenanceSchedules.AddAsync(schedule);
        await _context.SaveChangesAsync();
        return _mapper.EntityToResponse(schedule);
    }

    public DateTime CalculateNextDueDate(MaintenanceSchedule schedule)
    {
        var baseDate = schedule.NextDueDate ?? schedule.StartDate;

        var value = schedule.IntervalValue ?? 0;
        var unit = (MaintenanceRepeatIntervalUnit?)schedule.IntervalUnit;

        return unit switch
        {
            MaintenanceRepeatIntervalUnit.Day => baseDate.AddDays(value),
            MaintenanceRepeatIntervalUnit.Week => baseDate.AddDays(value * 7),
            MaintenanceRepeatIntervalUnit.Month => baseDate.AddMonths(value),
            MaintenanceRepeatIntervalUnit.Year => baseDate.AddYears(value),
            _ => baseDate
        };
    }

    public async Task<IEnumerable<MaintenanceScheduleResponseDTO>> GetScheduleByAssetAsync(int assetId)
    {
        var schedules = await _context.MaintenanceSchedules
                        .Where(x => x.AssetId == assetId && x.IsActive == true)
                        .ToListAsync();

        if (!schedules.Any())
            throw new KeyNotFoundException($"Tài s?n {assetId} chua có l?ch b?o trì");

        return _mapper.ListEntityToResponse(schedules);
    }

    public async Task<bool> ToggleScheduleAsync(int scheduleId)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync(scheduleId)
        ?? throw new Exception("Schedule not found");

        schedule.IsActive = !schedule.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }
}
