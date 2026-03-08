using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class DepreciationPolicy
{
    public int PolicyId { get; set; }

    public string Name { get; set; } = null!;

    public int Method { get; set; }

    public int UsefullLifeMonths { get; set; }

    public decimal SalvageValue { get; set; }

    public DateTime CreateDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<DrepreciationRecord> DrepreciationRecords { get; set; } = new List<DrepreciationRecord>();
}
