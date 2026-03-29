using System;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

public partial class EaldsDbContext : DbContext
{
    public EaldsDbContext() { }

    public EaldsDbContext(DbContextOptions<EaldsDbContext> options) : base(options) { }

    // ── Core ──────────────────────────────────────────────────────────────
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<UserRole> UserRoles { get; set; }
    public virtual DbSet<Department> Departments { get; set; }
    public virtual DbSet<Employee> Employees { get; set; }
    public virtual DbSet<Supplier> Suppliers { get; set; }
    public virtual DbSet<Warehouse> Warehouses { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }

    // ── Asset & AssetInstance ────────────────────────────────────────────
    public virtual DbSet<AssetCategory> AssetCategories { get; set; }
    public virtual DbSet<AssetType> AssetTypes { get; set; }
    public virtual DbSet<Asset> Assets { get; set; }
    public virtual DbSet<AssetInstance> AssetInstances { get; set; }
    public virtual DbSet<Guarantee> Guarantees { get; set; }
    public virtual DbSet<AssetLocation> AssetLocations { get; set; }
    public virtual DbSet<AssetUsage> AssetUsages { get; set; }
    public virtual DbSet<AssetLifeCycle> AssetLifeCycles { get; set; }
    public virtual DbSet<AssetCapitalization> AssetCapitalizations { get; set; }
    public virtual DbSet<AssetRevaluation> AssetRevaluations { get; set; }

    // ── Workflow & Requests ──────────────────────────────────────────────
    public virtual DbSet<Workflow> Workflows { get; set; }
    public virtual DbSet<RequestType> RequestTypes { get; set; }
    public virtual DbSet<WorkflowStep> WorkflowSteps { get; set; }
    public virtual DbSet<AssetRequest> AssetRequests { get; set; }
    public virtual DbSet<AssetRequestRecord> AssetRequestRecords { get; set; }
    public virtual DbSet<Approval> Approvals { get; set; }

    // ── Maintenance & Repair ─────────────────────────────────────────────
    public virtual DbSet<MaintenanceTemplate> MaintenanceTemplates { get; set; }
    public virtual DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
    public virtual DbSet<MaintenanceTask> MaintenanceTasks { get; set; }
    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
    public virtual DbSet<RepairTask> RepairTasks { get; set; }
    public virtual DbSet<RepairRecord> RepairRecords { get; set; }

    // ── Transfer, Disposal, Depreciation ──────────────────────────────────
    public virtual DbSet<TransferRecord> TransferRecords { get; set; }
    public virtual DbSet<DisposalRecord> DisposalRecords { get; set; }
    public virtual DbSet<DepreciationPolicy> DepreciationPolicies { get; set; }
    public virtual DbSet<DepreciationRecord> DepreciationRecords { get; set; }

