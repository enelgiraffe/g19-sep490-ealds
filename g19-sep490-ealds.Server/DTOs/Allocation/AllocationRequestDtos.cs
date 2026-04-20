using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.Allocation;

public class AllocationLineInputDto
{
    [Required]
    public int AssetTypeId { get; set; }

    [Required]
    public int AssetId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public string? Reason { get; set; }
}

public class CreateDepartmentAllocationRequestDto
{
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = null!;

    [MinLength(1)]
    public List<AllocationLineInputDto> Lines { get; set; } = new();
}

public class AllocationRequestListItemDto
{
    public int AssetRequestId { get; set; }

    public int? AssetAllocationOrderId { get; set; }

    public string Title { get; set; } = null!;

    public int Status { get; set; }

    /// <summary>Receiving department (allocation target).</summary>
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public int RequestedByUserId { get; set; }

    public string RequestedByName { get; set; } = null!;

    /// <summary>When the department head confirmed receipt (order row); null if not yet confirmed.</summary>
    public DateTime? ReceiptConfirmedAt { get; set; }

    public int? ReceiptConfirmedByUserId { get; set; }

    public string? ReceiptConfirmedByName { get; set; }
}

public class AllocationOrderLineDetailDto
{
    public int AssetTypeId { get; set; }

    public string AssetTypeName { get; set; } = null!;

    public int AssetId { get; set; }

    public string AssetCode { get; set; } = null!;

    public string AssetName { get; set; } = null!;

    public int Quantity { get; set; }

    public string? Reason { get; set; }
}

/// <summary>Light list row for accountant đơn cấp phát / hoàn trả (orders only).</summary>
public class AllocationOrderSummaryDto
{
    public int AssetAllocationOrderId { get; set; }

    public int AssetRequestId { get; set; }

    public string Title { get; set; } = null!;

    public string DepartmentName { get; set; } = null!;

    /// <summary>awaiting_confirm | confirmed</summary>
    public string OrderStatus { get; set; } = null!;

    public int RequestStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }
}

public class AllocationOrderDetailDto
{
    public int AssetAllocationOrderId { get; set; }

    public int AssetRequestId { get; set; }

    /// <summary>allocation | return — UI copy and confirm semantics.</summary>
    public string OrderKind { get; set; } = "allocation";

    public string Title { get; set; } = null!;

    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public string OrderStatus { get; set; } = null!;

    public int RequestStatus { get; set; }

    public int RequestedByUserId { get; set; }

    public string RequestedByName { get; set; } = null!;

    public DateTime RequestSubmittedAt { get; set; }

    /// <summary>When the accountant approval created this order (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public int? ConfirmedByUserId { get; set; }

    public string? ConfirmedByName { get; set; }

    /// <summary>Ý kiến / ghi chú kế toán khi duyệt yêu cầu (trước khi tạo đơn).</summary>
    public string? AccountantComment { get; set; }

    public List<AllocationOrderLineDetailDto> Lines { get; set; } = new();
}
