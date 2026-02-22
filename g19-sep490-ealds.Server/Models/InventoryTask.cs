using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class InventoryTask
{
    public int TaskId { get; set; }

    public int AssetId { get; set; }

    public int SessionId { get; set; }

    public int AssignedUserId { get; set; }

    public int DepartmentId { get; set; }

    public int Status { get; set; }

    public DateTime CheckDate { get; set; }

    public string? Note { get; set; }

    public virtual Asset Asset { get; set; } = null!;

    public virtual User AssignedUser { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancies { get; set; } = new List<InventoryDiscrepancy>();

    public virtual ICollection<InventoryRecord> InventoryRecords { get; set; } = new List<InventoryRecord>();

    public virtual InventorySession Session { get; set; } = null!;
}
