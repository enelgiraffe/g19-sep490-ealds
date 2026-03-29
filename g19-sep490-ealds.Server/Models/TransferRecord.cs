using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("TransferRecord")]
public partial class TransferRecord
{
    [Key]
    public int TransferId { get; set; }

    public int AssetInstanceId { get; set; }

    public int AssetRequestId { get; set; }

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public int? FromUserId { get; set; }

    public int? ToUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime TransferDate { get; set; }

    public int ExecutedBy { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("TransferRecords")]
    public virtual AssetInstance AssetInstance { get; set; } = null!;

    [ForeignKey("AssetRequestId")]
    [InverseProperty("TransferRecords")]
    public virtual AssetRequest AssetRequest { get; set; } = null!;

    [ForeignKey("FromLocationId")]
    [InverseProperty("TransferRecordsFrom")]
    public virtual AssetLocation FromLocation { get; set; } = null!;

    [ForeignKey("ToLocationId")]
    [InverseProperty("TransferRecordsTo")]
    public virtual AssetLocation ToLocation { get; set; } = null!;

    [ForeignKey("FromUserId")]
    [InverseProperty("TransferRecordFromUsers")]
    public virtual User? FromUser { get; set; }

    [ForeignKey("ToUserId")]
    [InverseProperty("TransferRecordToUsers")]
    public virtual User? ToUser { get; set; }

    [ForeignKey("ExecutedBy")]
    [InverseProperty("TransferRecordExecutors")]
    public virtual User ExecutedByNavigation { get; set; } = null!;
}
