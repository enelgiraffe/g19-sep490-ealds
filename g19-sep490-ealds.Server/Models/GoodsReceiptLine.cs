using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class GoodsReceiptLine
{
    public int GoodsReceiptLineId { get; set; }

    public int GoodsReceiptId { get; set; }

    public int ProcurementLineId { get; set; }

    public decimal QuantityReceived { get; set; }

    /// <summary>Catalog asset used when generating instances (may differ from PO line).</summary>
    public int? AssetId { get; set; }

    public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;

    public virtual ProcurementLine ProcurementLine { get; set; } = null!;

    public virtual Asset? Asset { get; set; }

    public virtual ICollection<AssetInstance> AssetInstances { get; set; } = new List<AssetInstance>();

    public virtual ICollection<SupplierInvoiceLine> SupplierInvoiceLines { get; set; } = new List<SupplierInvoiceLine>();
}
