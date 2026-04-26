using g19_sep490_ealds.Server.DTOs.Suppliers;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDTO>> GetAllAsync(string? keyword);
    Task<SupplierDTO> GetByIdAsync(int id);
    Task<SupplierDTO> CreateAsync(CreateSupplierDTO dto);
    Task UpdateAsync(int id, UpdateSupplierDTO dto);
    Task<string?> DeleteAsync(int id);
}
