namespace g19_sep490_ealds.Server.Models;

public partial class AssetCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<AssetType> AssetTypes { get; set; } = new List<AssetType>();

    public virtual ICollection<BudgetAllocation> BudgetAllocations { get; set; } = new List<BudgetAllocation>();

    public virtual ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();
}
