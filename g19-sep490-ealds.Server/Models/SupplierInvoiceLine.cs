namespace g19_sep490_ealds.Server.Models;

public partial class SupplierInvoiceLine
{
    public int SupplierInvoiceLineId { get; set; }

    public int SupplierInvoiceId { get; set; }

    /// <summary>Null for ad-hoc charges (shipping, fees) not tied to a PO line.</summary>
    public int? ProcurementLineId { get; set; }

    /// <summary>Label for ad-hoc charge lines when <see cref="ProcurementLineId"/> is null.</summary>
    public string? ChargeDescription { get; set; }

    /// <summary>Optional link to a goods receipt line when the invoice is based on a GR.</summary>
    public int? GoodsReceiptLineId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public virtual SupplierInvoice SupplierInvoice { get; set; } = null!;

    public virtual ProcurementLine? ProcurementLine { get; set; }

    public virtual GoodsReceiptLine? GoodsReceiptLine { get; set; }
}
