using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
