using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class AcceptanceRecordDTO
{
    public int AcceptanceId { get; set; }

    public int ProcurementId { get; set; } 

    public DateTime AcceptanceDate { get; set; }

    public DateTime TrialStartDate { get; set; }

    public DateTime TrialEndDate { get; set; }

    public AcceptanceRecordStatus Status { get; set; }

    public string? Note { get; set; }

    public int AcceptedBy { get; set; }
}