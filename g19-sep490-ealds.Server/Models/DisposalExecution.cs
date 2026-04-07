using System;

namespace g19_sep490_ealds.Server.Models;

public partial class DisposalExecution
{
    public int DisposalExecutionId { get; set; }

    public int AssetRequestId { get; set; }

    public int? DisposalRecordId { get; set; }

    public DateTime? PlannedExecutionDate { get; set; }

    public DateTime? ExecutedDate { get; set; }

    public int? ExecutionMethod { get; set; }

    public string? BuyerName { get; set; }

    public string? BuyerContact { get; set; }

    public string? ContractNo { get; set; }

    public string? InvoiceNo { get; set; }

    public string? MinutesNo { get; set; }

    public decimal? ActualDisposalValue { get; set; }

    public decimal? ExpenseValue { get; set; }

    public string? AttachmentUrls { get; set; }

    public string? ExecutionNote { get; set; }

    /// <summary>0 Draft, 1 Submitted, 2 Completed, 3 Rejected</summary>
    public int Status { get; set; }

    public int? SubmittedBy { get; set; }

    public DateTime? SubmittedDate { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual DisposalRecord? DisposalRecord { get; set; }
}
