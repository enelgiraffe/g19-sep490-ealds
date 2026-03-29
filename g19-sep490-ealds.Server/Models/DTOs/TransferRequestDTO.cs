using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class TransferRequestDTO
{
    /// <summary>Physical asset instance to transfer.</summary>
    public int AssetInstanceId { get; set; }

    public int RequestTypeId { get; set; }

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public int? FromUserId { get; set; }

    public int? ToUserId { get; set; }

    public DateTime? TransferDate { get; set; }

    public int ExecuteBy { get; set; }

    public int CreatedBy { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }
}
