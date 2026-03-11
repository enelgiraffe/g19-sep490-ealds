using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class ReportDamageDTO
{
    public int AssetId { get; set; }

    public int ReportedBy { get; set; }

    public int? RequestTypeId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? Severity { get; set; }
}
