using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetUsage")]
public partial class AssetUsage
{
    [Key]
    public int UsageId { get; set; }

    public int AssetId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("AssetUsages")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("AssetUsages")]
    public virtual Employee Employee { get; set; } = null!;
}