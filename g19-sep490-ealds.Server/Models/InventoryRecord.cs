using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("InventoryRecord")]
public partial class InventoryRecord
{
    [Key]
    public int RecordId { get; set; }

    public int TaskId { get; set; }

    public int ActualLocationId { get; set; }

    public int? ActualUserId { get; set; }

    public string ActualCondition { get; set; } = null!;

    public bool? IsFound { get; set; }

    public int CheckedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CheckedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateCheckCompleted { get; set; }

    public int? ActualQuantity { get; set; }

    [ForeignKey("ActualLocationId")]
    [InverseProperty("InventoryRecords")]
    public virtual AssetLocation ActualLocation { get; set; } = null!;

    [ForeignKey("ActualUserId")]
    [InverseProperty("InventoryRecordActualUsers")]
    public virtual User? ActualUser { get; set; }

    [ForeignKey("CheckedBy")]
    [InverseProperty("InventoryRecordCheckedByNavigations")]
    public virtual User CheckedByNavigation { get; set; } = null!;

    [ForeignKey("TaskId")]
    [InverseProperty("InventoryRecords")]
    public virtual InventoryTask Task { get; set; } = null!;
}