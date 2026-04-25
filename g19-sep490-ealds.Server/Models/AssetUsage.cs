namespace g19_sep490_ealds.Server.Models;

public partial class AssetUsage
{
    public int UsageId { get; set; }

    public int AssetInstanceId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? Note { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
