using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("InventoryTask")]
public partial class InventoryTask
{
    [Key]
    public int TaskId { get; set; }

    public int AssetId { get; set; }

    public int SessionId { get; set; }

    public int AssignedUserId { get; set; }

    public int DepartmentId { get; set; }

    public int Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CheckDate { get; set; }

    public string? Note { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("InventoryTasks")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("AssignedUserId")]
    [InverseProperty("InventoryTasks")]
    public virtual User AssignedUser { get; set; } = null!;

    [ForeignKey("DepartmentId")]
    [InverseProperty("InventoryTasks")]
    public virtual Department Department { get; set; } = null!;

    [InverseProperty("Task")]
    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancies { get; set; } = new List<InventoryDiscrepancy>();

    [InverseProperty("Task")]
    public virtual ICollection<InventoryRecord> InventoryRecords { get; set; } = new List<InventoryRecord>();

    [ForeignKey("SessionId")]
    [InverseProperty("InventoryTasks")]
    public virtual InventorySession Session { get; set; } = null!;
}