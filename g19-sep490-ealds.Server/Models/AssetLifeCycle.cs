using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetLifeCycle")]
public partial class AssetLifeCycle
{
    [Key]
    public int AuditId { get; set; }

    public int AssetId { get; set; }

    public int ActionType { get; set; }

    public int RelatedEntityType { get; set; }

    public int RelatedEntityId { get; set; }

    public int ActorUserId { get; set; }

    public int ActorRoleId { get; set; }

    public string? Description { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime OccurredAt { get; set; }

    [ForeignKey("ActorRoleId")]
    [InverseProperty("AssetLifeCycles")]
    public virtual Role ActorRole { get; set; } = null!;

    [ForeignKey("ActorUserId")]
    [InverseProperty("AssetLifeCycles")]
    public virtual User ActorUser { get; set; } = null!;

    [ForeignKey("AssetId")]
    [InverseProperty("AssetLifeCycles")]
    public virtual Asset Asset { get; set; } = null!;
}