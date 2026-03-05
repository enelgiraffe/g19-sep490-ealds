using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetRequestRecord")]
public partial class AssetRequestRecord
{
    [Key]
    public int RecordId { get; set; }

    public int AssetRequestId { get; set; }

    public int FromStatus { get; set; }

    public int ToStatus { get; set; }

    public int Action { get; set; }

    public int ActionByUserId { get; set; }

    public int ActionRoleId { get; set; }

    public string? Comment { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime OccurredAt { get; set; }

    [ForeignKey("ActionByUserId")]
    [InverseProperty("AssetRequestRecords")]
    public virtual User ActionByUser { get; set; } = null!;

    [ForeignKey("ActionRoleId")]
    [InverseProperty("AssetRequestRecords")]
    public virtual Role ActionRole { get; set; } = null!;

    [ForeignKey("AssetRequestId")]
    [InverseProperty("AssetRequestRecords")]
    public virtual AssetRequest AssetRequest { get; set; } = null!;
}
