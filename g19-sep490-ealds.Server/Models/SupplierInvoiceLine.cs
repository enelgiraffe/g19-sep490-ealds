namespace g19_sep490_ealds.Server.Models;

public partial class SupplierInvoiceLine
{
    public int SupplierInvoiceLineId { get; set; }

    public int SupplierInvoiceId { get; set; }

    public int ProcurementLineId { get; set; }

    /// <summary>Optional link to a goods receipt line when the invoice is based on a GR.</summary>
    public int? GoodsReceiptLineId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public virtual SupplierInvoice SupplierInvoice { get; set; } = null!;

    public virtual ProcurementLine ProcurementLine { get; set; } = null!;

    public virtual GoodsReceiptLine? GoodsReceiptLine { get; set; }
}
