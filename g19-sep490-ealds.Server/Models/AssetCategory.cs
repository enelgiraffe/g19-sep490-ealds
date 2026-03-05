using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("AssetCategory")]
public partial class AssetCategory
{
    [Key]
    public int CategoryId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [InverseProperty("Category")]
    public virtual ICollection<AssetType> AssetTypes { get; set; } = new List<AssetType>();

    [InverseProperty("AssetCategory")]
    public virtual ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();
}
