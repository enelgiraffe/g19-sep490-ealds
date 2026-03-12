using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class ReportDamageDTO
{
    public int AssetId { get; set; }

    public int ReportedBy { get; set; }

    public int? RequestTypeId { get; set; }

    // Date when damage was observed/reported
    public DateTime ReportDate { get; set; }

    public string? Description { get; set; }

    // Link to an existing Document (file upload) for evidence; must exist in Documents table
    public int? DocumentId { get; set; }
}
