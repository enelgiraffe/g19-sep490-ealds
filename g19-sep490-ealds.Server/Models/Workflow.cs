namespace g19_sep490_ealds.Server.Models;

public partial class Workflow
{
    public int WorkflowId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();

    public virtual ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
}