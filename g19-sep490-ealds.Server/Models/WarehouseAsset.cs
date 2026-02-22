using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class WarehouseAsset
{
    public int WarehouseId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
