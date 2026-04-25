using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.DTOs.Repair;

/// <summary>
/// Một dòng lịch sử sửa chữa (bảng RepairRecord), cùng dạng hiển thị với lịch sử bảo dưỡng trên UI.
/// </summary>
public class RepairRecordHistoryResponseDTO
{
    public int RecordId { get; set; }
    public int TaskId { get; set; }
    public int AssetInstanceId { get; set; }
    public string InstanceCode { get; set; } = string.Empty;

    public DateTime ExecutionDate { get; set; }
    public decimal TotalCost { get; set; }

    public string WorkPerformed { get; set; } = null!;
    public string ConditionBefore { get; set; } = null!;
    public string ConditionAfter { get; set; } = null!;
    public string? TechnicalNote { get; set; }

    public MaintenanceRecordStatus Status { get; set; }

    /// <summary>Tên đơn vị sửa chữa (từ biên bản hoặc từ công việc sửa chữa).</summary>
    public string? RepairUnitName { get; set; }

    /// <summary>Bảo hành theo lần sửa chữa (không phải bảo hành tài sản).</summary>
    public DateOnly? RepairWarrantyStartDate { get; set; }

    public DateOnly? RepairWarrantyEndDate { get; set; }

    public int? RepairWarrantyPeriodValue { get; set; }

    public string? RepairWarrantyPeriodUnit { get; set; }

    public string? RepairWarrantyConditions { get; set; }

    public string? RepairWarrantyNote { get; set; }
}
