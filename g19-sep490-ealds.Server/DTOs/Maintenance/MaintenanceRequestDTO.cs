using System;

namespace g19_sep490_ealds.Server.DTOs.Maintenance;

public class MaintenanceRequestDTO
{
    /// <summary>Optional. Null/0 for ad-hoc maintenance proposal (đề xuất bảo dưỡng).</summary>
    public int? ScheduleId { get; set; }

    public int RequestTypeId { get; set; }

    /// <summary>Physical asset instance to maintain.</summary>
    public int AssetInstanceId { get; set; }

    public DateTime? PlannedDate { get; set; }

    public int AssignTo { get; set; }

    public string? Address { get; set; }

    public int CreatedBy { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }
}
