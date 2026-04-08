using System;
using System.Text.Json.Serialization;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class TransferHandoverConfirmBody
{
    public string? Note { get; set; }
}

public class TransferHandoverDetailsDto
{
    [JsonPropertyName("side")]
    public string Side { get; set; } = "";

    [JsonPropertyName("protocolCode")]
    public string ProtocolCode { get; set; } = "";

    [JsonPropertyName("assetRequestId")]
    public int AssetRequestId { get; set; }

    [JsonPropertyName("fromDepartment")]
    public string FromDepartment { get; set; } = "";

    [JsonPropertyName("toDepartment")]
    public string ToDepartment { get; set; } = "";

    [JsonPropertyName("instanceCode")]
    public string InstanceCode { get; set; } = "";

    [JsonPropertyName("assetCode")]
    public string AssetCode { get; set; } = "";

    [JsonPropertyName("assetName")]
    public string AssetName { get; set; } = "";

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";
}

public class TransferHandoverRecordItemDto
{
    public int TransferHandoverRecordId { get; set; }

    public string Side { get; set; } = "";

    public int ActionByUserId { get; set; }

    public string? ActionByUserName { get; set; }

    public DateTime OccurredAt { get; set; }

    public TransferHandoverDetailsDto Details { get; set; } = new();

    public string? UserNote { get; set; }
}
