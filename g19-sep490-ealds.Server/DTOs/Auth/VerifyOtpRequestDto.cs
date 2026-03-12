using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Auth;

public class VerifyOtpRequestDto
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có đúng 6 chữ số.")]
    public string OtpCode { get; set; } = null!;
}
