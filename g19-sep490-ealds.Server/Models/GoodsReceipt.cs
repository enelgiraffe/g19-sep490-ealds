using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

/// <summary>Posted goods receipt (biên nhận hàng) against a purchase order.</summary>
public partial class GoodsReceipt
{
    public int GoodsReceiptId { get; set; }

    public int ProcurementId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    /// <summary>1 = posted (completed).</summary>
    public int Status { get; set; }

    public string? Note { get; set; }

    public virtual Procurement Procurement { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();

    public virtual ICollection<SupplierInvoice> SupplierInvoices { get; set; } = new List<SupplierInvoice>();
}
