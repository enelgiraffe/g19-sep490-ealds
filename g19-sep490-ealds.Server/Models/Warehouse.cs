using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("Warehouse")]
public partial class Warehouse
{
    [Key]
    public int WarehouseId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Location { get; set; }

    [InverseProperty("Warehouse")]
    public virtual ICollection<AssetInstance> AssetInstances { get; set; } = new List<AssetInstance>();
}
