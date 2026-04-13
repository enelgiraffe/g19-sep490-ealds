namespace g19_sep490_ealds.Server.Models;

public partial class AssetRequestRecord
{
    public int RecordId { get; set; }

    public int AssetRequestId { get; set; }

    public int FromStatus { get; set; }

    public int ToStatus { get; set; }

    public int Action { get; set; }

    public int ActionByUserId { get; set; }

    public int ActionRoleId { get; set; }

    public string? Comment { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual User ActionByUser { get; set; } = null!;

    public virtual Role ActionRole { get; set; } = null!;

    public virtual AssetRequest AssetRequest { get; set; } = null!;
}