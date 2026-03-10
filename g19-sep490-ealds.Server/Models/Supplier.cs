using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("Supplier")]
[Index("Code", Name = "UQ__Supplier__A25C5AA7B0056715", IsUnique = true)]
public partial class Supplier
{
    [Key]
    public int SupplierId { get; set; }

    [StringLength(50)]
    public string Code { get; set; } = null!;

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(255)]
    public string? Email { get; set; }

    public int Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [InverseProperty("Supplier")]
    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    [InverseProperty("Supplier")]
    public virtual ICollection<RepairRecord> RepairRecords { get; set; } = new List<RepairRecord>();
}
