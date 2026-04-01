using System;

namespace g19_sep490_ealds.Server.Models;

public enum BudgetAllocationStatus : byte
{
    Pending = 0,
    Allocated = 1,
    Recalled = 2
}

/// <summary>
/// Audit row for accountant actions: assign an asset instance to a department, or remove it from that department.
/// </summary>
public partial class BudgetAllocation
{
    public int BudgetAllocationId { get; set; }

    public int DepartmentId { get; set; }

    public int AssetInstanceId { get; set; }

    public int AssetCategoryId { get; set; }

    public int SubmittedByUserId { get; set; }

    /// <summary>Snapshot of submitter name (employee name or email) at submit time.</summary>
    public string SubmittedByDisplayName { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public BudgetAllocationStatus Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual AssetCategory AssetCategory { get; set; } = null!;

    public virtual User SubmittedByUser { get; set; } = null!;
}
