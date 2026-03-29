using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetRequest")]
public partial class AssetRequest
{
    [Key]
    public int AssetRequestId { get; set; }

    public int UserId { get; set; }

    /// <summary>Tham chiếu đến AssetInstance cụ thể (không phải Asset chung)</summary>
    public int? AssetInstanceId { get; set; }

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

    // Navigation
    [ForeignKey("UserId")]
    [InverseProperty("AssetRequests")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("AssetRequests")]
    public virtual AssetInstance? AssetInstance { get; set; }

    [ForeignKey("RequestTypeId")]
    [InverseProperty("AssetRequests")]
    public virtual RequestType RequestType { get; set; } = null!;

    [InverseProperty("AssetRequest")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<AssetRequestRecord> AssetRequestRecords { get; set; } = new List<AssetRequestRecord>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<TransferRecord> TransferRecords { get; set; } = new List<TransferRecord>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<DisposalRecord> DisposalRecords { get; set; } = new List<DisposalRecord>();

    [InverseProperty("AssetRequest")]
    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();
}
