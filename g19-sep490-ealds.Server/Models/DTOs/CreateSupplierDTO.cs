using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class CreateSupplierDTO
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required]
    [Range(0, 1)]
    public int Status { get; set; }

    [MaxLength(13)]
    [RegularExpression(@"^(\d{10}|\d{13})$", ErrorMessage = "MST must contain exactly 10 or 13 digits.")]
    public string? TaxCode { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(20)]
    [RegularExpression(@"^(?:\+84|0)(?:3|5|7|8|9)\d{8}$", ErrorMessage = "Số điện thoại phải có 10 số (ví dụ: 0912345678) hoặc dạng +84 (ví dụ: +84912345678).")]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [EmailAddress]
    public string? Email { get; set; }
}
