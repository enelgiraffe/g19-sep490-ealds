using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Profile;

public class UpdateProfileRequestDto
{
    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [MaxLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = null!;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    public string? Phone { get; set; }

    [MaxLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
    public string? Address { get; set; }

    public DateOnly? Dob { get; set; }

    public int? Gender { get; set; }

    public string? ImageUrl { get; set; }
}
