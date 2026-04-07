using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class GoodsReceiptCreateLineDto
{
    public int ProcurementLineId { get; set; }

    public decimal QuantityReceived { get; set; }

    /// <summary>Override catalog asset for this receipt line; falls back to PO line asset.</summary>
    public int? AssetId { get; set; }

    /// <summary>Optional serial per generated instance (same count as whole units received).</summary>
    public List<string?>? InstanceSerialNumbers { get; set; }
}

public class GoodsReceiptCreateDto
{
    public int ProcurementId { get; set; }

    public int WarehouseId { get; set; }

    public string? Note { get; set; }

    public List<GoodsReceiptCreateLineDto> Lines { get; set; } = new();
}

public class GoodsReceiptListItemDto
{
    public int GoodsReceiptId { get; set; }
    public int ProcurementId { get; set; }
    public string? ContractNo { get; set; }
    public string? SupplierName { get; set; }
    public decimal TotalReceivedQuantity { get; set; }
    public int Status { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class GoodsReceiptListResponseDto
{
    public List<GoodsReceiptListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class GoodsReceiptInstanceDto
{
    public int AssetInstanceId { get; set; }
    public string InstanceCode { get; set; } = null!;
    public string? SerialNumber { get; set; }
}

public class GoodsReceiptDetailLineDto
{
    public int GoodsReceiptLineId { get; set; }
    public int ProcurementLineId { get; set; }
    public int? AssetId { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetName { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal QuantityReceivedOnThisReceipt { get; set; }
    public decimal CumulativeReceivedQuantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public List<GoodsReceiptInstanceDto> Instances { get; set; } = new();
}

public class GoodsReceiptDetailDto
{
    public int GoodsReceiptId { get; set; }
    public int ProcurementId { get; set; }
    public string? ContractNo { get; set; }
    public string? SupplierName { get; set; }
    public DateTime CreatedDate { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
    public List<GoodsReceiptDetailLineDto> Lines { get; set; } = new();
}
