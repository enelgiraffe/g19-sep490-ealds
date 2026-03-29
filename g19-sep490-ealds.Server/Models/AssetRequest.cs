using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetRequest")]
public partial class AssetRequest
{
    [Key]
    public int AssetRequestId { get; set; }

    public int UserId { get; set; }

    public int? AssetId { get; set; }

    public int RequestTypeId { get; set; }

    [StringLength(255)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ProposedData { get; set; }

    public int Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApproveDate { get; set; }

    public int StepId { get; set; }

    [InverseProperty("AssetRequest")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [ForeignKey("AssetId")]
    [InverseProperty("AssetRequests")]
    public virtual Asset? Asset { get; set; }

    [InverseProperty("AssetRequest")]
    public virtual ICollection<AssetRequestRecord> AssetRequestRecords { get; set; } = new List<AssetRequestRecord>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("AssetRequestCreatedByNavigations")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("AssetRequest")]
    public virtual ICollection<DisposalRecord> DisposalRecords { get; set; } = new List<DisposalRecord>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();

    [ForeignKey("RequestTypeId")]
    [InverseProperty("AssetRequests")]
    public virtual RequestType RequestType { get; set; } = null!;

    [ForeignKey("StepId")]
    [InverseProperty("AssetRequests")]
    public virtual WorkflowStep Step { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("AssetRequestUsers")]
    public virtual User User { get; set; } = null!;
}