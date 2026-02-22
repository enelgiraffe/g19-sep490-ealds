using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class MaintenaceTask
{
    public int TaskId { get; set; }

    public int? ScheduleId { get; set; }

    public int? AssetRequestId { get; set; }

    public int AssetId { get; set; }

    public DateTime PlannedDate { get; set; }

    public int AssignTo { get; set; }

    public string? Address { get; set; }

    public int Status { get; set; }

    public DateTime CreatDate { get; set; }

    public int CreateBy { get; set; }

    public virtual Asset Asset { get; set; } = null!;

    public virtual AssetRequest? AssetRequest { get; set; }

    public virtual User AssignToNavigation { get; set; } = null!;

    public virtual User CreateByNavigation { get; set; } = null!;

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    public virtual MaintenanceSchedule? Schedule { get; set; }
}
