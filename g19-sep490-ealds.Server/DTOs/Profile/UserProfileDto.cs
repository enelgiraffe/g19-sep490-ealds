namespace g19_sep490_ealds.Server.DTOs.Profile;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? EmployeeCode { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateOnly? Dob { get; set; }
    public int? Gender { get; set; }
    public string? ImageUrl { get; set; }
    public string? DepartmentName { get; set; }

    public int? DepartmentId { get; set; }

    public string Role { get; set; } = null!;

    /// <summary>True when the user has the department-head role (RoleId from App:DepartmentHeadRoleId), independent of <see cref="Role"/> display string.</summary>
    public bool IsDepartmentHead { get; set; }
}
