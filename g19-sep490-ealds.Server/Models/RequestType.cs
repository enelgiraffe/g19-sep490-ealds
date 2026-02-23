using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

public partial class RequestType
{
    public int RequestTypeId { get; set; }

    public int WorkflowId { get; set; }

    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    public virtual Workflow Workflow { get; set; } = null!;
}
