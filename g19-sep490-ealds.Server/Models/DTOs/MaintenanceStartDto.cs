using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class MaintenanceStartDto
{
    public int StartedBy { get; set; }      // User thực hiện bắt đầu

    public string? ReportNumber { get; set; }

    public DateTime? MaintenanceDate { get; set; }

    public int? PerformerUserId { get; set; }

    public string? MaintenanceProvider { get; set; }

    public decimal? EstimatedCost { get; set; }

    public DateTime? ExpectedCompletionDate { get; set; }

    public DateTime? ExpectedCompletionFrom { get; set; }

    public DateTime? ExpectedCompletionTo { get; set; }

    public string? MaintenanceContent { get; set; }

    public string? DetailedDescription { get; set; }

    public string? LocationType { get; set; }

    public string? Location { get; set; }

    public string? Comment { get; set; }

    public List<int>? AttachmentDocumentIds { get; set; }

    public List<string>? AttachmentUrls { get; set; }
}
