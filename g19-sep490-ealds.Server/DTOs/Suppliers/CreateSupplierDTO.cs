using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Suppliers;

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
    [RegularExpression(@"^(\d{10}|\d{13})$", ErrorMessage = "MST phải gồm đúng 10 hoặc 13 chữ số.")]
    public string? TaxCode { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(20)]
    [RegularExpression(@"^(?:\+84|0)(?:3|5|7|8|9)\d{8}$", ErrorMessage = "Số điện thoại phải có 10 số")]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [EmailAddress]
    public string? Email { get; set; }
}
