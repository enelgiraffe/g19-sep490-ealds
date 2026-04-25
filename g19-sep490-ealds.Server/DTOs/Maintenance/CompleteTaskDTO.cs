namespace g19_sep490_ealds.Server.DTOs.Maintenance;

public class CompleteTaskDTO
{
    public string WorkPerformed { get; set; } = null!;
    public string ConditionBefore { get; set; } = null!;
    public string ConditionAfter { get; set; } = null!;
    public decimal TotalCost { get; set; }
    public string? TechnicalNote { get; set; }
}