    // ── Procurement & Inventory ───────────────────────────────────────────
    public virtual DbSet<Procurement> Procurements { get; set; }
    public virtual DbSet<AcceptanceRecord> AcceptanceRecords { get; set; }
    public virtual DbSet<Document> Documents { get; set; }
    public virtual DbSet<InventorySession> InventorySessions { get; set; }
    public virtual DbSet<InventoryTask> InventoryTasks { get; set; }
    public virtual DbSet<InventoryRecord> InventoryRecords { get; set; }
    public virtual DbSet<InventoryDiscrepancy> InventoryDiscrepancies { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── User / Role ────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("User");
            e.HasKey(x => x.UserId);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Password).HasMaxLength(255);
            e.Property(x => x.RefreshToken).HasMaxLength(255);
            e.Property(x => x.ResetPasswordToken).HasMaxLength(255);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Role");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.CreateDate).HasColumnType("datetime");
            e.Property(x => x.UpdateDate).HasColumnType("datetime");
            e.HasOne(x => x.CreatedByNavigation).WithMany(p => p.Roles)
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("UserRole");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(p => p.UserRoles)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── Department / Employee ──────────────────────────────────────────
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("Department");
            e.HasKey(x => x.DepartmentId);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.Property(x => x.UpdateDate).HasColumnType("datetime");
            e.HasOne(x => x.CreatedByNavigation).WithMany(p => p.Departments)
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("Employee");
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Address).HasMaxLength(255);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.Property(x => x.UpdateDate).HasColumnType("datetime");
            e.HasOne(x => x.User).WithMany(p => p.EmployeeUsers)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Department).WithMany(p => p.Employees)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.CreatedByNavigation).WithMany(p => p.EmployeeCreatedByNavigations)
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── Supplier / Warehouse ───────────────────────────────────────────
        modelBuilder.Entity<Supplier>(e =>
        {
            e.ToTable("Supplier");
            e.HasKey(x => x.SupplierId);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.TaxCode).HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
        });

        modelBuilder.Entity<Warehouse>(e =>
        {
            e.ToTable("Warehouse");
            e.HasKey(x => x.WarehouseId);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Location).HasMaxLength(500);
        });

        // ── AssetCategory / AssetType / Asset / AssetInstance / Guarantee ──
        modelBuilder.Entity<AssetCategory>(e =>
        {
            e.ToTable("AssetCategory");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<AssetType>(e =>
        {
            e.ToTable("AssetType");
            e.HasKey(x => x.AssetTypeId);
            e.Property(x => x.Name).HasMaxLength(255);
            e.HasOne(x => x.Category).WithMany(p => p.AssetTypes)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Asset>(e =>
        {
            e.ToTable("Asset");
            e.HasKey(x => x.AssetId);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(100);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Unit).HasMaxLength(50);
            e.HasOne(x => x.AssetType).WithMany(p => p.Assets)
                .HasForeignKey(x => x.AssetTypeId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.CreatedByNavigation).WithMany(p => p.Assets)
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AssetInstance>(e =>
        {
            e.ToTable("AssetInstance");
            e.HasKey(x => x.AssetInstanceId);
            e.HasIndex(x => x.InstanceCode).IsUnique();
            e.Property(x => x.InstanceCode).HasMaxLength(100);
            e.Property(x => x.SerialNumber).HasMaxLength(100);
            e.Property(x => x.ContractNo).HasMaxLength(100);
            e.Property(x => x.Condition).HasMaxLength(255);
            e.Property(x => x.OriginalPrice).HasColumnType("decimal(18, 2)");
            e.Property(x => x.CurrentValue).HasColumnType("decimal(18, 2)");
            e.HasOne(x => x.Asset).WithMany(p => p.AssetInstances)
                .HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Warehouse).WithMany(p => p.AssetInstances)
                .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.DepreciationPolicy).WithMany(p => p.AssetInstances)
                .HasForeignKey(x => x.DepreciationPolicyId);
            e.HasOne(x => x.Supplier).WithMany(p => p.AssetInstances)
                .HasForeignKey(x => x.SupplierId);
        });

        modelBuilder.Entity<Guarantee>(e =>
        {
            e.ToTable("Guarantee");
            e.HasKey(x => x.GuaranteeId);
            e.Property(x => x.WarrantyPeriodUnit).HasMaxLength(20);
            e.HasOne(x => x.AssetInstance).WithMany(p => p.Guarantees)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── AssetLocation / AssetUsage ─────────────────────────────────────
        modelBuilder.Entity<AssetLocation>(e =>
        {
            e.ToTable("AssetLocation");
            e.HasKey(x => x.LocationId);
            e.Property(x => x.Note).HasMaxLength(255);
            e.HasOne(x => x.AssetInstance).WithMany(p => p.AssetLocations)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Department).WithMany(p => p.AssetLocations)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AssetUsage>(e =>
        {
            e.ToTable("AssetUsage");
            e.HasKey(x => x.UsageId);
            e.Property(x => x.Note).HasMaxLength(255);
            e.HasOne(x => x.AssetInstance).WithMany(p => p.AssetUsages)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Employee).WithMany(p => p.AssetUsages)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── AssetLifeCycle ─────────────────────────────────────────────────
        modelBuilder.Entity<AssetLifeCycle>(e =>
        {
            e.ToTable("AssetLifeCycle");
            e.HasKey(x => x.AuditId);
            e.Property(x => x.OccurredAt).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.HasOne(x => x.AssetInstance).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ActorUser).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ActorRole).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(x => x.ActorRoleId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── AssetCapitalization / AssetRevaluation ─────────────────────────
        modelBuilder.Entity<AssetCapitalization>(e =>
        {
            e.ToTable("AssetCapitalization");
            e.HasKey(x => x.Id);
            e.Property(x => x.CapitalizedDate).HasColumnType("datetime");
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.AssetInstance).WithMany(p => p.AssetCapitalizations)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.CapitalizedByNavigation).WithMany(p => p.AssetCapitalizations)
                .HasForeignKey(x => x.CapitalizedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AssetRevaluation>(e =>
        {
            e.ToTable("AssetRevaluation");
            e.HasKey(x => x.Id);
            e.Property(x => x.OldValue).HasColumnType("decimal(18, 2)");
            e.Property(x => x.NewValue).HasColumnType("decimal(18, 2)");
            e.Property(x => x.EffectiveDate).HasColumnType("datetime");
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasOne(x => x.AssetInstance).WithMany(p => p.AssetRevaluations)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── Workflow / RequestType / WorkflowStep ──────────────────────────
        modelBuilder.Entity<Workflow>(e =>
        {
            e.ToTable("Workflow");
            e.HasKey(x => x.WorkflowId);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.CreateDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<RequestType>(e =>
        {
            e.ToTable("RequestType");
            e.HasKey(x => x.RequestTypeId);
            e.HasOne(x => x.Workflow).WithMany(p => p.RequestTypes)
                .HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WorkflowStep>(e =>
        {
            e.ToTable("WorkflowStep");
            e.HasKey(x => x.StepId);
            e.HasOne(x => x.Workflow).WithMany(p => p.WorkflowSteps)
                .HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Role).WithMany(p => p.WorkflowSteps)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── AssetRequest / AssetRequestRecord / Approval ───────────────────
        modelBuilder.Entity<AssetRequest>(e =>
        {
            e.ToTable("AssetRequest");
            e.HasKey(x => x.AssetRequestId);
            e.Property(x => x.Title).HasMaxLength(255);
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.Property(x => x.ApproveDate).HasColumnType("datetime");
            e.HasOne(x => x.User).WithMany(p => p.AssetRequests)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssetInstance).WithMany(p => p.AssetRequests)
                .HasForeignKey(x => x.AssetInstanceId);
            e.HasOne(x => x.RequestType).WithMany(p => p.AssetRequests)
                .HasForeignKey(x => x.RequestTypeId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AssetRequestRecord>(e =>
        {
            e.ToTable("AssetRequestRecord");
            e.HasKey(x => x.RecordId);
            e.Property(x => x.OccurredAt).HasColumnType("datetime");
            e.HasOne(x => x.AssetRequest).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(x => x.AssetRequestId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ActionByUser).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(x => x.ActionByUserId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ActionRole).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(x => x.ActionRoleId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Approval>(e =>
        {
            e.ToTable("Approval");
            e.HasKey(x => x.ApprovalId);
            e.Property(x => x.DecisionDate).HasColumnType("datetime");
            e.HasOne(x => x.Step).WithMany(p => p.Approvals)
                .HasForeignKey(x => x.StepId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssetRequest).WithMany(p => p.Approvals)
                .HasForeignKey(x => x.AssetRequestId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ApprovedUser).WithMany(p => p.Approvals)
                .HasForeignKey(x => x.ApprovedUserId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ApprovedRole).WithMany(p => p.Approvals)
                .HasForeignKey(x => x.ApprovedRoleId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── Maintenance ────────────────────────────────────────────────────
        modelBuilder.Entity<MaintenanceTemplate>(e =>
        {
            e.ToTable("MaintenanceTemplate");
            e.HasKey(x => x.TemplateId);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.RepeatIntervalUnit).HasMaxLength(100);
            e.HasOne(x => x.AssetType).WithMany(p => p.MaintenanceTemplates)
                .HasForeignKey(x => x.AssetTypeId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<MaintenanceSchedule>(e =>
        {
            e.ToTable("MaintenanceSchedule");
            e.HasKey(x => x.ScheduleId);
            e.Property(x => x.StartDate).HasColumnType("datetime");
            e.Property(x => x.NextDueDate).HasColumnType("datetime");
            e.Property(x => x.EndDate).HasColumnType("datetime");
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.HasOne(x => x.Template).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Asset).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(x => x.AssetId);
            e.HasOne(x => x.AssetInstance).WithMany()
                .HasForeignKey(x => x.AssetInstanceId);
        });

        modelBuilder.Entity<MaintenanceTask>(e =>
        {
            e.ToTable("MaintenanceTask");
            e.HasKey(x => x.TaskId);
            e.Property(x => x.PlannedDate).HasColumnType("datetime");
            e.Property(x => x.ExpectedCompletionDate).HasColumnType("datetime");
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.HasOne(x => x.AssetInstance).WithMany(p => p.MaintenanceTasks)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssetRequest).WithMany(p => p.MaintenanceTasks)
                .HasForeignKey(x => x.AssetRequestId);
            e.HasOne(x => x.AssignToNavigation).WithMany(p => p.MaintenanceTaskAssignToNavigations)
                .HasForeignKey(x => x.AssignTo).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.PerformerNavigation).WithMany(p => p.MaintenanceTaskPerformerNavigations)
                .HasForeignKey(x => x.PerformerUserId);
            e.HasOne(x => x.CreateByNavigation).WithMany(p => p.MaintenanceTaskCreateByNavigations)
                .HasForeignKey(x => x.CreateBy).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Schedule).WithMany(p => p.MaintenanceTasks)
                .HasForeignKey(x => x.ScheduleId);
        });

        modelBuilder.Entity<MaintenanceRecord>(e =>
        {
            e.ToTable("MaintenanceRecord");
            e.HasKey(x => x.RecordId);
            e.Property(x => x.ExecutionDate).HasColumnType("datetime");
            e.Property(x => x.TotalCost).HasColumnType("decimal(18, 2)");
            e.Property(x => x.PerformedBy).HasMaxLength(255);
            e.HasOne(x => x.Task).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssetInstance).WithMany()
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── Repair ─────────────────────────────────────────────────────────
        modelBuilder.Entity<RepairTask>(e =>
        {
            e.ToTable("RepairTask");
            e.HasKey(x => x.TaskId);
            e.Property(x => x.EstimatedCost).HasColumnType("decimal(18, 2)");
            e.Property(x => x.RepairDate).HasColumnType("datetime");
            e.Property(x => x.ExpectedCompletionDate).HasColumnType("datetime");
            e.HasOne(x => x.AssetInstance).WithMany(p => p.RepairTasks)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssetRequest).WithMany(p => p.RepairTasks)
                .HasForeignKey(x => x.AssetRequestId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<RepairRecord>(e =>
        {
            e.ToTable("RepairRecord");
            e.HasKey(x => x.RecordId);
            e.Property(x => x.ActualCost).HasColumnType("decimal(18, 2)");
            e.Property(x => x.RepairDate).HasColumnType("datetime");
            e.Property(x => x.DamageDate).HasColumnType("datetime");
            e.HasOne(x => x.Task).WithMany(p => p.RepairRecords)
                .HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Supplier).WithMany(p => p.RepairRecords)
                .HasForeignKey(x => x.SupplierId);
        });

        // ── Transfer / Disposal ────────────────────────────────────────────
        modelBuilder.Entity<TransferRecord>(e =>
        {
            e.ToTable("TransferRecord");
            e.HasKey(x => x.TransferId);
            e.Property(x => x.TransferDate).HasColumnType("datetime");
            e.HasOne(x => x.AssetInstance).WithMany(p => p.TransferRecords)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssetRequest).WithMany(p => p.TransferRecords)
                .HasForeignKey(x => x.AssetRequestId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.FromLocation).WithMany(p => p.TransferRecordsFrom)
                .HasForeignKey(x => x.FromLocationId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ToLocation).WithMany(p => p.TransferRecordsTo)
                .HasForeignKey(x => x.ToLocationId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.FromUser).WithMany(p => p.TransferRecordFromUsers)
                .HasForeignKey(x => x.FromUserId);
            e.HasOne(x => x.ToUser).WithMany(p => p.TransferRecordToUsers)
                .HasForeignKey(x => x.ToUserId);
            e.HasOne(x => x.ExecutedByNavigation).WithMany(p => p.TransferRecordExecutors)
                .HasForeignKey(x => x.ExecutedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<DisposalRecord>(e =>
        {
            e.ToTable("DisposalRecord");
            e.HasKey(x => x.DiposalId);
            e.Property(x => x.DiposalValue).HasColumnType("decimal(18, 2)");
            e.Property(x => x.DiposalDate).HasColumnType("datetime");
            e.HasOne(x => x.AssetInstance).WithMany(p => p.DisposalRecords)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssetRequest).WithMany(p => p.DisposalRecords)
                .HasForeignKey(x => x.AssetRequestId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ExecutedByNavigation).WithMany(p => p.DisposalRecords)
                .HasForeignKey(x => x.ExecutedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── Depreciation ───────────────────────────────────────────────────
        modelBuilder.Entity<DepreciationPolicy>(e =>
        {
            e.ToTable("DepreciationPolicy");
            e.HasKey(x => x.PolicyId);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.SalvageValue).HasColumnType("decimal(18, 2)");
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
        });

        modelBuilder.Entity<DepreciationRecord>(e =>
        {
            e.ToTable("DepreciationRecord");
            e.HasKey(x => x.RecordId);
            e.Property(x => x.DepreciationAmount).HasColumnType("decimal(18, 2)");
            e.Property(x => x.OriginalValue).HasColumnType("decimal(18, 2)");
            e.Property(x => x.RemainingValue).HasColumnType("decimal(18, 2)");
            e.Property(x => x.AccumulatedDepreciation).HasColumnType("decimal(18, 2)");
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.HasOne(x => x.AssetInstance).WithMany(p => p.DepreciationRecords)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Policy).WithMany(p => p.DepreciationRecords)
                .HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        // ── Procurement / Acceptance / Document ────────────────────────────
        modelBuilder.Entity<Procurement>(e =>
        {
            e.ToTable("Procurement");
            e.HasKey(x => x.ProcurementId);
            e.Property(x => x.ContractNo).HasMaxLength(100);
            e.Property(x => x.Title).HasMaxLength(255);
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18, 2)");
            e.Property(x => x.AdvanceAmount).HasColumnType("decimal(18, 2)");
            e.Property(x => x.RemainingAmount).HasColumnType("decimal(18, 2)");
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.HasOne(x => x.AssetRequest).WithMany(p => p.Procurements)
                .HasForeignKey(x => x.AssetRequestId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Supplier).WithMany(p => p.Procurements)
                .HasForeignKey(x => x.SupplierId);
            e.HasOne(x => x.CreatedByNavigation).WithMany(p => p.Procurements)
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AcceptanceRecord>(e =>
        {
            e.ToTable("AcceptanceRecord");
            e.HasKey(x => x.AcceptanceId);
            e.Property(x => x.AcceptanceDate).HasColumnType("datetime");
            e.Property(x => x.TrialStartDate).HasColumnType("datetime");
            e.Property(x => x.TrialEndDate).HasColumnType("datetime");
            e.HasOne(x => x.Procurement).WithMany(p => p.AcceptanceRecords)
                .HasForeignKey(x => x.ProcurementId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("Document");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.FileUrl).HasMaxLength(500);
            e.Property(x => x.UploadedDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.HasOne(x => x.Procurement).WithMany(p => p.Documents)
                .HasForeignKey(x => x.ProcurementId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.UploadedByNavigation).WithMany(p => p.Documents)
                .HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Asset).WithMany()
                .HasForeignKey(x => x.AssetId);
            e.HasOne(x => x.AssetInstance).WithMany()
                .HasForeignKey(x => x.AssetInstanceId);
        });

        // ── Inventory ──────────────────────────────────────────────────────
        modelBuilder.Entity<InventorySession>(e =>
        {
            e.ToTable("InventorySession");
            e.HasKey(x => x.SessionId);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Purpose).HasMaxLength(200);
            e.Property(x => x.StartDate).HasColumnType("datetime");
            e.Property(x => x.EndDate).HasColumnType("datetime");
            e.Property(x => x.CreateDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
            e.HasOne(x => x.Department).WithMany(p => p.InventorySessions)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssetCategory).WithMany(p => p.InventorySessions)
                .HasForeignKey(x => x.AssetCategoryId);
            e.HasOne(x => x.AssetType).WithMany(p => p.InventorySessions)
                .HasForeignKey(x => x.AssetTypeId);
        });

        modelBuilder.Entity<InventoryTask>(e =>
        {
            e.ToTable("InventoryTask");
            e.HasKey(x => x.TaskId);
            e.Property(x => x.CheckDate).HasColumnType("datetime");
            e.HasOne(x => x.AssetInstance).WithMany(p => p.InventoryTasks)
                .HasForeignKey(x => x.AssetInstanceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.AssignedUser).WithMany(p => p.InventoryTasks)
                .HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.Department).WithMany(p => p.InventoryTasks)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<InventoryRecord>(e =>
        {
            e.ToTable("InventoryRecord");
            e.HasKey(x => x.RecordId);
            e.Property(x => x.CheckedDate).HasColumnType("datetime");
            e.Property(x => x.DateCheckCompleted).HasColumnType("datetime");
            e.HasOne(x => x.Task).WithMany(p => p.InventoryRecords)
                .HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ActualLocation).WithMany(p => p.InventoryRecords)
                .HasForeignKey(x => x.ActualLocationId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ActualUser).WithMany(p => p.InventoryRecordActualUsers)
                .HasForeignKey(x => x.ActualUserId);
            e.HasOne(x => x.CheckedByNavigation).WithMany(p => p.InventoryRecordCheckedByNavigations)
                .HasForeignKey(x => x.CheckedBy).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<InventoryDiscrepancy>(e =>
        {
            e.ToTable("InventoryDiscrepancy");
            e.HasKey(x => x.DiscrepancyId);
            e.Property(x => x.BookValue).HasColumnType("decimal(18, 2)");
            e.Property(x => x.ActualValue).HasColumnType("decimal(18, 2)");
            e.HasOne(x => x.Task).WithMany(p => p.InventoryDiscrepancies)
                .HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.BookLocation).WithMany(p => p.InventoryDiscrepancyBookLocations)
                .HasForeignKey(x => x.BookLocationId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.BookUser).WithMany(p => p.InventoryDiscrepancyBookUsers)
                .HasForeignKey(x => x.BookUserId);
            e.HasOne(x => x.ActualLocation).WithMany(p => p.InventoryDiscrepancyActualLocations)
                .HasForeignKey(x => x.ActualLocationId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(x => x.ActualUser).WithMany(p => p.InventoryDiscrepancyActualUsers)
                .HasForeignKey(x => x.ActualUserId);
        });

        // ── Notification ───────────────────────────────────────────────────
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("Notification");
            e.HasKey(x => x.NotificationId);
            e.Property(x => x.Title).HasMaxLength(255);
            e.Property(x => x.Content).HasMaxLength(100);
            e.Property(x => x.SentDate).HasColumnType("datetime").HasDefaultValueSql("getdate()");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
