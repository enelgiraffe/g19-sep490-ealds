using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class MaintenanceRequestDTO
{
    public int ScheduleId { get; set; }

    public int RequestTypeId { get; set; }

    public int AssetId { get; set; }

    public DateTime? PlannedDate { get; set; }

    public int AssignTo { get; set; }

    public string? Address { get; set; }

    public int CreatedBy { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }
}
