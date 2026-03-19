using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class RepairCompleteDto
{
    public DateTime? RepairDate { get; set; }
    public decimal ActualCost { get; set; }
    public string Result { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public int CompletedBy { get; set; }
}
