namespace g19_sep490_ealds.Server.Models.DTOs;

public class AssetRequestListItemDTO
{
    public int AssetRequestId { get; set; }
    public int UserId { get; set; }
    public int? AssetId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ProposedData { get; set; }
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
    public int CreatedBy { get; set; }
    public string? CreatorName { get; set; }
    public string? CreatorDepartmentName { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetName { get; set; }
    public int? AssetQuantity { get; set; }
    public int? AssetInstanceId { get; set; }
    public string? InstanceCode { get; set; }
}
