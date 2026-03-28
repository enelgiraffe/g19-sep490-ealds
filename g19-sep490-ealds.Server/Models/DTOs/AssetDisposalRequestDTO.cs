using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class AssetDisposalRequestDTO
{
    public int UserId { get; set; }

    public int? AssetId { get; set; }

    public int? RequestTypeId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int CreatedBy { get; set; }

    public int DiposalMethod { get; set; }

    public decimal DiposalValue { get; set; }

    // DiposalDate không nhận từ client – server tự gán khi tạo đề nghị

    public string? Reason { get; set; }
}
