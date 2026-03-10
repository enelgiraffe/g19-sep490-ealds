namespace g19_sep490_ealds.Server.Models.DTOs;

public class AssetRequestListItemDTO
{
    public int AssetRequestId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ProposedData { get; set; }
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
    public int CreatedBy { get; set; }
    public string? CreatorName { get; set; }
}
