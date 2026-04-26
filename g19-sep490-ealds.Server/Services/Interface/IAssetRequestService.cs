using g19_sep490_ealds.Server.DTOs.AssetRequests;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAssetRequestService
{
    Task<List<AssetRequestListItemDTO>> GetPurchaseListAsync(int requestTypeId);

    Task<AssetRequestDetailDTO> GetPurchaseByIdAsync(int id);

    Task<List<AssetRequestPurchaseLineResponseDTO>> GetPurchaseLinesAsync(int id);

    Task<int> CreateAsync(AssetRequestDTO dto);

    Task<int> UpdateAsync(int id, AssetRequestDTO dto);

    Task<AssetRequestFullDetailDTO> GetDetailsAsync(int id);

    Task<AssetRequestPagedResultDTO> ListAsync(int? status, int? requestTypeId, int? userId, int page, int pageSize);

    Task RevertToDraftAsync(int id, int userId);

    Task DeleteDraftAsync(int id, int userId);
}
