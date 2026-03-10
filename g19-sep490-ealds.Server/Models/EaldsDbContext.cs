using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

public partial class EaldsDbContext : 
    DbContext
{
    public EaldsDbContext()
    {
    }

    public EaldsDbContext(DbContextOptions<EaldsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcceptanceRecord> AcceptanceRecords { get; set; }

    public virtual DbSet<Approval> Approvals { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<AssetCapitalization> AssetCapitalizations { get; set; }

    public virtual DbSet<AssetCategory> AssetCategories { get; set; }

    public virtual DbSet<AssetLifeCycle> AssetLifeCycles { get; set; }

    public virtual DbSet<AssetLocation> AssetLocations { get; set; }

    public virtual DbSet<AssetRequest> AssetRequests { get; set; }

    public virtual DbSet<AssetRequestRecord> AssetRequestRecords { get; set; }

    public virtual DbSet<AssetType> AssetTypes { get; set; }

    public virtual DbSet<AssetUsage> AssetUsages { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<DepreciationPolicy> DepreciationPolicies { get; set; }

    public virtual DbSet<DiposalRecord> DiposalRecords { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DrepreciationRecord> DrepreciationRecords { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<InventoryDiscrepancy> InventoryDiscrepancies { get; set; }

    public virtual DbSet<InventoryRecord> InventoryRecords { get; set; }

    public virtual DbSet<InventorySession> InventorySessions { get; set; }

    public virtual DbSet<InventoryTask> InventoryTasks { get; set; }

    public virtual DbSet<MaintenaceTask> MaintenaceTasks { get; set; }

    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    public virtual DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }

    public virtual DbSet<MaintenanceTemplate> MaintenanceTemplates { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Procurement> Procurements { get; set; }

    public virtual DbSet<RepairRecord> RepairRecords { get; set; }

    public virtual DbSet<RepairTask> RepairTasks { get; set; }

    public virtual DbSet<RequestType> RequestTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<TransferRecord> TransferRecords { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<WarehouseAsset> WarehouseAssets { get; set; }

    public virtual DbSet<Workflow> Workflows { get; set; }

    public virtual DbSet<WorkflowStep> WorkflowSteps { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcceptanceRecord>(entity =>
        {
            entity.HasKey(e => e.AcceptanceId).HasName("PK__Acceptan__747806F6B4C75151");

            entity.ToTable("AcceptanceRecord");

            entity.Property(e => e.AcceptanceDate).HasColumnType("datetime");
            entity.Property(e => e.TrialEndDate).HasColumnType("datetime");
            entity.Property(e => e.TrialStartDate).HasColumnType("datetime");

            entity.HasOne(d => d.AcceptedByNavigation).WithMany(p => p.AcceptanceRecords)
                .HasForeignKey(d => d.AcceptedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Acceptanc__Accep__07C12930");

            entity.HasOne(d => d.Procurement).WithMany(p => p.AcceptanceRecords)
                .HasForeignKey(d => d.ProcurementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Acceptanc__Accep__06CD04F7");
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity.HasKey(e => e.ApprovalId).HasName("PK__Approval__328477F426B63B6B");

            entity.ToTable("Approval");

            entity.Property(e => e.DecisionDate).HasColumnType("datetime");

            entity.HasOne(d => d.ApprovedRole).WithMany(p => p.Approvals)
                .HasForeignKey(d => d.ApprovedRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__Approv__68487DD7");

            entity.HasOne(d => d.ApprovedUser).WithMany(p => p.Approvals)
                .HasForeignKey(d => d.ApprovedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__Approv__6754599E");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.Approvals)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__AssetR__66603565");

            entity.HasOne(d => d.Step).WithMany(p => p.Approvals)
                .HasForeignKey(d => d.StepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__StepId__693CA210");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.AssetId).HasName("PK__Asset__43492352E67F3D08");

            entity.ToTable("Asset");

            entity.HasIndex(e => e.Code, "UQ__Asset__A25C5AA73764417A").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.CurrentValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Unit).HasMaxLength(50);

            entity.HasOne(d => d.AssetType).WithMany(p => p.Assets)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__AssetType__5BE2A6F2");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Assets)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__CreatedBy__5DCAEF64");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Assets)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__Warehouse__5CD6CB2B");
        });

        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__AssetCat__19093A0BBD9AABBB");

            entity.ToTable("AssetCategory");

            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<AssetLifeCycle>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AssetLif__A17F2398D8AAD078");

            entity.ToTable("AssetLifeCycle");

            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.ActorRole).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(d => d.ActorRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Actor__76969D2E");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(d => d.ActorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Actor__75A278F5");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Occur__74AE54BC");
        });

        modelBuilder.Entity<AssetLocation>(entity =>
        {
            entity.HasKey(e => e.LocationId).HasName("PK__AssetLoc__E7FEA497FCEA8382");

            entity.ToTable("AssetLocation");

            entity.Property(e => e.Note).HasMaxLength(255);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetLocations)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLoca__Asset__6FE99F9F");

            entity.HasOne(d => d.Department).WithMany(p => p.AssetLocations)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLoca__Depar__70DDC3D8");
        });

        modelBuilder.Entity<AssetRequest>(entity =>
        {
            entity.HasKey(e => e.AssetRequestId).HasName("PK__AssetReq__0CA9D3840B64CC53");

            entity.ToTable("AssetRequest");

            entity.Property(e => e.ApproveDate).HasColumnType("datetime");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetRequests)
                .HasForeignKey(d => d.AssetId)
                .HasConstraintName("FK__AssetRequ__Asset__6383C8BA");

            entity.HasOne(d => d.RequestType).WithMany(p => p.AssetRequests)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Reque__628FA481");

            entity.HasOne(d => d.User).WithMany(p => p.AssetRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__UserI__619B8048");
        });

        modelBuilder.Entity<AssetRequestRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__AssetReq__FBDF78E9FDA92C09");

            entity.ToTable("AssetRequestRecord");

            entity.Property(e => e.OccurredAt).HasColumnType("datetime");

            entity.HasOne(d => d.ActionByUser).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(d => d.ActionByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Actio__1AD3FDA4");

            entity.HasOne(d => d.ActionRole).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(d => d.ActionRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Actio__1BC821DD");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Asset__19DFD96B");
        });

        modelBuilder.Entity<AssetType>(entity =>
        {
            entity.HasKey(e => e.AssetTypeId).HasName("PK__AssetTyp__FD33C2C25C7997D4");

            entity.ToTable("AssetType");

            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.Category).WithMany(p => p.AssetTypes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetType__Categ__5629CD9C");
        });

        modelBuilder.Entity<AssetUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__AssetUsa__29B197202990F957");

            entity.ToTable("AssetUsage");

            entity.Property(e => e.Note).HasMaxLength(255);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetUsages)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetUsag__Asset__6C190EBB");

            entity.HasOne(d => d.Employee).WithMany(p => p.AssetUsages)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetUsag__Emplo__6D0D32F4");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED77FCD9A8");

            entity.ToTable("Department");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Departments)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Departmen__Updat__4222D4EF");
        });

        modelBuilder.Entity<DepreciationPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__Deprecia__2E1339A4DBD75EB5");

            entity.ToTable("DepreciationPolicy");

            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.SalvageValue).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<DiposalRecord>(entity =>
        {
            entity.HasKey(e => e.DiposalId).HasName("PK__DiposalR__EF94B94CAAD0B945");

            entity.ToTable("DiposalRecord");

            entity.Property(e => e.DiposalDate).HasColumnType("datetime");
            entity.Property(e => e.DiposalValue).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Asset).WithMany(p => p.DiposalRecords)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiposalRe__Asset__123EB7A3");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.DiposalRecords)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiposalRe__Asset__1332DBDC");

            entity.HasOne(d => d.ExecutedByNavigation).WithMany(p => p.DiposalRecords)
                .HasForeignKey(d => d.ExecutedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiposalRe__Execu__14270015");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__Document__1ABEEF0FA851738F");

            entity.ToTable("Document");

            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.UploadedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Procurement).WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProcurementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document__Procur__03F0984C");
        });

        modelBuilder.Entity<DrepreciationRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Drepreci__FBDF78E9C90EFE9C");

            entity.ToTable("DrepreciationRecord");

            entity.Property(e => e.AccumulatedDepreciation).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DepreciationAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RemainingValue).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Asset).WithMany(p => p.DrepreciationRecords)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Dreprecia__Asset__0E6E26BF");

            entity.HasOne(d => d.Policy).WithMany(p => p.DrepreciationRecords)
                .HasForeignKey(d => d.PolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Dreprecia__Polic__0F624AF8");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F119EA8424A");

            entity.ToTable("Employee");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EmployeeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__Create__47DBAE45");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__Depart__46E78A0C");

            entity.HasOne(d => d.User).WithMany(p => p.EmployeeUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__UserId__45F365D3");
        });

        modelBuilder.Entity<InventoryDiscrepancy>(entity =>
        {
            entity.HasKey(e => e.DiscrepancyId).HasName("PK__Inventor__7462A89253901749");

            entity.ToTable("InventoryDiscrepancy");

            entity.Property(e => e.ActualValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BookValue).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ActualLocation).WithMany(p => p.InventoryDiscrepancyActualLocations)
                .HasForeignKey(d => d.ActualLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Actua__55F4C372");

            entity.HasOne(d => d.ActualUser).WithMany(p => p.InventoryDiscrepancyActualUsers)
                .HasForeignKey(d => d.ActualUserId)
                .HasConstraintName("FK__Inventory__Actua__57DD0BE4");

            entity.HasOne(d => d.BookLocation).WithMany(p => p.InventoryDiscrepancyBookLocations)
                .HasForeignKey(d => d.BookLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__BookL__55009F39");

            entity.HasOne(d => d.BookUser).WithMany(p => p.InventoryDiscrepancyBookUsers)
                .HasForeignKey(d => d.BookUserId)
                .HasConstraintName("FK__Inventory__BookU__56E8E7AB");

            entity.HasOne(d => d.Task).WithMany(p => p.InventoryDiscrepancies)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__TaskI__540C7B00");
        });

        modelBuilder.Entity<InventoryRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Inventor__FBDF78E9EA25DCF8");

            entity.ToTable("InventoryRecord");

            entity.Property(e => e.CheckedDate).HasColumnType("datetime");
            entity.Property(e => e.DateCheckCompleted).HasColumnType("datetime");

            entity.HasOne(d => d.ActualLocation).WithMany(p => p.InventoryRecords)
                .HasForeignKey(d => d.ActualLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Actua__4F47C5E3");

            entity.HasOne(d => d.ActualUser).WithMany(p => p.InventoryRecordActualUsers)
                .HasForeignKey(d => d.ActualUserId)
                .HasConstraintName("FK__Inventory__Actua__503BEA1C");

            entity.HasOne(d => d.CheckedByNavigation).WithMany(p => p.InventoryRecordCheckedByNavigations)
                .HasForeignKey(d => d.CheckedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Check__51300E55");

            entity.HasOne(d => d.Task).WithMany(p => p.InventoryRecords)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__DateC__4E53A1AA");
        });

        modelBuilder.Entity<InventorySession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Inventor__C9F49290BE3C907A");

            entity.ToTable("InventorySession");

            entity.HasIndex(e => e.Code, "UQ__Inventor__A25C5AA78FAD898D").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Purpose).HasMaxLength(200);
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.AssetCategory).WithMany(p => p.InventorySessions)
                .HasForeignKey(d => d.AssetCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__43D61337");

            entity.HasOne(d => d.AssetType).WithMany(p => p.InventorySessions)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__44CA3770");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InventorySessions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Creat__45BE5BA9");

            entity.HasOne(d => d.Department).WithMany(p => p.InventorySessions)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Creat__42E1EEFE");
        });

        modelBuilder.Entity<InventoryTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Inventor__7C6949B117E656E9");

            entity.ToTable("InventoryTask");

            entity.Property(e => e.CheckDate).HasColumnType("datetime");

            entity.HasOne(d => d.Asset).WithMany(p => p.InventoryTasks)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__498EEC8D");

            entity.HasOne(d => d.AssignedUser).WithMany(p => p.InventoryTasks)
                .HasForeignKey(d => d.AssignedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Assig__4B7734FF");

            entity.HasOne(d => d.Department).WithMany(p => p.InventoryTasks)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Depar__4A8310C6");

            entity.HasOne(d => d.Session).WithMany(p => p.InventoryTasks)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Sessi__489AC854");
        });

        modelBuilder.Entity<MaintenaceTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Maintena__7C6949B1AA925102");

            entity.ToTable("MaintenaceTask");

            entity.Property(e => e.CreatDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PlannedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Asset).WithMany(p => p.MaintenaceTasks)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenac__Creat__282DF8C2");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.MaintenaceTasks)
                .HasForeignKey(d => d.AssetRequestId)
                .HasConstraintName("FK__Maintenac__Asset__2A164134");

            entity.HasOne(d => d.AssignToNavigation).WithMany(p => p.MaintenaceTaskAssignToNavigations)
                .HasForeignKey(d => d.AssignTo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenac__Assig__2B0A656D");

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.MaintenaceTaskCreateByNavigations)
                .HasForeignKey(d => d.CreateBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenac__Creat__2BFE89A6");

            entity.HasOne(d => d.Schedule).WithMany(p => p.MaintenaceTasks)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK__Maintenac__Sched__29221CFB");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Maintena__FBDF78E957EB0F1D");

            entity.ToTable("MaintenanceRecord");

            entity.Property(e => e.ExecutionDate).HasColumnType("datetime");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Task).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Techn__2EDAF651");
        });

        modelBuilder.Entity<MaintenanceSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Maintena__9C8A5B496EFEDE90");

            entity.ToTable("MaintenanceSchedule");

            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.NextDueDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Asset).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__236943A5");

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.CreateBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Creat__245D67DE");

            entity.HasOne(d => d.Template).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Templ__22751F6C");
        });

        modelBuilder.Entity<MaintenanceTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK__Maintena__F87ADD279992E41B");

            entity.ToTable("MaintenanceTemplate");

            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RepeatIntervalUnit).HasMaxLength(100);

            entity.HasOne(d => d.AssetType).WithMany(p => p.MaintenanceTemplates)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__IsAct__1EA48E88");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12625C8C38");

            entity.ToTable("Notification");

            entity.Property(e => e.Content).HasMaxLength(100);
            entity.Property(e => e.SentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<Procurement>(entity =>
        {
            entity.HasKey(e => e.ProcurementId).HasName("PK__Procurem__95B451EC99F6191D");

            entity.ToTable("Procurement");

            entity.Property(e => e.AdvanceAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ContractNo).HasMaxLength(100);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RemainingAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.Procurements)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Procureme__Asset__7F2BE32F");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Procurements)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Procureme__Creat__00200768");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Procurements)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__Procureme__Suppl__7E37BEF6");
        });

        modelBuilder.Entity<RepairRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__RepairRe__FBDF78E9DBAA0F0C");

            entity.ToTable("RepairRecord");

            entity.Property(e => e.ActualCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RepairDate).HasColumnType("datetime");

            entity.HasOne(d => d.Supplier).WithMany(p => p.RepairRecords)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__RepairRec__Suppl__3F115E1A");

            entity.HasOne(d => d.Task).WithMany(p => p.RepairRecords)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairRec__TaskI__3E1D39E1");
        });

        modelBuilder.Entity<RepairTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__RepairTa__7C6949B11CA3EB4B");

            entity.ToTable("RepairTask");

            entity.Property(e => e.EstimatedCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Asset).WithMany(p => p.RepairTasks)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairTas__Asset__3A4CA8FD");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.RepairTasks)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairTas__Asset__3B40CD36");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasKey(e => e.RequestTypeId).HasName("PK__RequestT__4D328B839803B9F3");

            entity.ToTable("RequestType");

            entity.HasOne(d => d.Workflow).WithMany(p => p.RequestTypes)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RequestTy__Workf__5165187F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1ABA6BB0F6");

            entity.ToTable("Role");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Roles)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Role__UpdatedBy__3B75D760");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666B4C484C0AA");

            entity.ToTable("Supplier");

            entity.HasIndex(e => e.Code, "UQ__Supplier__A25C5AA73A268A53").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.TaxCode).HasMaxLength(50);
        });

        modelBuilder.Entity<TransferRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Transfer__FBDF78E93D1B7772");

            entity.ToTable("TransferRecord");

            entity.Property(e => e.TransferDate).HasColumnType("datetime");

            entity.HasOne(d => d.Asset).WithMany(p => p.TransferRecords)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Asset__31B762FC");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.TransferRecords)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Asset__32AB8735");

            entity.HasOne(d => d.ExecuteByNavigation).WithMany(p => p.TransferRecordExecuteByNavigations)
                .HasForeignKey(d => d.ExecuteBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Execu__37703C52");

            entity.HasOne(d => d.FromLocation).WithMany(p => p.TransferRecordFromLocations)
                .HasForeignKey(d => d.FromLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__FromL__339FAB6E");

            entity.HasOne(d => d.FromUser).WithMany(p => p.TransferRecordFromUsers)
                .HasForeignKey(d => d.FromUserId)
                .HasConstraintName("FK__TransferR__FromU__3587F3E0");

            entity.HasOne(d => d.ToLocation).WithMany(p => p.TransferRecordToLocations)
                .HasForeignKey(d => d.ToLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__ToLoc__3493CFA7");

            entity.HasOne(d => d.ToUser).WithMany(p => p.TransferRecordToUsers)
                .HasForeignKey(d => d.ToUserId)
                .HasConstraintName("FK__TransferR__ToUse__367C1819");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4CD9659AAB");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__A9D10534A0864F67").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.RefreshToken).HasMaxLength(255);
            entity.Property(e => e.RefreshTokenExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.ResetPasswordToken).HasMaxLength(255);
            entity.Property(e => e.ResetPasswordTokenExpiryTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UserRole");

            entity.HasOne(d => d.Role).WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__RoleId__3E52440B");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__UserId__3D5E1FD2");
        });

        modelBuilder.Entity<WarehouseAsset>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFF9C170F644");

            entity.ToTable("WarehouseAsset");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.WorkflowId).HasName("PK__Workflow__5704A66A5F7ECD21");

            entity.ToTable("Workflow");

            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(e => e.StepId).HasName("PK__Workflow__24343357E49AFA53");

            entity.ToTable("WorkflowStep");

            entity.HasOne(d => d.Role).WithMany(p => p.WorkflowSteps)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkflowS__RoleI__4E88ABD4");

            entity.HasOne(d => d.Workflow).WithMany(p => p.WorkflowSteps)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkflowS__Workf__4D94879B");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
