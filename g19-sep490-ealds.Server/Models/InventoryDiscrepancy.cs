using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class InventoryDiscrepancy
{
    public int DiscrepancyId { get; set; }

    public int TaskId { get; set; }

    public int DiscrepancyType { get; set; }

    public decimal BookValue { get; set; }

    public int BookLocationId { get; set; }

    public int? BookUserId { get; set; }

    public string BookCondition { get; set; } = null!;

    public decimal ActualValue { get; set; }

    public int ActualLocationId { get; set; }

    public int? ActualUserId { get; set; }

    public string ActualCondition { get; set; } = null!;

    public virtual AssetLocation ActualLocation { get; set; } = null!;

    public virtual User? ActualUser { get; set; }

    public virtual AssetLocation BookLocation { get; set; } = null!;

    public virtual User? BookUser { get; set; }

    public virtual InventoryTask Task { get; set; } = null!;
}
