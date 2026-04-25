using System.Collections.Generic;
using System.Threading.Tasks;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface ITransferRequestService
{
    Task<IEnumerable<TransferRequestListItemDTO>> GetListAsync(int userId, bool isAccountant);
    Task<IEnumerable<TransferHandoverRecordItemDto>> GetHandoverRecordsAsync(int userId, bool isAccountant, int assetRequestId);
    Task<CreateTransferResultDTO> CreateAsync(int userId, TransferRequestDTO dto);
    Task<int> UpdateDraftAsync(int userId, int assetRequestId, UpdateTransferDraftBody body);
    Task DeleteAsync(int userId, int assetRequestId);
    Task<bool> ConfirmSendAsync(int userId, bool isAccountant, int assetRequestId, TransferHandoverConfirmBody? body);
    Task<bool> ConfirmReceiveAsync(int userId, bool isAccountant, int assetRequestId, TransferHandoverConfirmBody? body);
}
