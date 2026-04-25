using System.Collections.Generic;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.BudgetAllocation;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IBudgetAllocationService
{
    Task<IEnumerable<BudgetAllocationListItemDto>> GetListAsync(int? departmentId, string? status);
    Task<IEnumerable<AssetInstanceOptionDto>> GetAssetInstanceOptionsAsync(int categoryId, int departmentId, string mode, string? search);
    Task<BudgetAllocationListItemDto> CreateAsync(int userId, CreateBudgetAllocationDto dto);
    Task DeleteAsync(int id);
}
