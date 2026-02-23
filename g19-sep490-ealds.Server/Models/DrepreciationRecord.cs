using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class DrepreciationRecord
{
    public int RecordId { get; set; }

    public int AssetId { get; set; }

    public int PolicyId { get; set; }

    public DateOnly Period { get; set; }

    public decimal DepreciationAmount { get; set; }

    public decimal AccumulatedDepreciation { get; set; }

    public decimal RemainingValue { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual Asset Asset { get; set; } = null!;

    public virtual DepreciationPolicy Policy { get; set; } = null!;
}
