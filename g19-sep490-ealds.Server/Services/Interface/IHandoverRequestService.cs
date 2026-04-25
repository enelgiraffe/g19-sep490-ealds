using System.Collections.Generic;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.Allocation;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IHandoverRequestService
{
    Task<int> GetDepartmentAssignedAsync(int userId, int assetId);
    Task<int> CreateAsync(int userId, CreateDepartmentAllocationRequestDto dto);
    Task<IEnumerable<AllocationRequestListItemDto>> GetListAsync(int userId);
    Task<AllocationOrderDetailDto> GetOrderAsync(int userId, int orderId);
    Task ConfirmOrderAsync(int userId, int orderId);
    Task<IEnumerable<AllocationOrderSummaryDto>> GetOrdersSummaryAsync(int userId);
}
