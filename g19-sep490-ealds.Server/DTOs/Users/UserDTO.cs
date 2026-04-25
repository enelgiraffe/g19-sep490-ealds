using System.Collections.Generic;

namespace g19_sep490_ealds.Server.DTOs.Users;

public class UserDTO
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public int Status { get; set; }
    public string? EmployeeCode { get; set; }
    public string? FullName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Phone { get; set; }
    public string? ImageUrl { get; set; }
    public List<int> RoleIds { get; set; } = new List<int>();
    public List<string> Roles { get; set; } = new List<string>();
}

public class RoleOptionDTO
{
    public int RoleId { get; set; }
    public string Name { get; set; } = null!;
}

public class DepartmentOptionDTO
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = null!;
}

public class UserMetadataDTO
{
    public List<RoleOptionDTO> Roles { get; set; } = new List<RoleOptionDTO>();
    public List<DepartmentOptionDTO> Departments { get; set; } = new List<DepartmentOptionDTO>();
}
