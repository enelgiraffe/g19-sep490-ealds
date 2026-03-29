using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("Asset")]
public partial class Asset
{
    [Key]
    public int AssetId { get; set; }

    [StringLength(100)]
    public string Code { get; set; } = null!;

    [StringLength(255)]
    public string Name { get; set; } = null!;

    public int AssetTypeId { get; set; }

    public int Status { get; set; }

    [StringLength(50)]
    public string Unit { get; set; } = null!;

    public int? Quantity { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly? InUseDate { get; set; }

    public string? Specification { get; set; }

    public string? Note { get; set; }

    // Navigation
    [ForeignKey("AssetTypeId")]
    [InverseProperty("Assets")]
    public virtual AssetType AssetType { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    [InverseProperty("Assets")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Asset")]
    public virtual ICollection<AssetInstance> AssetInstances { get; set; } = new List<AssetInstance>();

    [InverseProperty("Asset")]
    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    [InverseProperty("Asset")]
    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
}
