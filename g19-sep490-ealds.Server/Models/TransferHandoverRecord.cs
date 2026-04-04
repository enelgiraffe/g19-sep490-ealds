using System;

namespace g19_sep490_ealds.Server.Models;

/// <summary>
/// Audit row created when bên gửi / bên nhận xác nhận bàn giao (one per action).
/// </summary>
public partial class TransferHandoverRecord
{
    public int TransferHandoverRecordId { get; set; }

    public int TransferId { get; set; }

    /// <summary>Sender or Receiver — matches UI tab / role for this action.</summary>
    public string Side { get; set; } = null!;

    public int ActionByUserId { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary>JSON snapshot: departments, asset codes, side-specific summary.</summary>
    public string DetailsJson { get; set; } = "{}";

    public string? UserNote { get; set; }

    public virtual TransferRecord Transfer { get; set; } = null!;

    public virtual User ActionByUser { get; set; } = null!;
}
