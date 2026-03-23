using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class RepairStartDto
{
    // Compatibility with existing approval action payload.
    public int StartedBy { get; set; }
    public string? Comment { get; set; }

    // Form fields: "số biên bản"
    public string? ReportNumber { get; set; }

    // Thông tin ghi nhận tài sản hỏng
    public DateTime? DamageDate { get; set; }
    public string? DamageCondition { get; set; }
    public List<int>? AttachmentDocumentIds { get; set; }
    public List<string>? AttachmentUrls { get; set; }

    // Thông tin sửa chữa
    public DateTime? RepairDate { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public DateTime? ExpectedCompletionFrom { get; set; }
    public DateTime? ExpectedCompletionTo { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? RepairProgressStatus { get; set; }
}
