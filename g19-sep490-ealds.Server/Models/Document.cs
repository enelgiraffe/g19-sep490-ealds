using System;
using System.Collections.Generic;

namespace g19_sep490_ealds.Server.Models;

public partial class Document
{
    public int DocumentId { get; set; }

    public int ProcurementId { get; set; }

    public int DocumentType { get; set; }

    public string FileUrl { get; set; } = null!;

    public int UploadedBy { get; set; }

    public DateTime UploadedDate { get; set; }

    public virtual Procurement Procurement { get; set; } = null!;
}
