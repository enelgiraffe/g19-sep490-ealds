using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class AssetRequestDTO
{
    public int UserId { get; set; }

    public int RequestTypeId { get; set; }

    public int? AssetId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ProposedData { get; set; }

    public int CreatedBy { get; set; }
}
