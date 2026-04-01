using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IMaintenanceRecordService
{
    Task<IEnumerable<MaintenanceRecordResponseDTO>> GetRecordsByAssetAsync(int assetId);
    Task<IEnumerable<MaintenanceRecordResponseDTO>> GetRecordsByInstanceAsync(int assetInstanceId);
}