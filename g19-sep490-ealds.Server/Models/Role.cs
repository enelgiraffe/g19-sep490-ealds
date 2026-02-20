using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("Role")]
public partial class Role
{
    [Key]
    public int RoleId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string Code { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    public int? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdateDate { get; set; }

    public int? UpdatedBy { get; set; }

    [InverseProperty("ApprovedRole")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [InverseProperty("ActorRole")]
    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    [InverseProperty("ActionRole")]
    public virtual ICollection<AssetRequestRecord> AssetRequestRecords { get; set; } = new List<AssetRequestRecord>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("Roles")]
    public virtual User? CreatedByNavigation { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
}