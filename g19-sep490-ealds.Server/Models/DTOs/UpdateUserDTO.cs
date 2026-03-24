using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class UpdateUserDTO
{
    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Phone { get; set; } = null!;

    [Required]
    public int DepartmentId { get; set; }

    public int Status { get; set; }

    public List<int> RoleIds { get; set; } = new List<int>();
}

public class AdminChangePasswordDTO
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = null!;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; } = null!;
}
