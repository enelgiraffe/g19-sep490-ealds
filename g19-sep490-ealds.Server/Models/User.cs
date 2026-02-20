using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("User")]
[Index("Email", Name = "UQ__User__A9D10534EBCFA9A1", IsUnique = true)]
public partial class User
{
    [Key]
    public int UserId { get; set; }

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string Password { get; set; } = null!;

    public int Status { get; set; }

    [StringLength(255)]
    public string? RefreshToken { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? RefreshTokenExpiryTime { get; set; }

    [StringLength(255)]
    public string? ResetPasswordToken { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ResetPasswordTokenExpiryTime { get; set; }

    [InverseProperty("AcceptedByNavigation")]
    public virtual ICollection<AcceptanceRecord> AcceptanceRecords { get; set; } = new List<AcceptanceRecord>();

    [InverseProperty("ApprovedUser")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [InverseProperty("ActorUser")]
    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    [InverseProperty("ActionByUser")]
    public virtual ICollection<AssetRequestRecord> AssetRequestRecords { get; set; } = new List<AssetRequestRecord>();

    [InverseProperty("User")]
    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    [InverseProperty("ExecutedByNavigation")]
    public virtual ICollection<DiposalRecord> DiposalRecords { get; set; } = new List<DiposalRecord>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Employee> EmployeeCreatedByNavigations { get; set; } = new List<Employee>();

    [InverseProperty("User")]
    public virtual ICollection<Employee> EmployeeUsers { get; set; } = new List<Employee>();

    [InverseProperty("ActualUser")]
    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyActualUsers { get; set; } = new List<InventoryDiscrepancy>();

    [InverseProperty("BookUser")]
    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyBookUsers { get; set; } = new List<InventoryDiscrepancy>();

    [InverseProperty("ActualUser")]
    public virtual ICollection<InventoryRecord> InventoryRecordActualUsers { get; set; } = new List<InventoryRecord>();

    [InverseProperty("CheckedByNavigation")]
    public virtual ICollection<InventoryRecord> InventoryRecordCheckedByNavigations { get; set; } = new List<InventoryRecord>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();

    [InverseProperty("AssignedUser")]
    public virtual ICollection<InventoryTask> InventoryTasks { get; set; } = new List<InventoryTask>();

    [InverseProperty("AssignToNavigation")]
    public virtual ICollection<MaintenaceTask> MaintenaceTaskAssignToNavigations { get; set; } = new List<MaintenaceTask>();

    [InverseProperty("CreateByNavigation")]
    public virtual ICollection<MaintenaceTask> MaintenaceTaskCreateByNavigations { get; set; } = new List<MaintenaceTask>();

    [InverseProperty("CreateByNavigation")]
    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    [InverseProperty("ExecuteByNavigation")]
    public virtual ICollection<TransferRecord> TransferRecordExecuteByNavigations { get; set; } = new List<TransferRecord>();

    [InverseProperty("FromUser")]
    public virtual ICollection<TransferRecord> TransferRecordFromUsers { get; set; } = new List<TransferRecord>();

    [InverseProperty("ToUser")]
    public virtual ICollection<TransferRecord> TransferRecordToUsers { get; set; } = new List<TransferRecord>();
}