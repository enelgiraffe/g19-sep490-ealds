namespace g19_sep490_ealds.Server.Models;

public partial class AssetRequest
{
    public int AssetRequestId { get; set; }

    public int UserId { get; set; }

    public int? AssetId { get; set; }

    public int? AssetInstanceId { get; set; }

    public int RequestTypeId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ProposedData { get; set; }

    public int Status { get; set; }

    public DateTime CreateDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ApproveDate { get; set; }

    public int StepId { get; set; }

    /// <summary>When set (e.g. allocation requests), filters list queries by requesting department.</summary>
    public int? AllocationTargetDepartmentId { get; set; }

    /// <summary>When an allocation request is auto-created from an approved purchase requisition, links to that PR.</summary>
    public int? SourcePurchaseRequestId { get; set; }

    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    public virtual AssetAllocationOrder? AssetAllocationOrder { get; set; }

    public virtual Asset? Asset { get; set; }

    public virtual AssetInstance? AssetInstance { get; set; }

    public virtual ICollection<AssetRequestRecord> AssetRequestRecords { get; set; } = new List<AssetRequestRecord>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<DisposalRecord> DisposalRecords { get; set; } = new List<DisposalRecord>();

    public virtual ICollection<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();

    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    public virtual ICollection<RepairTask> RepairTasks { get; set; } = new List<RepairTask>();

    public virtual ICollection<TransferRecord> TransferRecords { get; set; } = new List<TransferRecord>();

    public virtual ICollection<AssetRequestPurchaseLine> PurchaseLines { get; set; } = new List<AssetRequestPurchaseLine>();

    public virtual RequestType RequestType { get; set; } = null!;

    public virtual WorkflowStep Step { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
