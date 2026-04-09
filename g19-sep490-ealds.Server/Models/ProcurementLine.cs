namespace g19_sep490_ealds.Server.Models;

/// <summary>Line item on a purchase order (<see cref="Procurement"/>).</summary>
public partial class ProcurementLine
{
    public int LineId { get; set; }

    public int ProcurementId { get; set; }

    public int LineIndex { get; set; }

    public string? Description { get; set; }

    public int? AssetId { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Cumulative quantity received across all goods receipts.</summary>
    public decimal ReceivedQuantity { get; set; }

    public string? Unit { get; set; }

    public decimal UnitPrice { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public virtual Procurement Procurement { get; set; } = null!;

    public virtual Asset? Asset { get; set; }

    public virtual ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = new List<GoodsReceiptLine>();

    public virtual ICollection<SupplierInvoiceLine> SupplierInvoiceLines { get; set; } = new List<SupplierInvoiceLine>();
}
