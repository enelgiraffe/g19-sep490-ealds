using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int Status { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public string? ResetPasswordToken { get; set; }

    public DateTime? ResetPasswordTokenExpiryTime { get; set; }

    public virtual ICollection<AcceptanceRecord> AcceptanceRecords { get; set; } = new List<AcceptanceRecord>();

    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    public virtual ICollection<AssetRequestRecord> AssetRequestRecords { get; set; } = new List<AssetRequestRecord>();

    public virtual ICollection<AssetRequest> AssetRequests { get; set; } = new List<AssetRequest>();

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<DiposalRecord> DiposalRecords { get; set; } = new List<DiposalRecord>();

    public virtual ICollection<Employee> EmployeeCreatedByNavigations { get; set; } = new List<Employee>();

    public virtual ICollection<Employee> EmployeeUsers { get; set; } = new List<Employee>();

    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyActualUsers { get; set; } = new List<InventoryDiscrepancy>();

    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyBookUsers { get; set; } = new List<InventoryDiscrepancy>();

    public virtual ICollection<InventoryRecord> InventoryRecordActualUsers { get; set; } = new List<InventoryRecord>();

    public virtual ICollection<InventoryRecord> InventoryRecordCheckedByNavigations { get; set; } = new List<InventoryRecord>();

    public virtual ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();

    public virtual ICollection<InventoryTask> InventoryTasks { get; set; } = new List<InventoryTask>();

    public virtual ICollection<MaintenaceTask> MaintenaceTaskAssignToNavigations { get; set; } = new List<MaintenaceTask>();

    public virtual ICollection<MaintenaceTask> MaintenaceTaskCreateByNavigations { get; set; } = new List<MaintenaceTask>();

    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();

    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<TransferRecord> TransferRecordExecuteByNavigations { get; set; } = new List<TransferRecord>();

    public virtual ICollection<TransferRecord> TransferRecordFromUsers { get; set; } = new List<TransferRecord>();

    public virtual ICollection<TransferRecord> TransferRecordToUsers { get; set; } = new List<TransferRecord>();
}
