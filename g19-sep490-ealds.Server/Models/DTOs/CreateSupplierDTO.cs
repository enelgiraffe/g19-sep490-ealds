using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class CreateSupplierDTO
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [EmailAddress]
    public string? Email { get; set; }
}
