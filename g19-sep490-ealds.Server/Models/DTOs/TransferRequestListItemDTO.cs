using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class TransferRequestListItemDTO
{
    public int RecordId { get; set; }

    public int AssetRequestId { get; set; }

    public string Code { get; set; } = null!;

    public DateTime TransferDate { get; set; }

    public string AssetCode { get; set; } = null!;

    public string AssetName { get; set; } = null!;

    public string? AssetTypeName { get; set; }

    public int? AssetInstanceId { get; set; }

    public string? InstanceCode { get; set; }

    public string FromDepartment { get; set; } = null!;

    public string ToDepartment { get; set; } = null!;

    public int FromDepartmentId { get; set; }

    public int ToDepartmentId { get; set; }

    public int CreatedBy { get; set; }

    public string? CreatedByName { get; set; }

    public int Quantity { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = null!;

    /// <summary>Điều chuyển, bảo dưỡng, thanh lý: lý do / mô tả (AssetRequest.Description hoặc tương đương). Không dùng cho GET sửa chữa.</summary>
    public string? Reason { get; set; }

    /// <summary>Sửa chữa: tình trạng hỏng hóc (RepairTask.Reason). Chỉ gán cho GET /api/Assets/Requests/repair.</summary>
    public string? DamageCondition { get; set; }

    /// <summary>Mô tả đơn / phương án sửa chữa (AssetRequest.Description) khi có.</summary>
    public string? RequestDescription { get; set; }

    /// <summary>Chỉ gán cho danh sách thanh lý: nguyên giá cá thể tại thời điểm tra cứu.</summary>
    public decimal? OriginalPrice { get; set; }

    /// <summary>Chỉ gán cho danh sách thanh lý: giá trị còn lại trên sổ (sau khấu hao).</summary>
    public decimal? CurrentValue { get; set; }

    /// <summary>Chỉ gán cho thanh lý: giá trị khai báo trên đơn (DisposalRecord.DiposalValue).</summary>
    public decimal? DisposalDeclaredValue { get; set; }

    public bool IsSenderConfirmed { get; set; }

    public bool IsReceiverConfirmed { get; set; }

    /// <summary>Ghi chú/ý kiến khi kế toán phê duyệt (Approval.Comment, role ACCOUNTANT).</summary>
    public string? AccountantComment { get; set; }

    /// <summary>Ghi chú/ý kiến khi giám đốc phê duyệt (Approval.Comment, role DIRECTOR).</summary>
    public string? DirectorComment { get; set; }
}

