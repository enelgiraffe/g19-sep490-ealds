using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.DTOs.Repair;

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

    /// <summary>Tạo đơn vị sửa chữa mới khi hoàn thành (mã + tên); nếu có thì ưu tiên hơn SupplierId.</summary>
    public RepairSupplierCreateDto? NewSupplier { get; set; }

    public List<int>? AttachmentDocumentIds { get; set; }
    public List<string>? AttachmentUrls { get; set; }

    /// <summary>Bảo hành gắn với biên bản sửa chữa (giống trường bảo hành cá thể); không thay đổi bảo hành tài sản.</summary>
    public DateOnly? RepairWarrantyStartDate { get; set; }

    public DateOnly? RepairWarrantyEndDate { get; set; }

    public int? RepairWarrantyPeriodValue { get; set; }

    public string? RepairWarrantyPeriodUnit { get; set; }

    public string? RepairWarrantyConditions { get; set; }

    public string? RepairWarrantyNote { get; set; }
}
