using g19_sep490_ealds.Server.DTOs.AssetCategory;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetCategoryService
{
    Task<IEnumerable<AssetCategoryResponseDto>> GetAllAsync(string? keyword);
    Task<AssetCategoryDetailDto> GetByIdAsync(int id);
    Task<AssetCategoryResponseDto> CreateAsync(CreateAssetCategoryDto dto);
    Task<AssetCategoryResponseDto> UpdateAsync(int id, UpdateAssetCategoryDto dto);
    Task DeleteAsync(int id);
}
