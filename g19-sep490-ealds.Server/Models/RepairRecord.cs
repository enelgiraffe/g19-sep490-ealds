using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class RepairRecord
{
    public int RepairId { get; set; }

    public int TaskId { get; set; }

    public decimal ActualCost { get; set; }

    public DateTime RepairDate { get; set; }

    public string Result { get; set; } = null!;

    public int? SupplierId { get; set; }

    public DateTime? DamageDate { get; set; }

    public string? DamageCondition { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual RepairTask Task { get; set; } = null!;
}
