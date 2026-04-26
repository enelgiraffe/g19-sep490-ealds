using System.Threading.Tasks;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IDisposalExecutionService
{
    Task<DisposalExecutionDto> GetByAssetRequestAsync(int assetRequestId);
    Task<DisposalExecutionDto> SaveDraftAsync(int userId, int assetRequestId, SaveDisposalExecutionDto dto);
    Task<DisposalFinalizeResultDTO> FinalizeAsync(int userId, int assetRequestId);
    Task<DisposalExecutionDto> RecordAppraisalAsync(int userId, int assetRequestId, RecordDisposalAppraisalDto dto);
}
