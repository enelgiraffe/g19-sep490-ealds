using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class RepairRecord
{
    public int RecordId { get; set; }

    public int TaskId { get; set; }

    public decimal ActualCost { get; set; }

    public DateTime RepairDate { get; set; }

    public string Result { get; set; } = null!;

    public int? SupplierId { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual RepairTask Task { get; set; } = null!;
}
