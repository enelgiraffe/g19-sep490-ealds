using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class MaintenanceCompleteDto
{
    public DateTime? ExecutionDate { get; set; }
    public decimal TotalCost { get; set; }
    public string WorkPerformed { get; set; } = string.Empty;
    public string ConditionBefore { get; set; } = string.Empty;
    public string ConditionAfter { get; set; } = string.Empty;
    public string? TechnicalNote { get; set; }
    public int CompletedBy { get; set; }
}
