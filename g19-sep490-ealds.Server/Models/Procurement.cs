using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("Procurement")]
public partial class Procurement
{
    [Key]
    public int ProcurementId { get; set; }

    public int AssetRequestId { get; set; }

    public int? SupplierId { get; set; }

    [StringLength(100)]
    public string ContractNo { get; set; } = null!;

    public DateOnly ContractDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AdvanceAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RemainingAmount { get; set; }

    public int Status { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [InverseProperty("Procurement")]
    public virtual ICollection<AcceptanceRecord> AcceptanceRecords { get; set; } = new List<AcceptanceRecord>();

    [ForeignKey("AssetRequestId")]
    [InverseProperty("Procurements")]
    public virtual AssetRequest AssetRequest { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    [InverseProperty("Procurements")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Procurement")]
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    [ForeignKey("SupplierId")]
    [InverseProperty("Procurements")]
    public virtual Supplier? Supplier { get; set; }
}
