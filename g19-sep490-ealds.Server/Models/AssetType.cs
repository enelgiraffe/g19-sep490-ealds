using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetType")]
public partial class AssetType
{
    [Key]
    public int AssetTypeId { get; set; }

    public int CategoryId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [InverseProperty("AssetType")]
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    [ForeignKey("CategoryId")]
    [InverseProperty("AssetTypes")]
    public virtual AssetCategory Category { get; set; } = null!;

    [InverseProperty("AssetType")]
    public virtual ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();

    [InverseProperty("AssetType")]
    public virtual ICollection<MaintenanceTemplate> MaintenanceTemplates { get; set; } = new List<MaintenanceTemplate>();
}
