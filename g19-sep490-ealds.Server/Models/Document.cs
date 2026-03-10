using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("Document")]
public partial class Document
{
    [Key]
    public int DocumentId { get; set; }

    public int ProcurementId { get; set; }

    public int DocumentType { get; set; }

    [StringLength(500)]
    public string FileUrl { get; set; } = null!;

    public int UploadedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UploadedDate { get; set; }

    [ForeignKey("ProcurementId")]
    [InverseProperty("Documents")]
    public virtual Procurement Procurement { get; set; } = null!;
}
