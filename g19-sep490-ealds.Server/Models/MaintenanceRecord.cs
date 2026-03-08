using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class MaintenanceRecord
{
    public int RecordId { get; set; }

    public int TaskId { get; set; }

    public DateTime ExecutionDate { get; set; }

    public decimal TotalCost { get; set; }

    public int Status { get; set; }

    public string WorkPerformed { get; set; } = null!;

    public string ConditionBefore { get; set; } = null!;

    public string ConditionAfter { get; set; } = null!;

    public string? TechnicalNote { get; set; }

    public virtual MaintenaceTask Task { get; set; } = null!;
}
