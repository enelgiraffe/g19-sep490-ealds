using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class MaintenanceStartDto
{
    // Compatibility with existing approval action payload.
    public int StartedBy { get; set; }
    public string? Comment { get; set; }

    // Form fields: "số biên bản"
    public string? ReportNumber { get; set; }

    // Thông tin bảo dưỡng tài sản
    public DateTime? MaintenanceDate { get; set; }
    public int? PerformerUserId { get; set; }
    public string? MaintenanceProvider { get; set; }
    public decimal? EstimatedCost { get; set; }

    // Ngày dự kiến hoàn thành: cho phép ngày cụ thể hoặc khoảng thời gian.
    public DateTime? ExpectedCompletionDate { get; set; }
    public DateTime? ExpectedCompletionFrom { get; set; }
    public DateTime? ExpectedCompletionTo { get; set; }

    public string? MaintenanceContent { get; set; }
    public string? DetailedDescription { get; set; }
    public string? LocationType { get; set; } // "at-unit" | "provider"
    public string? Location { get; set; }

    // File đính kèm
    public List<int>? AttachmentDocumentIds { get; set; }
    public List<string>? AttachmentUrls { get; set; }
}
