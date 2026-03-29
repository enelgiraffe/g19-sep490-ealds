using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class InventorySession
{
    public int SessionId { get; set; }

    public string Code { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int DepartmentId { get; set; }

    public int? AssetCategoryId { get; set; }

    public int? AssetTypeId { get; set; }

    public int Status { get; set; }

    public DateTime? CreateDate { get; set; }

    public int? ProgressPercent { get; set; }

    public int CreatedBy { get; set; }

    public bool IsPeriodic { get; set; }

    public int? PeriodDays { get; set; }

    public virtual AssetCategory? AssetCategory { get; set; }

    public virtual AssetType? AssetType { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<InventoryTask> InventoryTasks { get; set; } = new List<InventoryTask>();
}
