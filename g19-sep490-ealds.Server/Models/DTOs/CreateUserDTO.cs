using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class CreateUserDTO
{
    [Required]
    [EmailAddress]
    [MaxLength(255)] // Assuming maximum length based on common practices
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    public int Status { get; set; } = 1; // Default active

    public List<int> RoleIds { get; set; } = new List<int>();
}
