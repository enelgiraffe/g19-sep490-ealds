
namespace g19_sep490_ealds.Server.Services.Interface;

public interface ISupplierInvoiceService
{
    Task<SupplierInvoiceListResponseDto> GetListAsync(
        string? invoiceNumber, int? supplierId,
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);

    Task<SupplierInvoiceDetailDto> GetByIdAsync(int id);

    Task<int> CreateAsync(int userId, SupplierInvoiceCreateDto dto);

    Task CancelAsync(int id);
}
