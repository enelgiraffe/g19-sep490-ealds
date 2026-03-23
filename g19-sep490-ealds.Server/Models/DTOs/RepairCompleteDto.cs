using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class RepairCompleteDto
{
    public int CompletedBy { get; set; }

    public string? ReportNumber { get; set; }

    // Thông tin ghi nhận tài sản hỏng
    public DateTime? DamageDate { get; set; }
    public string? DamageCondition { get; set; }

    public DateTime? CompletionDate { get; set; }
    public DateTime? RepairDate { get; set; }

    public DateTime? ReturnToUseDate { get; set; }

    public decimal ActualCost { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? DetailedDescription { get; set; }
    public int? SupplierId { get; set; }

    public List<int>? AttachmentDocumentIds { get; set; }
    public List<string>? AttachmentUrls { get; set; }
}
