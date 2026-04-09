using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class DisposalExecutionDto
{
    public int? DisposalExecutionId { get; set; }
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
    public int Status { get; set; }
    public int AssetRequestStatus { get; set; }
    public bool CanEdit { get; set; }
    public bool CanFinalize { get; set; }
    public string? BlockFinalizeReason { get; set; }
}

public class SaveDisposalExecutionDto
{
    public int UserId { get; set; }
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
}

public class FinalizeDisposalExecutionDto
{
    public int UserId { get; set; }
}

public class RecordDisposalAppraisalDto
{
    public int UserId { get; set; }
    public DateTime? AppraisalDate { get; set; }
    public string? AppraisalMinutesNo { get; set; }
    public string? AppraisalConclusion { get; set; }
}
