using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("Workflow")]
public partial class Workflow
{
    [Key]
    public int WorkflowId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    public bool IsActive { get; set; }

    [InverseProperty("Workflow")]
    public virtual ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();

    [InverseProperty("Workflow")]
    public virtual ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
}