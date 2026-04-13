using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class SupplierInvoiceCreateLineDto
{
    /// <summary>Null for ad-hoc charge lines (shipping, other fees).</summary>
    public int? ProcurementLineId { get; set; }

    public int? GoodsReceiptLineId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>Required when <see cref="ProcurementLineId"/> is null.</summary>
    public string? ChargeDescription { get; set; }
}

public class SupplierInvoiceCreateDto
{
    public int ProcurementId { get; set; }

    public int? GoodsReceiptId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    public string? Note { get; set; }

    public List<SupplierInvoiceCreateLineDto> Lines { get; set; } = new();
}

public class SupplierInvoiceListItemDto
{
    public int SupplierInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal TotalAmount { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public int Status { get; set; }
    public int ProcurementId { get; set; }
    public int? GoodsReceiptId { get; set; }
}

public class SupplierInvoiceListResponseDto
{
    public List<SupplierInvoiceListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class SupplierInvoiceDetailLineDto
{
    public int SupplierInvoiceLineId { get; set; }
    public int? ProcurementLineId { get; set; }
    public int? GoodsReceiptLineId { get; set; }
    public string? ChargeDescription { get; set; }
    public int? AssetId { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class SupplierInvoiceDetailDto
{
    public int SupplierInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public string Currency { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public int Status { get; set; }
    public int ProcurementId { get; set; }
    public int? GoodsReceiptId { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<SupplierInvoiceDetailLineDto> Lines { get; set; } = new();
}
