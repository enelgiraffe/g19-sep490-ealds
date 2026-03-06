using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Auth;

public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "Token là bắt buộc.")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string ConfirmNewPassword { get; set; } = null!;
}
