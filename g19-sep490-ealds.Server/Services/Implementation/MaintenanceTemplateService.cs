using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class MaintenanceTemplateService : IMaintenanceTemplateService
{
    private readonly IMaintenanceTemplateMapper _mapper;
    private readonly EaldsDbContext _context;
    private readonly ILogger<MaintenanceTemplateService> _logger;

    public MaintenanceTemplateService(IMaintenanceTemplateMapper mapper,
        EaldsDbContext context,
        ILogger<MaintenanceTemplateService> logger)
    {
        _mapper = mapper;
        _context = context;
        _logger = logger;
    }

    private void ValidateFrequency(TemplateCreateDTO create)
    {
        switch (create.FrequencyType)
        {
            //validate loai mot lan
            case MaintenanceFrequencyType.OneTime:

                if (create.RepeatIntervalValue != 0)
                    throw new Exception("Bảo trì một lần không được có giá trị khoảng thời gian lặp lại");

                if (create.RepeatIntervalUnit != 0)
                    throw new Exception("Bảo trì một lần không được có đơn vị khoảng thời gian");

                if (!create.OneTimeScheduledDate.HasValue)
                    throw new Exception("Vui lòng chọn ngày bảo dưỡng (một lần).");

                break;
            //validate loai dinh ki
            case MaintenanceFrequencyType.Periodic:

                if (create.RepeatIntervalValue <= 0)
                    throw new Exception("Bảo trì định kỳ phải có giá trị khoảng thời gian > 0");

                if (!Enum.IsDefined(typeof(MaintenanceRepeatIntervalUnit), create.RepeatIntervalUnit))
                    throw new Exception("Đơn vị khoảng thời gian không hợp lệ");

                var unit = create.RepeatIntervalUnit;
                //business rule them cho loai don vi
                switch (unit)
                {
                    case MaintenanceRepeatIntervalUnit.Day when create.RepeatIntervalValue < 7:
                        throw new Exception("Bảo trì theo ngày phải >= 7 ngày");

                    case MaintenanceRepeatIntervalUnit.Week when create.RepeatIntervalValue < 2:
                        throw new Exception("Bảo trì theo tuần phải >= 2 tuần");
                }

                break;

            default:
                throw new Exception("Loại bảo trì không hợp lệ");
        }
    }
    public async Task<MaintenanceTemplateResponseDTO> CreateTemplateAsync(TemplateCreateDTO create, int? actorUserId = null)
    {
        try
        {
            //kiem tra AssetType ton tai khong
            var type = await _context.AssetTypes.AnyAsync(x => x.AssetTypeId == create.AssetTypeId);
            if (!type)
                throw new Exception("Không có loại tài sản nào");

            //Kiem tra loai template
            ValidateFrequency(create);

            var existTemplate = await _context.MaintenanceTemplates.AnyAsync(x => x.AssetTypeId == create.AssetTypeId
                                                                                  && x.Name == create.Name && x.IsActive == true);
            if (existTemplate)
                throw new Exception("Tên quy định bảo dưỡng đã tồn tại cho loại tài sản này");

            MaintenanceTemplate entity = _mapper.CreateToEntity(create);
            await _context.MaintenanceTemplates.AddAsync(entity);
            await _context.SaveChangesAsync();

            await ApplyTemplateToExistingAssetsAsync(entity, actorUserId);
            return _mapper.EntityToResponse(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo mẫu bảo trì");
            throw;
        }
    }

    private static MaintenanceSchedule BuildScheduleFromTemplate(
        MaintenanceTemplate template,
        int assetInstanceId,
        int createdByUserId,
        DateTime nowLocal)
    {
        MaintenanceRepeatIntervalUnit parsedUnit = MaintenanceRepeatIntervalUnit.Month;
        var hasInterval = template.FrequencyType == (int)MaintenanceFrequencyType.Periodic
                          && template.RepeatIntervalValue > 0
                          && Enum.TryParse(template.RepeatIntervalUnit, true, out parsedUnit);

        var anchor = nowLocal;
        if (template.FrequencyType == (int)MaintenanceFrequencyType.OneTime && template.OneTimeScheduledDate.HasValue)
            anchor = template.OneTimeScheduledDate.Value.Date.AddHours(12);
        var nextDueDate = hasInterval
            ? CalculateNextDueDate(anchor, template.RepeatIntervalValue, parsedUnit)
            : anchor;

        return new MaintenanceSchedule
        {
            // Lịch từ quy định template áp theo từng cá thể => để AssetId = null để thỏa CK_MaintenanceSchedule_Scope.
            AssetId = null,
            AssetInstanceId = assetInstanceId,
            TemplateId = template.TemplateId,
            Content = template.Content,
            ScheduleType = (int)ScheduleType.Auto,
            IntervalValue = hasInterval ? template.RepeatIntervalValue : null,
            IntervalUnit = hasInterval ? (int)parsedUnit : null,
            StartDate = anchor,
            NextDueDate = nextDueDate,
            EndDate = null,
            IsActive = true,
            CreateBy = createdByUserId,
            CreateDate = nowLocal
        };
    }

    private static DateTime CalculateNextDueDate(
        DateTime baseDate,
        int intervalValue,
        MaintenanceRepeatIntervalUnit intervalUnit)
    {
        if (intervalValue <= 0)
            return baseDate;

        return intervalUnit switch
        {
            MaintenanceRepeatIntervalUnit.Day => baseDate.AddDays(intervalValue),
            MaintenanceRepeatIntervalUnit.Week => baseDate.AddDays(7 * intervalValue),
            MaintenanceRepeatIntervalUnit.Month => baseDate.AddMonths(intervalValue),
            MaintenanceRepeatIntervalUnit.Year => baseDate.AddYears(intervalValue),
            _ => baseDate
        };
    }

    private async Task<int?> ResolveScheduleCreatorUserIdAsync(int? actorUserId)
    {
        if (actorUserId.HasValue && actorUserId.Value > 0)
        {
            var actorExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserId == actorUserId.Value);
            if (actorExists)
                return actorUserId.Value;
        }

        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.UserId)
            .Select(u => (int?)u.UserId)
            .FirstOrDefaultAsync();
    }

    private async Task ApplyTemplateToExistingAssetsAsync(MaintenanceTemplate template, int? actorUserId = null)
    {
        if (!template.IsActive)
            return;

        var instances = await _context.AssetInstances
            .AsNoTracking()
            .Where(ai => ai.Asset != null && ai.Asset.AssetTypeId == template.AssetTypeId)
            .Select(ai => new { ai.AssetInstanceId, ai.AssetId })
            .ToListAsync();
        if (instances.Count == 0)
            return;

        var instanceIds = instances.Select(i => i.AssetInstanceId).ToList();
        var existingSchedules = await _context.MaintenanceSchedules
            .Where(s => s.TemplateId == template.TemplateId
                        && s.AssetInstanceId.HasValue
                        && instanceIds.Contains(s.AssetInstanceId.Value))
            .ToListAsync();
        var existingSet = existingSchedules
            .Where(s => s.AssetInstanceId.HasValue)
            .Select(s => s.AssetInstanceId!.Value)
            .ToHashSet();

        // Backfill old periodic schedules that were created with NextDueDate == StartDate.
        var changedExisting = false;
        foreach (var schedule in existingSchedules)
        {
            if (schedule.IntervalValue is int iv && iv > 0 && schedule.IntervalUnit is int iu)
            {
                var unit = (MaintenanceRepeatIntervalUnit)iu;
                var expectedNext = CalculateNextDueDate(schedule.StartDate, iv, unit);
                if (schedule.NextDueDate == null || schedule.NextDueDate.Value <= schedule.StartDate)
                {
                    schedule.NextDueDate = expectedNext;
                    changedExisting = true;
                }
            }
            else if (schedule.NextDueDate == null)
            {
                schedule.NextDueDate = schedule.StartDate;
                changedExisting = true;
            }
        }

        var nowLocal = DateTime.UtcNow.AddHours(7);
        var creatorUserId = await ResolveScheduleCreatorUserIdAsync(actorUserId);
        if (!creatorUserId.HasValue)
            throw new Exception("Không tìm thấy người dùng để ghi nhận lịch bảo dưỡng tự động.");
        var newSchedules = instances
            .Where(i => !existingSet.Contains(i.AssetInstanceId))
            .Select(i => BuildScheduleFromTemplate(template, i.AssetInstanceId, creatorUserId.Value, nowLocal))
            .ToList();

        if (newSchedules.Count == 0 && !changedExisting)
            return;

        if (newSchedules.Count > 0)
            await _context.MaintenanceSchedules.AddRangeAsync(newSchedules);
        await _context.SaveChangesAsync();
    }

    public async Task EnsureSchedulesForNewInstanceAsync(int assetInstanceId, int? actorUserId = null)
    {
        var inst = await _context.AssetInstances
            .AsNoTracking()
            .Include(i => i.Asset)
            .FirstOrDefaultAsync(i => i.AssetInstanceId == assetInstanceId);
        if (inst?.Asset == null)
            return;

        var templates = await _context.MaintenanceTemplates
            .AsNoTracking()
            .Where(t => t.AssetTypeId == inst.Asset.AssetTypeId && t.IsActive)
            .ToListAsync();
        if (templates.Count == 0)
            return;

        var nowLocal = DateTime.UtcNow.AddHours(7);
        var creatorUserId = await ResolveScheduleCreatorUserIdAsync(actorUserId);
        if (!creatorUserId.HasValue)
            return;
        foreach (var t in templates)
        {
            var exists = await _context.MaintenanceSchedules.AnyAsync(s =>
                s.TemplateId == t.TemplateId && s.AssetInstanceId == assetInstanceId);
            if (exists)
                continue;

            await _context.MaintenanceSchedules.AddAsync(
                BuildScheduleFromTemplate(t, assetInstanceId, creatorUserId.Value, nowLocal));
        }

        await _context.SaveChangesAsync();
    }

    public async Task<MaintenanceTemplateResponseDTO> FindTemplateByIdAsync(int id)
    {
        var template = await _context.MaintenanceTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không có Id {id} tồn tại");

        return _mapper.EntityToResponse(template);
    }

    public async Task<IEnumerable<MaintenanceTemplateResponseDTO>> GetAllTemplatesAsync()
    {
        var template = await _context.MaintenanceTemplates.ToListAsync();
        if (template == null)
            throw new Exception("Không có bản ghi nào");

        foreach (var activeTemplate in template.Where(t => t.IsActive))
        {
            await ApplyTemplateToExistingAssetsAsync(activeTemplate);
        }

        return _mapper.ListEntityToResponse(template);
    }

    public async Task<bool> HardDeleteTemplateAsync(int id)
    {
        var template = await _context.MaintenanceTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không có Id {id} tồn tại");

        var scheduleIds = await _context.MaintenanceSchedules
            .Where(s => s.TemplateId == id)
            .Select(s => s.ScheduleId)
            .ToListAsync();

        if (scheduleIds.Count > 0)
        {
            var tasks = await _context.MaintenanceTasks
                .Where(t => t.ScheduleId.HasValue && scheduleIds.Contains(t.ScheduleId.Value))
                .ToListAsync();

            foreach (var task in tasks)
                task.ScheduleId = null;

            var schedules = await _context.MaintenanceSchedules
                .Where(s => s.TemplateId == id)
                .ToListAsync();
            _context.MaintenanceSchedules.RemoveRange(schedules);
        }

        _context.MaintenanceTemplates.Remove(template);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<MaintenanceTemplateResponseDTO>> SearchTemplateByKeyAsync(string name)
    {
        var keyword = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllTemplatesAsync();

        var normalized = keyword.ToLower();
        var templates = await _context.MaintenanceTemplates
            .AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(normalized))
            .OrderBy(x => x.Name)
            .ToListAsync();

        return _mapper.ListEntityToResponse(templates);
    }

    public async Task<MaintenanceTemplateResponseDTO> ToggleTemplateStatusAsync(int id)
    {
        var template = await _context.MaintenanceTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không có Id {id} tồn tại");

        template.IsActive = !template.IsActive;

        await _context.SaveChangesAsync();
        return _mapper.EntityToResponse(template);
    }

    public async Task<MaintenanceTemplateResponseDTO> UpdatTemplateAsync(int id, TemplateUpdateDTO update)
    {
        var template = await _context.MaintenanceTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không có Id {id} tồn tại");

        var normalizedName = (update.Name ?? string.Empty).Trim();
        var isSameCurrentTemplate = template.AssetTypeId == update.AssetTypeId &&
                                    string.Equals((template.Name ?? string.Empty).Trim(), normalizedName,
                                        StringComparison.OrdinalIgnoreCase);

        if (!isSameCurrentTemplate && await _context.MaintenanceTemplates.AnyAsync(x =>
                x.TemplateId != id &&
                x.AssetTypeId == update.AssetTypeId &&
                x.IsActive &&
                x.Name.ToLower() == normalizedName.ToLower()))
        {
            throw new Exception("Tên đã được sử dụng");
        }

        ValidateFrequency(new TemplateCreateDTO
        {
            AssetTypeId = update.AssetTypeId,
            Name = normalizedName,
            Content = update.Content ?? string.Empty,
            FrequencyType = update.FrequencyType,
            RepeatIntervalValue = update.RepeatIntervalValue,
            RepeatIntervalUnit = update.RepeatIntervalUnit,
            IsActive = template.IsActive,
            OneTimeScheduledDate = update.OneTimeScheduledDate
        });

        var result = _mapper.UpdateToEntity(update);
        template.AssetTypeId = result.AssetTypeId;
        template.Name = normalizedName;
        template.Content = result.Content;
        template.FrequencyType = result.FrequencyType;
        template.RepeatIntervalValue = result.RepeatIntervalValue;
        template.RepeatIntervalUnit = result.RepeatIntervalUnit;
        template.OneTimeScheduledDate = result.OneTimeScheduledDate;
        await _context.SaveChangesAsync();

        return _mapper.EntityToResponse(template);
    }
}   
