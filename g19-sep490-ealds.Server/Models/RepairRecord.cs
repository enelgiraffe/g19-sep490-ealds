namespace g19_sep490_ealds.Server.Models;

public partial class RepairRecord
{
    public int RepairId { get; set; }

    public int TaskId { get; set; }

    public decimal ActualCost { get; set; }

    public DateTime RepairDate { get; set; }

    public string Result { get; set; } = null!;

    /// <summary>Mô tả chi tiết khi hoàn thành sửa chữa (khớp form / DTO DetailedDescription).</summary>
    public string? DetailedDescription { get; set; }

    /// <summary>Ngày đưa vào sử dụng lại (khớp form / DTO ReturnToUseDate).</summary>
    public DateTime? ReturnToUseDate { get; set; }

    public int? SupplierId { get; set; }

    public DateTime? DamageDate { get; set; }

    public string? DamageCondition { get; set; }

    /// <summary>Bảo hành theo lần sửa chữa (cấu trúc tương tự Guarantee; không cập nhật bảo hành tài sản).</summary>
    public DateOnly? RepairWarrantyStartDate { get; set; }

    public DateOnly? RepairWarrantyEndDate { get; set; }

    public int? RepairWarrantyPeriodValue { get; set; }

    public string? RepairWarrantyPeriodUnit { get; set; }

    public string? RepairWarrantyConditions { get; set; }

    public string? RepairWarrantyNote { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual RepairTask Task { get; set; } = null!;
}
