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

    public int Quantity { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Reason { get; set; }

    public bool IsSenderConfirmed { get; set; }

    public bool IsReceiverConfirmed { get; set; }
}

