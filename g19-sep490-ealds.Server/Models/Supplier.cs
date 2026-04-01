namespace g19_sep490_ealds.Server.Models;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? TaxCode { get; set; }

    public string? ContactName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public int Status { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual ICollection<AssetInstance> AssetInstances { get; set; } = new List<AssetInstance>();

    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    public virtual ICollection<RepairRecord> RepairRecords { get; set; } = new List<RepairRecord>();
}