using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

public partial class EALDSDbcontext : DbContext
{
    public EALDSDbcontext()
    {
    }

    public EALDSDbcontext(DbContextOptions<EALDSDbcontext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcceptanceRecord> AcceptanceRecords { get; set; }

    public virtual DbSet<Approval> Approvals { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<AssetCapitalization> AssetCapitalizations { get; set; }

    public virtual DbSet<AssetCategory> AssetCategories { get; set; }

    public virtual DbSet<AssetInstance> AssetInstances { get; set; }

    public virtual DbSet<AssetLifeCycle> AssetLifeCycles { get; set; }

    public virtual DbSet<AssetLocation> AssetLocations { get; set; }

    public virtual DbSet<AssetRequest> AssetRequests { get; set; }

    public virtual DbSet<AssetRequestRecord> AssetRequestRecords { get; set; }

    public virtual DbSet<AssetRevaluation> AssetRevaluations { get; set; }

    public virtual DbSet<AssetType> AssetTypes { get; set; }

    public virtual DbSet<AssetUsage> AssetUsages { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<DepreciationPolicy> DepreciationPolicies { get; set; }

    public virtual DbSet<DepreciationRecord> DepreciationRecords { get; set; }

    public virtual DbSet<DisposalRecord> DisposalRecords { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Guarantee> Guarantees { get; set; }

    public virtual DbSet<InventoryDiscrepancy> InventoryDiscrepancies { get; set; }

    public virtual DbSet<InventoryRecord> InventoryRecords { get; set; }

    public virtual DbSet<InventorySession> InventorySessions { get; set; }

    public virtual DbSet<InventoryTask> InventoryTasks { get; set; }

    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    public virtual DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }

    public virtual DbSet<MaintenanceTask> MaintenanceTasks { get; set; }

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

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseAsset> WarehouseAssets { get; set; }

    public virtual DbSet<Workflow> Workflows { get; set; }

    public virtual DbSet<WorkflowStep> WorkflowSteps { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=EALDS28032026;User=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcceptanceRecord>(entity =>
        {
            entity.HasKey(e => e.AcceptanceId).HasName("PK__Acceptan__747806F633C2D773");

            entity.HasOne(d => d.Procurement).WithMany(p => p.AcceptanceRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Acceptanc__Procu__489AC854");
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity.HasKey(e => e.ApprovalId).HasName("PK__Approval__328477F487D6EABD");

            entity.HasOne(d => d.ApprovedRole).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__Approv__40F9A68C");

            entity.HasOne(d => d.ApprovedUser).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__Approv__40058253");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__AssetR__3F115E1A");

            entity.HasOne(d => d.Step).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__StepId__3E1D39E1");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.AssetId).HasName("PK__Asset__4349235285CF6243");

            entity.HasOne(d => d.AssetType).WithMany(p => p.Assets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__AssetType__5BE2A6F2");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Assets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__CreatedBy__5CD6CB2B");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Assets).HasConstraintName("FK__Asset__SupplierI__5DCAEF64");
        });

        modelBuilder.Entity<AssetCapitalization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetCap__3214EC07746907F8");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetCapitalizations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetCapi__Asset__1CBC4616");

            entity.HasOne(d => d.CapitalizedByNavigation).WithMany(p => p.AssetCapitalizations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetCapi__Capit__1DB06A4F");
        });

        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__AssetCat__19093A0B7E4791B2");
        });

        modelBuilder.Entity<AssetInstance>(entity =>
        {
            entity.HasKey(e => e.AssetInstanceId).HasName("PK__AssetIns__77D4DF309AEEFA5B");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetInstances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetInst__Asset__619B8048");

            entity.HasOne(d => d.DepreciationPolicy).WithMany(p => p.AssetInstances).HasConstraintName("FK__AssetInst__Depre__6383C8BA");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.AssetInstances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetInst__Wareh__628FA481");
        });

        modelBuilder.Entity<AssetLifeCycle>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AssetLif__A17F2398B0879AC6");

            entity.Property(e => e.OccurredAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ActorRole).WithMany(p => p.AssetLifeCycles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Actor__3B40CD36");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AssetLifeCycles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Actor__3A4CA8FD");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetLifeCycles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Asset__3864608B");
        });

        modelBuilder.Entity<AssetLocation>(entity =>
        {
            entity.HasKey(e => e.LocationId).HasName("PK__AssetLoc__E7FEA49734114CF7");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLoca__Asset__245D67DE");

            entity.HasOne(d => d.Department).WithMany(p => p.AssetLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLoca__Depar__25518C17");
        });

        modelBuilder.Entity<AssetRequest>(entity =>
        {
            entity.HasKey(e => e.AssetRequestId).HasName("PK__AssetReq__0CA9D3845DF75518");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetRequests).HasConstraintName("FK__AssetRequ__Asset__72C60C4A");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AssetRequestCreatedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Creat__75A278F5");

            entity.HasOne(d => d.RequestType).WithMany(p => p.AssetRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Reque__73BA3083");

            entity.HasOne(d => d.Step).WithMany(p => p.AssetRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__StepI__76969D2E");

            entity.HasOne(d => d.User).WithMany(p => p.AssetRequestUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__UserI__71D1E811");
        });

        modelBuilder.Entity<AssetRequestRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__AssetReq__FBDF78E9EBA7EA91");

            entity.HasOne(d => d.ActionByUser).WithMany(p => p.AssetRequestRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Actio__7A672E12");

            entity.HasOne(d => d.ActionRole).WithMany(p => p.AssetRequestRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Actio__7B5B524B");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.AssetRequestRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Asset__797309D9");
        });

        modelBuilder.Entity<AssetRevaluation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetRev__3214EC0739FFD53B");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetRevaluations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetReva__Asset__2180FB33");
        });

        modelBuilder.Entity<AssetType>(entity =>
        {
            entity.HasKey(e => e.AssetTypeId).HasName("PK__AssetTyp__FD33C2C2373E1120");

            entity.HasOne(d => d.Category).WithMany(p => p.AssetTypes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetType__Categ__5535A963");
        });

        modelBuilder.Entity<AssetUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__AssetUsa__29B19720995F700F");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetUsag__Asset__282DF8C2");

            entity.HasOne(d => d.Employee).WithMany(p => p.AssetUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetUsag__Emplo__29221CFB");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED363D87CB");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.DepartmentCreatedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Departmen__Creat__440B1D61");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.DepartmentUpdatedByNavigations).HasConstraintName("FK__Departmen__Updat__44FF419A");
        });

        modelBuilder.Entity<DepreciationPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__Deprecia__2E1339A47B9C5CD9");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<DepreciationRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Deprecia__FBDF78E9870CA18C");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.DepreciationRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Depreciat__Asset__17F790F9");

            entity.HasOne(d => d.Policy).WithMany(p => p.DepreciationRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Depreciat__Polic__18EBB532");
        });

        modelBuilder.Entity<DisposalRecord>(entity =>
        {
            entity.HasKey(e => e.DiposalId).HasName("PK__Disposal__EF94B94C7B537826");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.DisposalRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DisposalR__Asset__339FAB6E");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.DisposalRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DisposalR__Asset__3493CFA7");

            entity.HasOne(d => d.ExecutedByNavigation).WithMany(p => p.DisposalRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DisposalR__Execu__3587F3E0");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__Document__1ABEEF0F3BF4D26F");

            entity.HasOne(d => d.Asset).WithMany(p => p.Documents).HasConstraintName("FK__Document__AssetI__6166761E");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.Documents).HasConstraintName("FK__Document__AssetI__625A9A57");

            entity.HasOne(d => d.Procurement).WithMany(p => p.Documents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document__Procur__607251E5");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Documents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document__Upload__634EBE90");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F117E9D38A2");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EmployeeCreatedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__Create__4AB81AF0");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__Depart__48CFD27E");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.EmployeeUpdatedByNavigations).HasConstraintName("FK__Employee__Update__4BAC3F29");

            entity.HasOne(d => d.User).WithMany(p => p.EmployeeUsers).HasConstraintName("FK__Employee__UserId__47DBAE45");
        });

        modelBuilder.Entity<Guarantee>(entity =>
        {
            entity.HasKey(e => e.GuaranteeId).HasName("PK__Guarante__7EC2C760D05F5C46");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.Guarantees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Guarantee__Asset__66603565");
        });

        modelBuilder.Entity<InventoryDiscrepancy>(entity =>
        {
            entity.HasKey(e => e.DiscrepancyId).HasName("PK__Inventor__7462A892378F5BDA");

            entity.HasOne(d => d.ActualLocation).WithMany(p => p.InventoryDiscrepancyActualLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Actua__56E8E7AB");

            entity.HasOne(d => d.ActualUser).WithMany(p => p.InventoryDiscrepancyActualUsers).HasConstraintName("FK__Inventory__Actua__57DD0BE4");

            entity.HasOne(d => d.BookLocation).WithMany(p => p.InventoryDiscrepancyBookLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__BookL__55009F39");

            entity.HasOne(d => d.BookUser).WithMany(p => p.InventoryDiscrepancyBookUsers).HasConstraintName("FK__Inventory__BookU__55F4C372");

            entity.HasOne(d => d.Task).WithMany(p => p.InventoryDiscrepancies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__TaskI__540C7B00");
        });

        modelBuilder.Entity<InventoryRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Inventor__FBDF78E9A4303F97");

            entity.HasOne(d => d.ActualLocation).WithMany(p => p.InventoryRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Actua__4F47C5E3");

            entity.HasOne(d => d.ActualUser).WithMany(p => p.InventoryRecordActualUsers).HasConstraintName("FK__Inventory__Actua__503BEA1C");

            entity.HasOne(d => d.CheckedByNavigation).WithMany(p => p.InventoryRecordCheckedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Check__51300E55");

            entity.HasOne(d => d.Task).WithMany(p => p.InventoryRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__TaskI__4E53A1AA");
        });

        modelBuilder.Entity<InventorySession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Inventor__C9F49290CBFCBB18");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.AssetCategory).WithMany(p => p.InventorySessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__5BAD9CC8");

            entity.HasOne(d => d.AssetType).WithMany(p => p.InventorySessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__5CA1C101");

            entity.HasOne(d => d.Department).WithMany(p => p.InventorySessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Depar__5AB9788F");
        });

        modelBuilder.Entity<InventoryTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Inventor__7C6949B103EAE78E");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.InventoryTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__4B7734FF");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Maintena__FBDF78E929EACB96");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.MaintenanceRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__0D7A0286");

            entity.HasOne(d => d.Task).WithMany(p => p.MaintenanceRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__TaskI__0C85DE4D");
        });

        modelBuilder.Entity<MaintenanceSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Maintena__9C8A5B49147EF11B");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.MaintenanceSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__01142BA1");

            entity.HasOne(d => d.Template).WithMany(p => p.MaintenanceSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Templ__02084FDA");
        });

        modelBuilder.Entity<MaintenanceTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Maintena__7C6949B1273960AA");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.MaintenanceTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__06CD04F7");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.MaintenanceTasks).HasConstraintName("FK__Maintenan__Asset__05D8E0BE");

            entity.HasOne(d => d.AssignToNavigation).WithMany(p => p.MaintenanceTaskAssignToNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Assig__07C12930");

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.MaintenanceTaskCreateByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Creat__08B54D69");

            entity.HasOne(d => d.PerformerUser).WithMany(p => p.MaintenanceTaskPerformerUsers).HasConstraintName("FK__Maintenan__Perfo__09A971A2");

            entity.HasOne(d => d.Schedule).WithMany(p => p.MaintenanceTasks).HasConstraintName("FK__Maintenan__Sched__04E4BC85");
        });

        modelBuilder.Entity<MaintenanceTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK__Maintena__F87ADD2769633290");

            entity.HasOne(d => d.AssetType).WithMany(p => p.MaintenanceTemplates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__7E37BEF6");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12151A0D3E");

            entity.Property(e => e.SentDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Ref).WithMany(p => p.Notifications).HasConstraintName("FK__Notificat__RefId__662B2B3B");
        });

        modelBuilder.Entity<Procurement>(entity =>
        {
            entity.HasKey(e => e.ProcurementId).HasName("PK__Procurem__95B451EC82BDF1B0");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.Procurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Procureme__Asset__43D61337");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Procurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Procureme__Creat__45BE5BA9");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Procurements).HasConstraintName("FK__Procureme__Suppl__44CA3770");
        });

        modelBuilder.Entity<RepairRecord>(entity =>
        {
            entity.HasKey(e => e.RepairId).HasName("PK__RepairRe__07D0BC2DC44C05D5");

            entity.HasOne(d => d.Supplier).WithMany(p => p.RepairRecords).HasConstraintName("FK__RepairRec__Suppl__151B244E");

            entity.HasOne(d => d.Task).WithMany(p => p.RepairRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairRec__TaskI__14270015");
        });

        modelBuilder.Entity<RepairTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__RepairTa__7C6949B159E5FCCA");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.RepairTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairTas__Asset__114A936A");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.RepairTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairTas__Asset__10566F31");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasKey(e => e.RequestTypeId).HasName("PK__RequestT__4D328B8330B023F9");

            entity.HasOne(d => d.Workflow).WithMany(p => p.RequestTypes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RequestTy__Workf__6B24EA82");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1A243A8463");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RoleCreatedByNavigations).HasConstraintName("FK__Role__CreatedBy__398D8EEE");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.RoleUpdatedByNavigations).HasConstraintName("FK__Role__UpdatedBy__3A81B327");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666B4CF445DCB");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<TransferRecord>(entity =>
        {
            entity.HasKey(e => e.TransferId).HasName("PK__Transfer__954900913A2EC0F6");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.TransferRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Asset__2BFE89A6");

            entity.HasOne(d => d.ExecutedByNavigation).WithMany(p => p.TransferRecordExecutedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Execu__30C33EC3");

            entity.HasOne(d => d.FromLocation).WithMany(p => p.TransferRecordFromLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__FromL__2CF2ADDF");

            entity.HasOne(d => d.FromUser).WithMany(p => p.TransferRecordFromUsers).HasConstraintName("FK__TransferR__FromU__2EDAF651");

            entity.HasOne(d => d.ToLocation).WithMany(p => p.TransferRecordToLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__ToLoc__2DE6D218");

            entity.HasOne(d => d.ToUser).WithMany(p => p.TransferRecordToUsers).HasConstraintName("FK__TransferR__ToUse__2FCF1A8A");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4CFBA5F3B2");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRole__RoleId__3E52440B"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRole__UserId__3D5E1FD2"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF2760AD11BF6099");
                        j.ToTable("UserRole");
                    });
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFF9CEA0CB80");
        });

        modelBuilder.Entity<WarehouseAsset>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFF93F429BC4");
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.WorkflowId).HasName("PK__Workflow__5704A66A97CD3B68");
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(e => e.StepId).HasName("PK__Workflow__2434335722B521B4");

            entity.HasOne(d => d.Role).WithMany(p => p.WorkflowSteps)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkflowS__RoleI__6EF57B66");

            entity.HasOne(d => d.Workflow).WithMany(p => p.WorkflowSteps)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkflowS__Workf__6E01572D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
