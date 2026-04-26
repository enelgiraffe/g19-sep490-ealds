using g19_sep490_ealds.Server.DTOs.AssetTypes;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetTypeService
{
    Task<IEnumerable<AssetTypeResponseDto>> GetAllAsync(int? categoryId, string? keyword);
    Task<AssetTypeDetailDto> GetByIdAsync(int id);
    Task<AssetTypeResponseDto> CreateAsync(CreateAssetTypeDto dto);
    Task<AssetTypeResponseDto> UpdateAsync(int id, UpdateAssetTypeDto dto);
    Task DeleteAsync(int id);
}
