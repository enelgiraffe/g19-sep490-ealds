
namespace g19_sep490_ealds.Server.Services.Interface;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderListResponseDto> GetListAsync(
        int? procurementId, int? supplierId, int? status,
        bool receivingEligible, int page, int pageSize);

    Task<PurchaseOrderDetailDto> GetByIdAsync(int id);

    Task<int> CreateAsync(int userId, PurchaseOrderCreateDto dto);

    Task UpdateAsync(int userId, int id, PurchaseOrderUpdateDto dto);

    Task CancelAsync(int id);

    Task DeleteAsync(int id);
}
