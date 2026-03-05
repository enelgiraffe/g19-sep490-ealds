using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Auth;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
