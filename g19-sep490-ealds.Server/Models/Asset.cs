using System.ComponentModel.DataAnnotations.Schema;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Models;

public partial class Asset
{
    public int AssetId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int AssetTypeId { get; set; }

    public int Status { get; set; }

    public string Unit { get; set; } = null!;

    public int? Quantity { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly? InUseDate { get; set; }

    public string? Specification { get; set; }

    public string? Note { get; set; }

    public bool IsFixedAsset { get; set; }

    public virtual ICollection<AssetInstance> AssetInstances { get; set; } = new List<AssetInstance>();

    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();

    public virtual ICollection<AssetAllocationOrderLine> AssetAllocationOrderLines { get; set; } = new List<AssetAllocationOrderLine>();

    [NotMapped]
    public AssetStatus StatusEnum
    {
        get => (AssetStatus)Status;
        set => Status = (int)value;
    }
}
