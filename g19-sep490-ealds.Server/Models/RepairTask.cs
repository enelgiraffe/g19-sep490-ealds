using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class RepairTask
{
    public int TaskId { get; set; }

    public int AssetRequestId { get; set; }

    public int AssetInstanceId { get; set; }

    public decimal EstimatedCost { get; set; }

    public string Reason { get; set; } = null!;

    public int Status { get; set; }

    public DateTime? RepairDate { get; set; }

    public DateTime? ExpectedCompletionDate { get; set; }

    public string? RepairProgressStatus { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual ICollection<RepairRecord> RepairRecords { get; set; } = new List<RepairRecord>();
}
