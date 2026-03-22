using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("DepreciationPolicy")]
public partial class DepreciationPolicy
{
    [Key]
    public int PolicyId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    public int Method { get; set; }

    public int UsefullLifeMonths { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SalvageValue { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    public bool IsActive { get; set; }

    [InverseProperty("Policy")]
    public virtual ICollection<DrepreciationRecord> DrepreciationRecords { get; set; } = new List<DrepreciationRecord>();

    [InverseProperty("DepreciationPolicy")]
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
