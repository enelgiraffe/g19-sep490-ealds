namespace g19_sep490_ealds.Server.Services.Interface;

public interface IRepairRequestService
{
    Task<IEnumerable<TransferRequestListItemDTO>> GetListAsync(int userId);
    Task<IEnumerable<DamagedInstancePendingRepairDto>> GetDamagedPendingAsync(int userId);
    Task<RepairRequestCreateResultDTO> CreateAsync(RepairRequestDTO dto);
    Task<RepairStartResultDTO> StartRepairAsync(int assetRequestId, RepairStartDto dto);
    Task<RepairCompleteResultDTO> CompleteRepairAsync(int taskId, RepairCompleteDto dto);
}
