namespace g19_sep490_ealds.Server.DTOs.Maintenance;

public class MaintenanceCompleteDto
{
    public int CompletedBy { get; set; }

    /// <summary>Số biên bản (form hoàn thành).</summary>
    public string? ReportNumber { get; set; }

    /// <summary>Ngày hoàn thành bảo dưỡng.</summary>
    public DateTime? CompletionDate { get; set; }

    /// <summary>Tên cũ / client cũ — ưu tiên thấp hơn CompletionDate.</summary>
    public DateTime? ExecutionDate { get; set; }

    /// <summary>Ngày đưa vào sử dụng lại.</summary>
    public DateTime? ReturnToUseDate { get; set; }

    /// <summary>Chi phí thực tế; nếu có thì dùng thay TotalCost.</summary>
    public decimal? ActualCost { get; set; }

    public decimal TotalCost { get; set; }

    /// <summary>Nội dung bảo dưỡng.</summary>
    public string? MaintenanceContent { get; set; }

    /// <summary>Mô tả chi tiết.</summary>
    public string? DetailedDescription { get; set; }

    public string? WorkPerformed { get; set; }
    public string? ConditionBefore { get; set; }
    public string? ConditionAfter { get; set; }
    public string? TechnicalNote { get; set; }

    public List<int>? AttachmentDocumentIds { get; set; }
    public List<string>? AttachmentUrls { get; set; }
}
