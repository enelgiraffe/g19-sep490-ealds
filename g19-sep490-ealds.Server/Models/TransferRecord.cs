using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("TransferRecord")]
public partial class TransferRecord
{
    [Key]
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

    [ForeignKey("AssetId")]
    [InverseProperty("TransferRecords")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("AssetRequestId")]
    [InverseProperty("TransferRecords")]
    public virtual AssetRequest AssetRequest { get; set; } = null!;

    [ForeignKey("ExecuteBy")]
    [InverseProperty("TransferRecordExecuteByNavigations")]
    public virtual User ExecuteByNavigation { get; set; } = null!;

    [ForeignKey("FromLocationId")]
    [InverseProperty("TransferRecordFromLocations")]
    public virtual AssetLocation FromLocation { get; set; } = null!;

    [ForeignKey("FromUserId")]
    [InverseProperty("TransferRecordFromUsers")]
    public virtual User? FromUser { get; set; }

    [ForeignKey("ToLocationId")]
    [InverseProperty("TransferRecordToLocations")]
    public virtual AssetLocation ToLocation { get; set; } = null!;

    [ForeignKey("ToUserId")]
    [InverseProperty("TransferRecordToUsers")]
    public virtual User? ToUser { get; set; }
}