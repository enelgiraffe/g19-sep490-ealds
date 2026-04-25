using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.DTOs.Allocation;

public class AllocationSummaryDTO
{
    public decimal TotalBudget { get; set; }
    public decimal AllocatedAmount { get; set; }
    public double AllocatedPercentage { get; set; }
    public decimal RevokedAmount { get; set; }
    public int RevokedCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int PendingCount { get; set; }
}

public class AllocationTransactionDTO
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Cat { get; set; } = string.Empty;
    public string Dept { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Date { get; set; } = string.Empty;
    
    // "allocated", "recalled", "pending"
    public string Status { get; set; } = string.Empty;
    public string Approver { get; set; } = string.Empty;
}

public class CreateAllocationRequestDTO
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Approver { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

