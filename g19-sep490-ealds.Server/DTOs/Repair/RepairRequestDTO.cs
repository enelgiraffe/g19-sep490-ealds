using System;

namespace g19_sep490_ealds.Server.DTOs.Repair;

public class RepairRequestDTO
{
    /// <summary>Physical asset instance to repair.</summary>
    public int AssetInstanceId { get; set; }

    public int RequestTypeId { get; set; }

    public decimal EstimatedCost { get; set; }

    /// <summary>Tình trạng hỏng hóc (lưu vào RepairTask.Reason).</summary>
    public string DamageCondition { get; set; } = null!;

    /// <summary>Phương án sửa chữa đề xuất (bắt buộc khi tạo đơn từ tài sản hỏng).</summary>
    public string? RepairKind { get; set; }

    public int CreatedBy { get; set; }

    public int? SupplierId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    // Ngày hỏng – phải <= ngày hiện tại nếu được cung cấp
    public DateTime? DamageDate { get; set; }
}
