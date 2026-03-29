using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class RepairRequestDTO
{
    /// <summary>Physical asset instance to repair.</summary>
    public int AssetInstanceId { get; set; }

    public int RequestTypeId { get; set; }

    public decimal EstimatedCost { get; set; }

    public string Reason { get; set; } = null!;

    public int CreatedBy { get; set; }

    public int? SupplierId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    // Ngày hỏng – phải <= ngày hiện tại nếu được cung cấp
    public DateTime? DamageDate { get; set; }
}
