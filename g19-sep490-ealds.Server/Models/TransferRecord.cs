using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class TransferRecord
{
    public int TransferId { get; set; }

    public int AssetInstanceId { get; set; }

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public int? FromUserId { get; set; }

    public int? ToUserId { get; set; }

    public DateTime TransferDate { get; set; }

    public int ExecutedBy { get; set; }

    public virtual AssetInstance AssetInstance { get; set; } = null!;

    public virtual User ExecutedByNavigation { get; set; } = null!;

    public virtual AssetLocation FromLocation { get; set; } = null!;

    public virtual User? FromUser { get; set; }

    public virtual AssetLocation ToLocation { get; set; } = null!;

    public virtual User? ToUser { get; set; }
}
