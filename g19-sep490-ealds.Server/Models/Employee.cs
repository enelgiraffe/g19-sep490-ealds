using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g19_sep490_ealds.Server.Models;

[Table("Employee")]
public partial class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    public int DepartmentId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string Code { get; set; } = null!;

    public DateOnly? Dob { get; set; }

    public int? Gender { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    public int Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdateDate { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<AssetUsage> AssetUsages { get; set; } = new List<AssetUsage>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("EmployeeCreatedByNavigations")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [ForeignKey("DepartmentId")]
    [InverseProperty("Employees")]
    public virtual Department Department { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("EmployeeUsers")]
    public virtual User User { get; set; } = null!;
}