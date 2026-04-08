using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class AssetType
{
    public int AssetTypeId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public virtual AssetCategory Category { get; set; } = null!;

    public virtual ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();

    public virtual ICollection<MaintenanceTemplate> MaintenanceTemplates { get; set; } = new List<MaintenanceTemplate>();

    public virtual ICollection<AssetAllocationOrderLine> AssetAllocationOrderLines { get; set; } = new List<AssetAllocationOrderLine>();
}
