using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class AcceptanceRecord
{
    public int AcceptanceId { get; set; }

    public int ProcurementId { get; set; }

    public DateTime AcceptanceDate { get; set; }

    public int Status { get; set; }

    public DateTime TrialStartDate { get; set; }

    public DateTime TrialEndDate { get; set; }

    public string? Note { get; set; }

    public int AcceptedBy { get; set; }

    public virtual Procurement Procurement { get; set; } = null!;
}
