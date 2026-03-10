using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("RequestType")]
public partial class RequestType
{
    [Key]
    public int RequestTypeId { get; set; }

    public int WorkflowId { get; set; }

    [InverseProperty("RequestType")]
    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    [ForeignKey("WorkflowId")]
    [InverseProperty("RequestTypes")]
    public virtual Workflow Workflow { get; set; } = null!;
}
