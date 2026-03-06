using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class Procurement
{
    public int ProcurementId { get; set; }

    public int AssetRequestId { get; set; }

    public int? SupplierId { get; set; }

    public string ContractNo { get; set; } = null!;

    public DateOnly ContractDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AdvanceAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public int Status { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual ICollection<AcceptanceRecord> AcceptanceRecords { get; set; } = new List<AcceptanceRecord>();

    public virtual AssetRequest AssetRequest { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Supplier? Supplier { get; set; }
}
