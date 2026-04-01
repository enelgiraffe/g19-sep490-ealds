namespace g19_sep490_ealds.Server.Models;

public partial class AssetLifeCycle
{
    public int AuditId { get; set; }

    public int AssetInstanceId { get; set; }

    public int ActionType { get; set; }

    public string? Description { get; set; }

    public DateTime OccurredAt { get; set; }

    public int RelatedEntityType { get; set; }

    public int RelatedEntityId { get; set; }

    public int ActorUserId { get; set; }

    public int ActorRoleId { get; set; }

    public virtual Role ActorRole { get; set; } = null!;

    public virtual User ActorUser { get; set; } = null!;

    public virtual AssetInstance AssetInstance { get; set; } = null!;
}