using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class CreateUserDTO
{
    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Phone { get; set; } = null!;

    [Required]
    public int DepartmentId { get; set; }

    public string? ImageUrl { get; set; }

    public int Status { get; set; } = 1;

    // SĐT: tuỳ chọn, nếu cung cấp phải đúng định dạng Việt Nam (10 số, bắt đầu bằng 0)
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (phải gồm 10 chữ số và bắt đầu bằng 0).")]
    public string? Phone { get; set; }

    public List<int> RoleIds { get; set; } = new List<int>();
}
