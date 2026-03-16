using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Profile;

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống.")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmNewPassword { get; set; } = null!;
}
