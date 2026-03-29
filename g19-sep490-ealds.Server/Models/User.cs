using System;
using System.Collections.Generic;

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

    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    public virtual ICollection<AssetCapitalization> AssetCapitalizations { get; set; } = new List<AssetCapitalization>();

    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    public virtual ICollection<AssetRequest> AssetRequestCreatedByNavigations { get; set; } = new List<AssetRequest>();

    public virtual ICollection<AssetRequestRecord> AssetRequestRecords { get; set; } = new List<AssetRequestRecord>();

    public virtual ICollection<AssetRequest> AssetRequestUsers { get; set; } = new List<AssetRequest>();

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public virtual ICollection<Department> DepartmentCreatedByNavigations { get; set; } = new List<Department>();

    public virtual ICollection<Department> DepartmentUpdatedByNavigations { get; set; } = new List<Department>();

    public virtual ICollection<DisposalRecord> DisposalRecords { get; set; } = new List<DisposalRecord>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<Employee> EmployeeCreatedByNavigations { get; set; } = new List<Employee>();

    public virtual ICollection<Employee> EmployeeUpdatedByNavigations { get; set; } = new List<Employee>();

    public virtual ICollection<Employee> EmployeeUsers { get; set; } = new List<Employee>();

    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyActualUsers { get; set; } = new List<InventoryDiscrepancy>();

    public virtual ICollection<InventoryDiscrepancy> InventoryDiscrepancyBookUsers { get; set; } = new List<InventoryDiscrepancy>();

    public virtual ICollection<InventoryRecord> InventoryRecordActualUsers { get; set; } = new List<InventoryRecord>();

    public virtual ICollection<InventoryRecord> InventoryRecordCheckedByNavigations { get; set; } = new List<InventoryRecord>();

    public virtual ICollection<MaintenanceTask> MaintenanceTaskAssignToNavigations { get; set; } = new List<MaintenanceTask>();

    public virtual ICollection<MaintenanceTask> MaintenanceTaskCreateByNavigations { get; set; } = new List<MaintenanceTask>();

    public virtual ICollection<MaintenanceTask> MaintenanceTaskPerformerUsers { get; set; } = new List<MaintenanceTask>();

    public virtual ICollection<Notification> NotificationRefs { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    public virtual ICollection<Role> RoleCreatedByNavigations { get; set; } = new List<Role>();

    public virtual ICollection<Role> RoleUpdatedByNavigations { get; set; } = new List<Role>();

    public virtual ICollection<TransferRecord> TransferRecordExecutedByNavigations { get; set; } = new List<TransferRecord>();

    public virtual ICollection<TransferRecord> TransferRecordFromUsers { get; set; } = new List<TransferRecord>();

    public virtual ICollection<TransferRecord> TransferRecordToUsers { get; set; } = new List<TransferRecord>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
