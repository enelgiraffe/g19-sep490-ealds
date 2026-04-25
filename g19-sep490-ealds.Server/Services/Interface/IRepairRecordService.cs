
namespace g19_sep490_ealds.Server.Services.Interface;

public interface IRepairRecordService
{
    Task<IEnumerable<RepairRecordHistoryResponseDTO>> GetHistoryByAssetAsync(int assetId);

    Task<IEnumerable<RepairRecordHistoryResponseDTO>> GetHistoryByInstanceAsync(int assetInstanceId);
}
