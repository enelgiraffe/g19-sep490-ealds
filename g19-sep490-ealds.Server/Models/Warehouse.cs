namespace g19_sep490_ealds.Server.Models;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string Name { get; set; } = null!;

    public string? Location { get; set; }

    public virtual ICollection<AssetInstance> AssetInstances { get; set; } = new List<AssetInstance>();
}