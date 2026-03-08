using g19_sep490_ealds.Server.Utils.EnumsStatus;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class AcceptanceRecord
{
    public int AcceptanceId { get; set; }

    public int ProcurementId { get; set; }

    public DateTime AcceptanceDate { get; set; }

    public DateTime TrialStartDate { get; set; }

    public DateTime TrialEndDate { get; set; }

    public int Status { get; set; }

    public string? Note { get; set; }

    public int AcceptedBy { get; set; }

    public virtual User AcceptedByNavigation { get; set; } = null!;

    public virtual Procurement Procurement { get; set; } = null!;
}
