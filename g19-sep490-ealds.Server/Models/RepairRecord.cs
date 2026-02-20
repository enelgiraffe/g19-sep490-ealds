using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("RepairRecord")]
public partial class RepairRecord
{
    [Key]
    public int RecordId { get; set; }

    public int TaskId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ActualCost { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime RepairDate { get; set; }

    public string Result { get; set; } = null!;

    public int? SupplierId { get; set; }

    [ForeignKey("SupplierId")]
    [InverseProperty("RepairRecords")]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey("TaskId")]
    [InverseProperty("RepairRecords")]
    public virtual RepairTask Task { get; set; } = null!;
}