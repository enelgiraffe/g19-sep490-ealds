using g19_sep490_ealds.Server.DTOs.Users;

namespace g19_sep490_ealds.Server.Services.Interface;

public interface IUsersService
{
    Task<IEnumerable<UserDTO>> GetUsersAsync();
    Task<UserDTO> GetUserAsync(int id);
    Task<UserDTO> CreateUserAsync(CreateUserDTO dto);
    Task UpdateUserAsync(int id, UpdateUserDTO dto);
    Task DeactivateUserAsync(int id);
    Task ManageUserRolesAsync(int id, AssignRoleDTO dto);
    Task<UserMetadataDTO> GetMetadataAsync();
    Task<IEnumerable<RoleOptionDTO>> GetRolesAsync();
    Task<IEnumerable<DepartmentOptionDTO>> GetDepartmentsAsync();
    Task AdminChangePasswordAsync(int id, AdminChangePasswordDTO dto);
}
