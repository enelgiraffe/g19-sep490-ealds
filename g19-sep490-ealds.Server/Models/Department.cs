using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("Department")]
public partial class Department
{
    [Key]
    public int DepartmentId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string Code { get; set; } = null!;

    public int Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdateDate { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    [InverseProperty("Department")]
    public virtual ICollection<AssetLocation> AssetLocations { get; set; } = new List<AssetLocation>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("Departments")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Department")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    [InverseProperty("Department")]
    public virtual ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();

    [InverseProperty("Department")]
    public virtual ICollection<InventoryTask> InventoryTasks { get; set; } = new List<InventoryTask>();
}