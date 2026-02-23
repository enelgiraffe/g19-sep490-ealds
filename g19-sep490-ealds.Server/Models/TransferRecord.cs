using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class TransferRecord
{
    public int RecordId { get; set; }

    public int AssetId { get; set; }

    public int AssetRequestId { get; set; }

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public int? FromUserId { get; set; }

    public int? ToUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime TransferDate { get; set; }

    public int ExecuteBy { get; set; }

    public virtual Asset Asset { get; set; } = null!;

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual User ExecuteByNavigation { get; set; } = null!;

    public virtual AssetLocation FromLocation { get; set; } = null!;

    public virtual User? FromUser { get; set; }

    public virtual AssetLocation ToLocation { get; set; } = null!;

    public virtual User? ToUser { get; set; }
}
