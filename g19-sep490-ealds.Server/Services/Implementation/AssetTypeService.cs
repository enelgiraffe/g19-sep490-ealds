using g19_sep490_ealds.Server.DTO.RequestDTO.AssetType;
using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AssetTypeService : IAssetTypeService
{
    private readonly IAssetTypeMapper _mapper;
    private readonly EaldsDbContext _context;
    private readonly ILogger<AssetTypeService> _logger;

    public AssetTypeService(
        IAssetTypeMapper mapper,
        EaldsDbContext context,
        ILogger<AssetTypeService> logger)
    {
        _mapper = mapper;
        _context = context;
        _logger = logger;
    }

    public async Task<AssetTypeResponseDTO> CreateAssetTypeAsync(AssetTypeCreateDTO create)
    {
        if (await _context.AssetTypes.AnyAsync(x => x.Name == create.Name))
        {
            throw new Exception("Tên dã du?c s? d?ng");
        }
        AssetType entity = _mapper.CreateToEntity(create);

        await _context.AssetTypes.AddAsync(entity);
        await _context.SaveChangesAsync();

        return _mapper.EntityToResponse(entity);
    }

    public async Task<IEnumerable<AssetTypeResponseDTO>> GetAllAssetTypeAsync()
    {
        var type = await _context.AssetTypes.ToListAsync();
        if (type == null)
        {
            throw new Exception("Không có b?n ghi nào");
        }
        var response = _mapper.ListEntityToResponse(type);
        return response;
    }

    public async Task<bool> DeleteAssetTypeAsync(int id)
    {
        var type = await _context.AssetTypes.FindAsync(id)
          ?? throw new KeyNotFoundException($"Không có Id {id} t?n t?i!");

        _context.AssetTypes.Remove(type);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<IEnumerable<AssetTypeResponseDTO>> SearchAssetTypeByKeyAsync(string name)
    {
        throw new NotImplementedException();
    }

    public async Task<AssetTypeResponseDTO> UpdateAssetTypeAsync(int id, AssetTypeUpdateDTO update)
    {
        var type = await _context.AssetTypes.FindAsync(id)
          ?? throw new KeyNotFoundException($"Không có Id {id} t?n t?i!");

        var result = _mapper.UpdateToEntity(update);
        type.CategoryId = result.CategoryId;
        type.Name = result.Name;
        await _context.SaveChangesAsync();
        return _mapper.EntityToResponse(type);
    }
}
