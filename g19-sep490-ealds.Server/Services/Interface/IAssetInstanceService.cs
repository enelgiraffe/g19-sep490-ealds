using System.Security.Claims;
using g19_sep490_ealds.Server.DTOs.Assets;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetInstanceService
{
    Task<IEnumerable<AssetInstanceResponseDTO>> GetAllAsync(ClaimsPrincipal user, string? keyword, AssetStatus? status, int? assetTypeId, int? warehouseId, int? currentDepartmentId, decimal? minPrice, decimal? maxPrice, DateOnly? fromDate, DateOnly? toDate, bool forTransferSelection);
    Task<IEnumerable<string>> GetInstanceCodePrefixesAsync();
    Task<AssetInstanceResponseDTO> GetByIdAsync(ClaimsPrincipal user, int id);
    Task<AssetInstanceResponseDTO> CreateAsync(ClaimsPrincipal user, int? actorUserId, CreateAssetInstanceDTO dto);
    Task<AssetInstanceResponseDTO> UpdateAsync(ClaimsPrincipal user, int actorUserId, int id, UpdateAssetInstanceDTO dto);
    Task<AssetInstanceResponseDTO> ChangeStatusAsync(int id, ChangeAssetInstanceStatusDTO dto);
    Task<AssetInstanceResponseDTO> DeleteAsync(int id, AssetStatus? status, DeleteAssetInstanceDTO? dto);
}
