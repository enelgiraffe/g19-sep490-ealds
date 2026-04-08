namespace g19_sep490_ealds.Server.Models.DTOs;

/// <summary>
/// Physical instance marked damaged, without an active repair approval workflow.
/// </summary>
public class DamagedInstancePendingRepairDto
{
    public int AssetInstanceId { get; set; }

    public int AssetId { get; set; }

    public string InstanceCode { get; set; } = null!;

    public string AssetCode { get; set; } = null!;

    public string AssetName { get; set; } = null!;

    /// <summary>Ghi chú/ghi nhận khi đánh dấu hỏng (thường chứa ngày hỏng + tình trạng).</summary>
    public string? DamageNote { get; set; }

    public string FromDepartment { get; set; } = null!;

    public int FromDepartmentId { get; set; }

    public string Location { get; set; } = null!;
}
