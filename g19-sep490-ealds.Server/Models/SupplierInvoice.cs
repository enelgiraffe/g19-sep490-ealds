using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

/// <summary>Supplier (purchase) invoice recorded by accountant (UC3).</summary>
public partial class SupplierInvoice
{
    public int SupplierInvoiceId { get; set; }

    public int ProcurementId { get; set; }

    /// <summary>Set when the invoice is tied to a specific goods receipt; otherwise null (PO-only reference).</summary>
    public int? GoodsReceiptId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    public string Currency { get; set; } = "VND";

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    /// <summary>0 = active, 1 = cancelled.</summary>
    public int Status { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Procurement Procurement { get; set; } = null!;

    public virtual GoodsReceipt? GoodsReceipt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<SupplierInvoiceLine> Lines { get; set; } = new List<SupplierInvoiceLine>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
