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
    private readonly EALDSDbcontext _context;
    private readonly ILogger<MaintenanceTemplateService> _logger;

    public MaintenanceTemplateService(IMaintenanceTemplateMapper mapper,
        EALDSDbcontext context,
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
            //validate loại một lần
            case MaintenanceFrequencyType.OneTime:

                if (create.RepeatIntervalValue != 0)
                    throw new Exception("Bảo trì một lần không được có giá trị khoảng thời gian lặp lại");

                if (create.RepeatIntervalUnit != 0)
                    throw new Exception("Bảo trì một lần không được có đơn vị khoảng thời gian");

                break;
            //validate loaoij định kỳ
            case MaintenanceFrequencyType.Periodic:

                if (create.RepeatIntervalValue <= 0)
                    throw new Exception("Bảo trì định kỳ phải có giá trị khoảng thời gian > 0");

                if (!Enum.IsDefined(typeof(MaintenanceRepeatIntervalUnit), create.RepeatIntervalUnit))
                    throw new Exception("Đơn vị khoảng thời gian không hợp lệ");

                var unit = create.RepeatIntervalUnit;
                //business rule thêm cho từng laoij đơn vị 
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
            //kiểm tra AssetType có tồn tại không
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
            return _mapper.EntityToResponse(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo mẫu bảo trì");
            throw;
        }
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

    public Task<IEnumerable<MaintenanceTemplateResponseDTO>> SearchTemplateByKeyAsync(string name)
    {
        throw new NotImplementedException();
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
        if (await _context.MaintenanceTemplates.AnyAsync(x => x.Name == update.Name))
        {
            throw new Exception("Tên đã được sử dụng");
        }
        var result = _mapper.UpdateToEntity(update);
        template.AssetTypeId = result.AssetTypeId;
        template.Name = result.Name;
        template.Content = result.Content;
        template.FrequencyType = result.FrequencyType;
        template.RepeatIntervalValue = result.RepeatIntervalValue;
        template.RepeatIntervalUnit = result.RepeatIntervalUnit;

        return _mapper.EntityToResponse(template);
    }
}   