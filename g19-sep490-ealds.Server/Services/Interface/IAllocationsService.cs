using System.Collections.Generic;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IAllocationsService
{
    Task<AllocationSummaryDTO> GetSummaryAsync();
    Task<IEnumerable<AllocationTransactionDTO>> GetTransactionsAsync();
    Task<int> AllocateAsync(CreateAllocationRequestDTO dto);
    Task<int> RecallAsync(CreateAllocationRequestDTO dto);
    Task ApproveTransactionAsync(int id);
    Task DeleteTransactionAsync(int id);
}
