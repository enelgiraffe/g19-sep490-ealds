using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("Document")]
public partial class Document
{
    [Key]
    public int DocumentId { get; set; }

    public int ProcurementId { get; set; }

    [StringLength(500)]
    public string FileUrl { get; set; } = null!;

    public int DocumentType { get; set; }

    public int? AssetId { get; set; }

    public int? AssetInstanceId { get; set; }

    public int UploadedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UploadedDate { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("Documents")]
    public virtual Asset? Asset { get; set; }

    [ForeignKey("AssetInstanceId")]
    [InverseProperty("Documents")]
    public virtual AssetInstance? AssetInstance { get; set; }

    [ForeignKey("ProcurementId")]
    [InverseProperty("Documents")]
    public virtual Procurement Procurement { get; set; } = null!;

    [ForeignKey("UploadedBy")]
    [InverseProperty("Documents")]
    public virtual User UploadedByNavigation { get; set; } = null!;
}