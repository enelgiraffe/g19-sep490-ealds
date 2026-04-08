using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class AssetRequestDTO
{
    public int UserId { get; set; }

    public int RequestTypeId { get; set; }

    public int? AssetId { get; set; }

    public int? AssetInstanceId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ProposedData { get; set; }

    public int CreatedBy { get; set; }

    /// <summary>
    /// Optional status for creation flow.
    /// -1: Draft, 0: Submitted/Pending approval (default)
    /// </summary>
    public int? Status { get; set; }
}

public class RevertToDraftDTO
{
    public int UserId { get; set; }
}
