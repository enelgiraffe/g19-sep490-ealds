using g19_sep490_ealds.Server.DTOs.Inventory;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IInventoryService
{
    Task<IEnumerable<InventorySessionListItemDTO>> GetSessionsAsync(int userId, int? departmentId, int? status, string? keyword, bool directorInventoryReport = false);
    Task<InventorySessionDetailDTO> GetSessionByIdAsync(int userId, int sessionId);
    Task<IEnumerable<SessionAssetCheckItemDTO>> GetSessionAssetsAsync(int userId, int sessionId, string? keyword, int? checkStatus);
    Task<IEnumerable<SessionAssetCheckItemDTO>> GetSessionAssetsForCatalogAssetAsync(int userId, int sessionId, int assetId, string? keyword, int? checkStatus);
    Task<AssetInventoryDetailDTO> GetAssetInventoryDetailAsync(int userId, int sessionId, int assetInstanceId);
    Task SaveAssetInventoryAsync(int userId, int sessionId, int assetInstanceId, SaveAssetInventoryDTO dto);
    Task<CreateSessionResultDTO> CreateSessionAsync(int userId, CreateInventorySessionDTO dto);
    Task<SubmitTaskRecordResultDTO> SubmitTaskRecordAsync(int userId, int sessionId, int taskId, SubmitInventoryTaskDTO dto);
    Task<CompleteSessionResultDTO> CompleteSessionAsync(int userId, int sessionId);
    Task<InventoryReviewSummaryDTO> GetReviewSummaryAsync(int userId, int sessionId);
    Task<DirectorApproveResultDTO> DirectorApproveSessionAsync(int userId, int sessionId, ReviewInventorySessionDTO dto);
    Task RequestInventoryRecheckAsync(int userId, int sessionId, ReviewInventorySessionDTO dto);
    Task DepartmentHeadFinishInventoryResolutionAsync(int userId, int sessionId, ReviewInventorySessionDTO dto);
    Task AccountantApplyDiscrepancyActualAsync(int userId, int sessionId, int discrepancyId);
    Task UpdateSessionAsync(int userId, int sessionId, UpdateInventorySessionDTO dto);
    Task ActivateSessionAsync(int userId, int sessionId);
    Task<CancelSessionResultDTO> CancelSessionAsync(int userId, int sessionId, ReviewInventorySessionDTO dto);
    Task<IEnumerable<InventoryDiscrepancyDTO>> GetDiscrepanciesAsync(int userId, int sessionId);
    Task<IEnumerable<DropdownItemDTO>> GetDepartmentsAsync();
    Task<IEnumerable<DropdownItemDTO>> GetAssetCategoriesAsync();
    Task<IEnumerable<DropdownItemDTO>> GetAssetTypesAsync(int? categoryId);
    Task<IEnumerable<DropdownItemDTO>> GetUsersAsync();
}
