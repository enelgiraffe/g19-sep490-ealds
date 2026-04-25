using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.DTOs.Repair;

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

    /// <summary>Đơn vị sửa chữa (Supplier đã có); có thể bỏ trống.</summary>
    public int? SupplierId { get; set; }

    /// <summary>Tạo đơn vị mới (mã + tên); nếu có thì ưu tiên hơn SupplierId.</summary>
    public RepairSupplierCreateDto? NewSupplier { get; set; }
}
