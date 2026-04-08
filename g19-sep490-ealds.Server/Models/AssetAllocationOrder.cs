using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public enum AssetAllocationOrderStatus : byte
{
    AwaitingDepartmentConfirm = 1,
    Confirmed = 2
}

/// <summary>Allocation (kho → phòng ban) or return to warehouse (phòng ban → kho).</summary>
public enum AssetAllocationOrderKind : byte
{
    Allocation = 1,
    ReturnToWarehouse = 2
}

/// <summary>Đơn cấp phát — created when accountant approves an allocation request.</summary>
public partial class AssetAllocationOrder
{
    public int AssetAllocationOrderId { get; set; }

    public int AssetRequestId { get; set; }

    public int DepartmentId { get; set; }

    /// <summary>User who submitted the allocation request (dept head account).</summary>
    public int RequestedByUserId { get; set; }

    /// <summary>When the request was submitted (copied from AssetRequest for reporting).</summary>
    public DateTime RequestSubmittedAt { get; set; }

    public AssetAllocationOrderStatus Status { get; set; }

    public AssetAllocationOrderKind Kind { get; set; } = AssetAllocationOrderKind.Allocation;

    /// <summary>When the accountant approved and this order row was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the receiving department head confirmed receipt (UTC).</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Department head user who confirmed receipt.</summary>
    public int? ConfirmedByUserId { get; set; }

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual User RequestedByUser { get; set; } = null!;

    public virtual User? ConfirmedByUser { get; set; }

    public virtual ICollection<AssetAllocationOrderLine> Lines { get; set; } = new List<AssetAllocationOrderLine>();
}
