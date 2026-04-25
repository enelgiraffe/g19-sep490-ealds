using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.DTOs.PurchaseOrders;

public class PurchaseOrderLineWriteDto
{
    public string? Description { get; set; }
    public int? AssetId { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
}

public class PurchaseOrderCreateDto
{
    /// <summary>Optional link to an approved requisition.</summary>
    public int? AssetRequestId { get; set; }

    public int SupplierId { get; set; }

    public string? ContractNo { get; set; }

    public string Currency { get; set; } = "VND";

    public List<PurchaseOrderLineWriteDto> Lines { get; set; } = new();

    /// <summary>If true, saves as draft (status=-1). Default false (status=0).</summary>
    public bool IsDraft { get; set; } = false;
}

public class PurchaseOrderUpdateDto : PurchaseOrderCreateDto
{
}

public class PurchaseOrderLineItemDto
{
    public int LineId { get; set; }
    public int LineIndex { get; set; }
    public string? Description { get; set; }

    /// <summary>From linked <see cref="Asset"/> when <see cref="AssetId"/> is set.</summary>
    public int? AssetTypeId { get; set; }

    public string? AssetTypeName { get; set; }

    public int? AssetId { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public decimal LineTotal { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal OpenQuantity { get; set; }
}

public class PurchaseOrderListItemDto
{
    public int ProcurementId { get; set; }
    public int? AssetRequestId { get; set; }
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string ContractNo { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
}

public class PurchaseOrderDetailDto : PurchaseOrderListItemDto
{
    /// <summary>When linked to a purchase requisition, the linked <see cref="AssetRequest.Title"/> (purchase purpose on PR screens).</summary>
    public string? AssetRequestTitle { get; set; }

    public List<PurchaseOrderLineItemDto> Lines { get; set; } = new();
}

public class PurchaseOrderListResponseDto
{
    public List<PurchaseOrderListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class CancelPurchaseOrderDto
{
    public string? Comment { get; set; }
}
