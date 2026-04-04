using System.ComponentModel.DataAnnotations;

namespace g19_sep490_ealds.Server.DTOs.BudgetAllocation;

public class BudgetAllocationListItemDto
{
    public int Id { get; set; }
    public int AssetInstanceId { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string Date { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string SubmittedBy { get; set; } = null!;
    public string? Note { get; set; }
}

public class CreateBudgetAllocationDto
{
    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public int AssetCategoryId { get; set; }

    [Required]
    public int AssetInstanceId { get; set; }

    public DateTime? TransactionDate { get; set; }

    [MaxLength(4000)]
    public string? Note { get; set; }

    /// <summary>True: remove instance from department; false: assign instance to department.</summary>
    public bool IsRecall { get; set; }
}

public class AssetInstanceOptionDto
{
    public int AssetInstanceId { get; set; }
    public string Label { get; set; } = null!;
}
