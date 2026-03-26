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
                    throw new Exception("B?o tr� m?t l?n kh�ng du?c c� gi� tr? kho?ng th?i gian l?p l?i");

                if (create.RepeatIntervalUnit != 0)
                    throw new Exception("B?o tr� m?t l?n kh�ng du?c c� don v? kho?ng th?i gian");

                break;
            //validate loaoij d?nh k?
            case MaintenanceFrequencyType.Periodic:

                if (create.RepeatIntervalValue <= 0)
                    throw new Exception("B?o tr� d?nh k? ph?i c� gi� tr? kho?ng th?i gian > 0");

                if (!Enum.IsDefined(typeof(MaintenanceRepeatIntervalUnit), create.RepeatIntervalUnit))
                    throw new Exception("�on v? kho?ng th?i gian kh�ng h?p l?");

                var unit = create.RepeatIntervalUnit;
                //business rule th�m cho t?ng laoij don v? 
                switch (unit)
                {
                    case MaintenanceRepeatIntervalUnit.Day when create.RepeatIntervalValue < 7:
                        throw new Exception("B?o tr� theo ng�y ph?i >= 7 ng�y");

                    case MaintenanceRepeatIntervalUnit.Week when create.RepeatIntervalValue < 2:
                        throw new Exception("B?o tr� theo tu?n ph?i >= 2 tu?n");
                }

                break;

            default:
                throw new Exception("Lo?i b?o tr� kh�ng h?p l?");
        }
    }
    public async Task<MaintenanceTemplateResponseDTO> CreateTemplateAsync(TemplateCreateDTO create)
    {
        try
        {
            //ki?m tra AssetType c� t?n t?i kh�ng
            var type = await _context.AssetTypes.AnyAsync(x => x.AssetTypeId == create.AssetTypeId);
            if (!type)
                throw new Exception("Kh�ng c� lo?i t�i s?n n�o");

            ValidateFrequency(create);

            var existTemplate = await _context.MaintenanceTemplates.AnyAsync(x => x.AssetTypeId == create.AssetTypeId
                                                                                  && x.Name == create.Name && x.IsActive == true);
            if (existTemplate)
                throw new Exception("L?ch b?o tr� chung n�y d� t?n t?i cho lo?i t�i s?n n�y");

            MaintenanceTemplate entity = _mapper.CreateToEntity(create);
            await _context.MaintenanceTemplates.AddAsync(entity);
            await _context.SaveChangesAsync();
            return _mapper.EntityToResponse(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "L?i khi t?o m?u b?o tr�");
            throw;
        }
    }

    public async Task<MaintenanceTemplateResponseDTO> FindTemplateByIdAsync(int id)
    {
        var template = await _context.MaintenanceTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Kh�ng c� Id {id} t?n t?i");

        return _mapper.EntityToResponse(template);
    }

    public async Task<IEnumerable<MaintenanceTemplateResponseDTO>> GetAllTemplatesAsync()
    {
        var template = await _context.MaintenanceTemplates.ToListAsync();
        if (template == null)
            throw new Exception("Kh�ng c� b?n ghi n�o");
        
        return _mapper.ListEntityToResponse(template);
    }

    public async Task<bool> HardDeleteTemplateAsync(int id)
    {
        var template = await _context.MaintenanceTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Kh�ng c� Id {id} t?n t?i");

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
            ?? throw new KeyNotFoundException($"Kh�ng c� Id {id} t?n t?i");

        template.IsActive = !template.IsActive;

        await _context.SaveChangesAsync();
        return _mapper.EntityToResponse(template);
    }

    public async Task<MaintenanceTemplateResponseDTO> UpdatTemplateAsync(int id, TemplateUpdateDTO update)
    {
        var template = await _context.MaintenanceTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Kh�ng c� Id {id} t?n t?i");
        if (await _context.MaintenanceTemplates.AnyAsync(x => x.Name == update.Name))
        {
            throw new Exception("T�n d� du?c s? d?ng");
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
