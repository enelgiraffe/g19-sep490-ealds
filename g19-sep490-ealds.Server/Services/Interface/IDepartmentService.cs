using g19_sep490_ealds.Server.DTOs.Departments;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDTO>> GetAllAsync(string? keyword);
    Task<DepartmentDTO> GetByIdAsync(int id);
    Task<DepartmentDTO> CreateAsync(int userId, CreateDepartmentDTO dto);
    Task UpdateAsync(int userId, int id, UpdateDepartmentDTO dto);
    Task<string?> DeleteAsync(int userId, int id);
}
