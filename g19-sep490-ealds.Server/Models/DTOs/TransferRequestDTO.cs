using System;

namespace g19_sep490_ealds.Server.Models.DTOs;

public class TransferRequestDTO
{
    /// <summary>Physical asset instance to transfer.</summary>
    public int AssetInstanceId { get; set; }

    public int RequestTypeId { get; set; }

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public int? FromUserId { get; set; }

    public int? ToUserId { get; set; }

    public DateTime? TransferDate { get; set; }

    public int ExecuteBy { get; set; }

    public int CreatedBy { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    /// <summary>When true, request is created as status Nháp (0) and approvers are not notified.</summary>
    public bool SaveAsDraft { get; set; }

    /// <summary>When true with <see cref="SaveAsDraft"/>, stores <see cref="DraftFormJson"/> on <c>AssetRequest</c> and creates no <c>TransferRecord</c> (incomplete form).</summary>
    public bool IncompleteDraft { get; set; }

    /// <summary>Serialized form state (asset ids, departments, date, reason) for incomplete draft.</summary>
    public string? DraftFormJson { get; set; }

    /// <summary>When submitting a real transfer, removes the prior asset request if it is an incomplete (ProposedData-only) draft for the same user.</summary>
    public int? ReplaceIncompleteAssetRequestId { get; set; }
}

public class UpdateTransferDraftBody
{
    public string? DraftFormJson { get; set; }
}
