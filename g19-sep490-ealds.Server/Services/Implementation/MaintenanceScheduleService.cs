using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceSchedule;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
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
            throw new Exception("Vui lùng nh?p n?i dung b?o d??ng ho?c ch?n m?u quy ??nh.");

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

        if (schedule.IntervalMonths is int months && months > 0)
            return baseDate.AddMonths(months);

        if (schedule.IntervalHours is int hours && hours > 0)
            return baseDate.AddHours(hours);

        return baseDate;
    }

    public async Task<IEnumerable<MaintenanceScheduleResponseDTO>> GetScheduleByAssetAsync(int assetId)
    {
        var schedules = await _context.MaintenanceSchedules
                        .Where(x => x.AssetId == assetId && x.IsActive == true)
                        .ToListAsync();

        if (!schedules.Any())
            throw new KeyNotFoundException($"Tùi s?n {assetId} chua cù l?ch b?o trù");

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
