using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.DTOs.Maintenance;

public class MaintenanceRecordResponseDTO
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

    /// Luôn là bảo dưỡng (bảng MaintenanceRecord). Lịch sử sửa chữa lấy từ API RepairRecord
    public string RecordSource { get; set; } = "maintenance";
}
