namespace g19_sep490_ealds.Server.Models;

public partial class Document
{
    public int DocumentId { get; set; }

    public int? ProcurementId { get; set; }

    public int? GoodsReceiptId { get; set; }

    public int? SupplierInvoiceId { get; set; }

    public string FileUrl { get; set; } = null!;

    public int DocumentType { get; set; }

    public int? AssetId { get; set; }

    public int? AssetInstanceId { get; set; }

    public int UploadedBy { get; set; }

    public DateTime UploadedDate { get; set; }

    public virtual Asset? Asset { get; set; }

    public virtual AssetInstance? AssetInstance { get; set; }

    public virtual Procurement? Procurement { get; set; }

    public virtual GoodsReceipt? GoodsReceipt { get; set; }

    public virtual SupplierInvoice? SupplierInvoice { get; set; }

    public virtual User UploadedByNavigation { get; set; } = null!;
}
