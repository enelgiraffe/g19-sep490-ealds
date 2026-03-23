namespace g19_sep490_ealds.Server.Models.DTOs;

public class SupplierDTO
{
    public int SupplierId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
}
