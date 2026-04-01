using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;

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

    /// <summary>maintenance | repair — phân biệt bản ghi bảo dưỡng và biên bản sửa chữa (ghép trong cùng API).</summary>
    public string RecordSource { get; set; } = "maintenance";
}