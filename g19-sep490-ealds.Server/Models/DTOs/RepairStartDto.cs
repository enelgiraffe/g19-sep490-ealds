using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class RepairStartDto
{
    public int StartedBy { get; set; }

    public string? Comment { get; set; }

    public string? DamageCondition { get; set; }

    public decimal? EstimatedCost { get; set; }

    public DateTime? RepairDate { get; set; }

    public DateTime? ExpectedCompletionDate { get; set; }

    public DateTime? ExpectedCompletionTo { get; set; }

    public string? RepairProgressStatus { get; set; }
}
