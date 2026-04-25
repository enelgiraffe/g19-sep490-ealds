
namespace g19_sep490_ealds.Server.Services.Interface;

public interface IGoodsReceiptService
{
    Task<GoodsReceiptListResponseDto> GetListAsync(
        int? goodsReceiptId, int? procurementId, int? supplierId,
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);

    Task<GoodsReceiptDetailDto> GetByIdAsync(int id);

    Task<int> CreateAsync(int userId, GoodsReceiptCreateDto dto);
}
