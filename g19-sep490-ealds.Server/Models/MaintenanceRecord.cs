using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class MaintenanceRecord
{
    public int RecordId { get; set; }

    public int TaskId { get; set; }

    public int AssetInstanceId { get; set; }

    public DateTime ExecutionDate { get; set; }

    public decimal TotalCost { get; set; }

    public int Status { get; set; }

    public string WorkPerformed { get; set; } = null!;

    public string ConditionBefore { get; set; } = null!;

    public string ConditionAfter { get; set; } = null!;

    public string? PerformedBy { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual MaintenanceTask Task { get; set; } = null!;
}
