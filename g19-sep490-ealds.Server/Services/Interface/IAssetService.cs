using System.Security.Claims;
using g19_sep490_ealds.Server.DTOs.Assets;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetService
{
    Task<IEnumerable<AssetResponseDTO>> GetAllAsync(ClaimsPrincipal user, string? keyword, AssetStatus? status, int? assetTypeId, bool warehouseStockOnly);
    Task<IReadOnlyList<int>> GetCatalogEligibleAssetTypeIdsAsync(ClaimsPrincipal user, bool forAllocation);
    Task<IEnumerable<string>> GetAssetCodePrefixesAsync();
    Task<AssetDetailResponseDTO> GetByIdAsync(ClaimsPrincipal user, int id);
    Task<IEnumerable<AssetInstanceResponseDTO>> GetAssetsByDepartmentAsync(ClaimsPrincipal user, int userId, int departmentId, string? keyword, AssetStatus? status);
    Task<AssetDetailResponseDTO> CreateAsync(ClaimsPrincipal user, int? actorUserId, CreateAssetDTO dto);
    Task<AssetDocumentDTO> AddDocumentAsync(int userId, int assetId, AddAssetDocumentDTO dto);
    Task RemoveDocumentAsync(int assetId, int documentId);
    Task<AssetDetailResponseDTO> UpdateAsync(int id, UpdateAssetDTO dto);
    Task<AssetDetailResponseDTO> ChangeStatusAsync(int id, ChangeAssetStatusDTO dto);
    Task<AssetResponseDTO> DeleteAsync(int id, AssetStatus? status, DeleteAssetDTO? dto);
}
