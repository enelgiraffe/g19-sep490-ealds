namespace g19_sep490_ealds.Server.Models;

public partial class Procurement
{
    public int ProcurementId { get; set; }

    /// <summary>Optional link to an approved purchase requisition. Null for standalone purchase orders (UC).</summary>
    public int? AssetRequestId { get; set; }

    public int? SupplierId { get; set; }

    public string ContractNo { get; set; } = null!;

    public DateOnly ContractDate { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>ISO currency code, e.g. VND, USD.</summary>
    public string Currency { get; set; } = "VND";

    public decimal TotalAmount { get; set; }

    public decimal AdvanceAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public int Status { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual ICollection<AcceptanceRecord> AcceptanceRecords { get; set; } = new List<AcceptanceRecord>();

    public virtual ICollection<ProcurementLine> Lines { get; set; } = new List<ProcurementLine>();

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<SupplierInvoice> SupplierInvoices { get; set; } = new List<SupplierInvoice>();

    public virtual AssetRequest? AssetRequest { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Supplier? Supplier { get; set; }
}