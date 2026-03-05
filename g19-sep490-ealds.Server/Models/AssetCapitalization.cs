using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetCapitalization")]
public partial class AssetCapitalization
{
    [Key]
    public int Id { get; set; }

    public int AssetId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CapitalizedDate { get; set; }

    public int? CapitalizedBy { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("AssetCapitalizations")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("CapitalizedBy")]
    [InverseProperty("AssetCapitalizations")]
    public virtual User? CapitalizedByNavigation { get; set; }
}
