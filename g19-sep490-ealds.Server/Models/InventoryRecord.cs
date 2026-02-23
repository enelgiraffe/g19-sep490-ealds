using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class InventoryRecord
{
    public int RecordId { get; set; }

    public int TaskId { get; set; }

    public int ActualLocationId { get; set; }

    public int? ActualUserId { get; set; }

    public string ActualCondition { get; set; } = null!;

    public bool? IsFound { get; set; }

    public int CheckedBy { get; set; }

    public DateTime CheckedDate { get; set; }

    public DateTime DateCheckCompleted { get; set; }

    public virtual AssetLocation ActualLocation { get; set; } = null!;

    public virtual User? ActualUser { get; set; }

    public virtual User CheckedByNavigation { get; set; } = null!;

    public virtual InventoryTask Task { get; set; } = null!;
}
