using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

[Table("User")]
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

    [InverseProperty("ApprovedUser")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [InverseProperty("CapitalizedByNavigation")]
    public virtual ICollection<AssetCapitalization> AssetCapitalizations { get; set; } = new List<AssetCapitalization>();

    [InverseProperty("ActorUser")]
    public virtual ICollection<AssetLifeCycle> AssetLifeCycles { get; set; } = new List<AssetLifeCycle>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<AssetRequest> AssetRequestCreatedByNavigations { get; set; } = new List<AssetRequest>();

    [InverseProperty("ActionByUser")]
    public virtual ICollection<AssetRequestRecord> AssetRequestRecords { get; set; } = new List<AssetRequestRecord>();

    [InverseProperty("User")]
    public virtual ICollection<AssetRequest> AssetRequestUsers { get; set; } = new List<AssetRequest>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Department> DepartmentCreatedByNavigations { get; set; } = new List<Department>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<Department> DepartmentUpdatedByNavigations { get; set; } = new List<Department>();

    [InverseProperty("ExecutedByNavigation")]
    public virtual ICollection<DisposalRecord> DisposalRecords { get; set; } = new List<DisposalRecord>();

    [InverseProperty("UploadedByNavigation")]
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Employee> EmployeeCreatedByNavigations { get; set; } = new List<Employee>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<Employee> EmployeeUpdatedByNavigations { get; set; } = new List<Employee>();

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

    [InverseProperty("AssignToNavigation")]
    public virtual ICollection<MaintenanceTask> MaintenanceTaskAssignToNavigations { get; set; } = new List<MaintenanceTask>();

    [InverseProperty("CreateByNavigation")]
    public virtual ICollection<MaintenanceTask> MaintenanceTaskCreateByNavigations { get; set; } = new List<MaintenanceTask>();

    [InverseProperty("PerformerUser")]
    public virtual ICollection<MaintenanceTask> MaintenanceTaskPerformerUsers { get; set; } = new List<MaintenanceTask>();

    [InverseProperty("Ref")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Role> RoleCreatedByNavigations { get; set; } = new List<Role>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<Role> RoleUpdatedByNavigations { get; set; } = new List<Role>();

    [InverseProperty("ExecutedByNavigation")]
    public virtual ICollection<TransferRecord> TransferRecordExecutedByNavigations { get; set; } = new List<TransferRecord>();

    [InverseProperty("FromUser")]
    public virtual ICollection<TransferRecord> TransferRecordFromUsers { get; set; } = new List<TransferRecord>();

    [InverseProperty("ToUser")]
    public virtual ICollection<TransferRecord> TransferRecordToUsers { get; set; } = new List<TransferRecord>();

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
