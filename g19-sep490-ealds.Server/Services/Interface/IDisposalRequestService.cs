using System.Collections.Generic;
using System.Threading.Tasks;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IDisposalRequestService
{
    Task<IEnumerable<TransferRequestListItemDTO>> GetListAsync();
    Task<(int assetRequestId, int diposalId)> CreateAsync(int userId, AssetDisposalRequestDTO dto);
}
