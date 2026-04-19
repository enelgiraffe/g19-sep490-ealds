using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;
using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
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
            //validate lo?i m?t l?n
            case MaintenanceFrequencyType.OneTime:

                if (create.RepeatIntervalValue != 0)
                    throw new Exception("Bảo trì một lần không được có giá trị khoảng thời gian lặp lại");

                if (create.RepeatIntervalUnit != 0)
                    throw new Exception("Bảo trì một lần không được có đơn vị khoảng thời gian");

                break;
            //validate loaoij d?nh k?
            case MaintenanceFrequencyType.Periodic:

                if (create.RepeatIntervalValue <= 0)
                    throw new Exception("Bảo trì định kỳ phải có giá trị khoảng thời gian > 0");

                if (!Enum.IsDefined(typeof(MaintenanceRepeatIntervalUnit), create.RepeatIntervalUnit))
                    throw new Exception("Đơn vị khoảng thời gian không hợp lệ");

                var unit = create.RepeatIntervalUnit;
                //business rule th�m cho t?ng laoij don v? 
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
    public async Task<MaintenanceTemplateResponseDTO> CreateTemplateAsync(TemplateCreateDTO create)
    {
        try
        {
            //ki?m tra AssetType c� t?n t?i kh�ng
            var type = await _context.AssetTypes.AnyAsync(x => x.AssetTypeId == create.AssetTypeId);
            if (!type)
                throw new Exception("Không có loại tài sản nào");

            ValidateFrequency(create);

            var existTemplate = await _context.MaintenanceTemplates.AnyAsync(x => x.AssetTypeId == create.AssetTypeId
                                                                                  && x.Name == create.Name && x.IsActive == true);
            if (existTemplate)
                throw new Exception("Lịch bảo trì chung này đã tồn tại cho loại tài sản này");

            MaintenanceTemplate entity = _mapper.CreateToEntity(create);
            await _context.MaintenanceTemplates.AddAsync(entity);
            await _context.SaveChangesAsync();

            await ApplyTemplateToExistingAssetsAsync(entity);
            return _mapper.EntityToResponse(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo mẫu bảo trì");
            throw;
        }
    }

    private async Task ApplyTemplateToExistingAssetsAsync(MaintenanceTemplate template)
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
        var existingInstanceIds = await _context.MaintenanceSchedules
            .AsNoTracking()
            .Where(s => s.TemplateId == template.TemplateId
                        && s.AssetInstanceId.HasValue
                        && instanceIds.Contains(s.AssetInstanceId.Value))
            .Select(s => s.AssetInstanceId!.Value)
            .Distinct()
            .ToListAsync();
        var existingSet = existingInstanceIds.ToHashSet();

        MaintenanceRepeatIntervalUnit parsedUnit = MaintenanceRepeatIntervalUnit.Month;
        var hasInterval = template.FrequencyType == (int)MaintenanceFrequencyType.Periodic
                          && template.RepeatIntervalValue > 0
                          && Enum.TryParse(template.RepeatIntervalUnit, true, out parsedUnit);

        var nowLocal = DateTime.UtcNow.AddHours(7);
        var newSchedules = instances
            .Where(i => !existingSet.Contains(i.AssetInstanceId))
            .Select(i => new MaintenanceSchedule
            {
                AssetId = i.AssetId,
                AssetInstanceId = i.AssetInstanceId,
                TemplateId = template.TemplateId,
                Content = template.Content,
                ScheduleType = (int)ScheduleType.Auto,
                IntervalValue = hasInterval ? template.RepeatIntervalValue : null,
                IntervalUnit = hasInterval ? (int)parsedUnit : null,
                StartDate = nowLocal,
                NextDueDate = nowLocal,
                EndDate = null,
                IsActive = true,
                CreateBy = 1,
                CreateDate = nowLocal
            })
            .ToList();

        if (newSchedules.Count == 0)
            return;

        await _context.MaintenanceSchedules.AddRangeAsync(newSchedules);
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

        return _mapper.ListEntityToResponse(template);
    }

    public async Task<bool> HardDeleteTemplateAsync(int id)
    {
        var template = await _context.MaintenanceTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không có Id {id} tồn tại");

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
            Name = update.Name,
            Content = update.Content,
            FrequencyType = update.FrequencyType,
            RepeatIntervalValue = update.RepeatIntervalValue,
            RepeatIntervalUnit = update.RepeatIntervalUnit,
            IsActive = template.IsActive
        });

        var result = _mapper.UpdateToEntity(update);
        template.AssetTypeId = result.AssetTypeId;
        template.Name = normalizedName;
        template.Content = result.Content;
        template.FrequencyType = result.FrequencyType;
        template.RepeatIntervalValue = result.RepeatIntervalValue;
        template.RepeatIntervalUnit = result.RepeatIntervalUnit;
        await _context.SaveChangesAsync();

        return _mapper.EntityToResponse(template);
    }
}   