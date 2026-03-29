using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class DepreciationRecord
{
    public int RecordId { get; set; }

    public int AssetInstanceId { get; set; }

    public int PolicyId { get; set; }

    public DateOnly Period { get; set; }

    public decimal DepreciationAmount { get; set; }

    public decimal OriginalValue { get; set; }

    public decimal RemainingValue { get; set; }

    public decimal AccumulatedDepreciation { get; set; }

    public DateTime CreateDate { get; set; }

    public bool? IsLocked { get; set; }

    public bool IsPosted { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual DepreciationPolicy Policy { get; set; } = null!;
}
