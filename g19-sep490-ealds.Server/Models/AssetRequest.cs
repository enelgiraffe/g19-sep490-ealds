using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetRequest")]
public partial class AssetRequest
{
    [Key]
    public int AssetRequestId { get; set; }

    public int UserId { get; set; }

    public int RequestTypeId { get; set; }

    public int? AssetId { get; set; }

    [StringLength(255)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ProposedData { get; set; }

    public int Status { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

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

    [InverseProperty("AssetRequest")]
    public virtual ICollection<DiposalRecord> DiposalRecords { get; set; } = new List<DiposalRecord>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<MaintenaceTask> MaintenaceTasks { get; set; } = new List<MaintenaceTask>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();

    [ForeignKey("RequestTypeId")]
    [InverseProperty("AssetRequests")]
    public virtual RequestType RequestType { get; set; } = null!;

    [InverseProperty("AssetRequest")]
    public virtual ICollection<TransferRecord> TransferRecords { get; set; } = new List<TransferRecord>();

    [ForeignKey("UserId")]
    [InverseProperty("AssetRequests")]
    public virtual User User { get; set; } = null!;
}