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
            throw new Exception("Vui lòng nhập nội dung bảo dưỡng hoặc chọn mẫu quy định.");

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

        if (schedule.IntervalValue is int v && v > 0 && schedule.IntervalUnit is int u)
        {
            var unit = (MaintenanceRepeatIntervalUnit)u;
            return unit switch
            {
                MaintenanceRepeatIntervalUnit.Day => baseDate.AddDays(v),
                MaintenanceRepeatIntervalUnit.Week => baseDate.AddDays(7 * v),
                MaintenanceRepeatIntervalUnit.Month => baseDate.AddMonths(v),
                MaintenanceRepeatIntervalUnit.Year => baseDate.AddYears(v),
                _ => baseDate
            };
        }

        return baseDate;
    }

    public async Task<IEnumerable<MaintenanceScheduleResponseDTO>> GetScheduleByAssetAsync(int assetId)
    {
        var instanceIds = await _context.AssetInstances
            .AsNoTracking()
            .Where(ai => ai.AssetId == assetId)
            .Select(ai => ai.AssetInstanceId)
            .ToListAsync();

        var schedules = await _context.MaintenanceSchedules
            .AsNoTracking()
            .Include(s => s.Template)
            .Include(s => s.AssetInstance)
            .Where(s =>
                s.IsActive &&
                ((s.AssetId == assetId && s.AssetInstanceId == null) ||
                 (s.AssetInstanceId != null && instanceIds.Contains(s.AssetInstanceId.Value))))
            .OrderBy(s => s.ScheduleId)
            .ToListAsync();

        return _mapper.ListEntityToResponse(schedules);
    }

    public async Task<IEnumerable<MaintenanceScheduleResponseDTO>> GetScheduleByInstanceAsync(int assetInstanceId)
    {
        var inst = await _context.AssetInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(ai => ai.AssetInstanceId == assetInstanceId);
        if (inst == null)
            return Enumerable.Empty<MaintenanceScheduleResponseDTO>();

        var catalogSchedules = await _context.MaintenanceSchedules
            .AsNoTracking()
            .Include(s => s.Template)
            .Include(s => s.AssetInstance)
            .Where(s =>
                s.IsActive &&
                s.AssetId == inst.AssetId &&
                s.AssetInstanceId == null)
            .ToListAsync();

        var ownSchedules = await _context.MaintenanceSchedules
            .AsNoTracking()
            .Include(s => s.Template)
            .Include(s => s.AssetInstance)
            .Where(s => s.IsActive && s.AssetInstanceId == assetInstanceId)
            .ToListAsync();

        var merged = catalogSchedules
            .Concat(ownSchedules)
            .OrderBy(s => s.ScheduleId)
            .ToList();

        return _mapper.ListEntityToResponse(merged);
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
