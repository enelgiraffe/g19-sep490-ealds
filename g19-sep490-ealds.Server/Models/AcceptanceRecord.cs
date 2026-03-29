using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("AcceptanceRecord")]
public partial class AcceptanceRecord
{
    [Key]
    public int AcceptanceId { get; set; }

    public int ProcurementId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime AcceptanceDate { get; set; }

    public int Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime TrialStartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime TrialEndDate { get; set; }

    public string? Note { get; set; }

    public int AcceptedBy { get; set; }

    [ForeignKey("ProcurementId")]
    [InverseProperty("AcceptanceRecords")]
    public virtual Procurement Procurement { get; set; } = null!;
}