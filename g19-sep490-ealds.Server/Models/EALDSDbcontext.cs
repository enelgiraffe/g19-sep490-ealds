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

    public virtual DbSet<AssetLifeCycle> AssetLifeCycles { get; set; }

    public virtual DbSet<AssetLocation> AssetLocations { get; set; }

    public virtual DbSet<AssetRequest> AssetRequests { get; set; }

    public virtual DbSet<AssetRequestRecord> AssetRequestRecords { get; set; }
    public virtual DbSet<AssetRevaluation> AssetRevaluations { get; set; }
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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=EALDS;User=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcceptanceRecord>(entity =>
        {
            entity.HasKey(e => e.AcceptanceId).HasName("PK__Acceptan__747806F673D36463");

            entity.HasOne(d => d.AcceptedByNavigation).WithMany(p => p.AcceptanceRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Acceptanc__Accep__07C12930");

            entity.HasOne(d => d.Procurement).WithMany(p => p.AcceptanceRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Acceptanc__Accep__06CD04F7");
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity.HasKey(e => e.ApprovalId).HasName("PK__Approval__328477F418993968");

            entity.HasOne(d => d.ApprovedRole).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__Approv__68487DD7");

            entity.HasOne(d => d.ApprovedUser).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__Approv__6754599E");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__AssetR__66603565");

            entity.HasOne(d => d.Step).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__StepId__693CA210");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.AssetId).HasName("PK__Asset__4349235243C0B222");

            entity.HasOne(d => d.AssetType).WithMany(p => p.Assets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__AssetType__5BE2A6F2");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Assets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__CreatedBy__5DCAEF64");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Assets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__Warehouse__5CD6CB2B");
        });

        modelBuilder.Entity<AssetCapitalization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetCap__3214EC07FA11F349");

            entity.Property(e => e.CapitalizedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetCapitalizations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetCapi__Asset__6442E2C9");

            entity.HasOne(d => d.CapitalizedByNavigation).WithMany(p => p.AssetCapitalizations).HasConstraintName("FK__AssetCapi__Capit__65370702");
        });

        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__AssetCat__19093A0B6FF27CE5");
        });

        modelBuilder.Entity<AssetLifeCycle>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AssetLif__A17F2398BC04AB43");

            entity.Property(e => e.OccurredAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ActorRole).WithMany(p => p.AssetLifeCycles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Actor__76969D2E");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AssetLifeCycles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Actor__75A278F5");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetLifeCycles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Occur__74AE54BC");
        });

        modelBuilder.Entity<AssetLocation>(entity =>
        {
            entity.HasKey(e => e.LocationId).HasName("PK__AssetLoc__E7FEA497C48D37F1");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLoca__Asset__6FE99F9F");

            entity.HasOne(d => d.Department).WithMany(p => p.AssetLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLoca__Depar__70DDC3D8");
        });

        modelBuilder.Entity<AssetRequest>(entity =>
        {
            entity.HasKey(e => e.AssetRequestId).HasName("PK__AssetReq__0CA9D3848E567F48");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetRequests).HasConstraintName("FK__AssetRequ__Asset__6383C8BA");

            entity.HasOne(d => d.RequestType).WithMany(p => p.AssetRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Reque__628FA481");

            entity.HasOne(d => d.User).WithMany(p => p.AssetRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__UserI__619B8048");
        });

        modelBuilder.Entity<AssetRequestRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__AssetReq__FBDF78E965CB30FB");

            entity.HasOne(d => d.ActionByUser).WithMany(p => p.AssetRequestRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Actio__1AD3FDA4");

            entity.HasOne(d => d.ActionRole).WithMany(p => p.AssetRequestRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Actio__1BC821DD");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.AssetRequestRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Asset__19DFD96B");
        });

        modelBuilder.Entity<AssetType>(entity =>
        {
            entity.HasKey(e => e.AssetTypeId).HasName("PK__AssetTyp__FD33C2C27763F2ED");

            entity.HasOne(d => d.Category).WithMany(p => p.AssetTypes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetType__Categ__5629CD9C");
        });

        modelBuilder.Entity<AssetUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__AssetUsa__29B19720F466E38F");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetUsag__Asset__6C190EBB");

            entity.HasOne(d => d.Employee).WithMany(p => p.AssetUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetUsag__Emplo__6D0D32F4");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED1D16266F");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Departments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Departmen__Updat__4222D4EF");
        });

        modelBuilder.Entity<DepreciationPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__Deprecia__2E1339A41E2A8EBF");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<DiposalRecord>(entity =>
        {
            entity.HasKey(e => e.DiposalId).HasName("PK__DiposalR__EF94B94C016AED96");

            entity.HasOne(d => d.Asset).WithMany(p => p.DiposalRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiposalRe__Asset__123EB7A3");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.DiposalRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiposalRe__Asset__1332DBDC");

            entity.HasOne(d => d.ExecutedByNavigation).WithMany(p => p.DiposalRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiposalRe__Execu__14270015");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__Document__1ABEEF0F6E7AF753");

            entity.Property(e => e.UploadedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Procurement).WithMany(p => p.Documents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document__Procur__03F0984C");
        });

        modelBuilder.Entity<DrepreciationRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Drepreci__FBDF78E9FEF57936");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Asset).WithMany(p => p.DrepreciationRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Dreprecia__Asset__0E6E26BF");

            entity.HasOne(d => d.Policy).WithMany(p => p.DrepreciationRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Dreprecia__Polic__0F624AF8");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F11DDE094D0");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EmployeeCreatedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__Create__47DBAE45");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__Depart__46E78A0C");

            entity.HasOne(d => d.User).WithMany(p => p.EmployeeUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__UserId__45F365D3");
        });

        modelBuilder.Entity<InventoryDiscrepancy>(entity =>
        {
            entity.HasKey(e => e.DiscrepancyId).HasName("PK__Inventor__7462A89230D2D1E8");

            entity.HasOne(d => d.ActualLocation).WithMany(p => p.InventoryDiscrepancyActualLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Actua__55F4C372");

            entity.HasOne(d => d.ActualUser).WithMany(p => p.InventoryDiscrepancyActualUsers).HasConstraintName("FK__Inventory__Actua__57DD0BE4");

            entity.HasOne(d => d.BookLocation).WithMany(p => p.InventoryDiscrepancyBookLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__BookL__55009F39");

            entity.HasOne(d => d.BookUser).WithMany(p => p.InventoryDiscrepancyBookUsers).HasConstraintName("FK__Inventory__BookU__56E8E7AB");

            entity.HasOne(d => d.Task).WithMany(p => p.InventoryDiscrepancies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__TaskI__540C7B00");
        });

        modelBuilder.Entity<InventoryRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Inventor__FBDF78E958ACFF49");

            entity.HasOne(d => d.ActualLocation).WithMany(p => p.InventoryRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Actua__4F47C5E3");

            entity.HasOne(d => d.ActualUser).WithMany(p => p.InventoryRecordActualUsers).HasConstraintName("FK__Inventory__Actua__503BEA1C");

            entity.HasOne(d => d.CheckedByNavigation).WithMany(p => p.InventoryRecordCheckedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Check__51300E55");

            entity.HasOne(d => d.Task).WithMany(p => p.InventoryRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__DateC__4E53A1AA");
        });

        modelBuilder.Entity<InventorySession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Inventor__C9F492903AFE5937");

            entity.HasOne(d => d.AssetCategory).WithMany(p => p.InventorySessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__43D61337");

            entity.HasOne(d => d.AssetType).WithMany(p => p.InventorySessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__44CA3770");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InventorySessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Creat__45BE5BA9");

            entity.HasOne(d => d.Department).WithMany(p => p.InventorySessions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Creat__42E1EEFE");
        });

        modelBuilder.Entity<InventoryTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Inventor__7C6949B10874330F");

            entity.HasOne(d => d.Asset).WithMany(p => p.InventoryTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__498EEC8D");

            entity.HasOne(d => d.AssignedUser).WithMany(p => p.InventoryTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Assig__4B7734FF");

            entity.HasOne(d => d.Department).WithMany(p => p.InventoryTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Depar__4A8310C6");

            entity.HasOne(d => d.Session).WithMany(p => p.InventoryTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Sessi__489AC854");
        });

        modelBuilder.Entity<MaintenaceTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Maintena__7C6949B1A03631EC");

            entity.Property(e => e.CreatDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Asset).WithMany(p => p.MaintenaceTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenac__Creat__282DF8C2");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.MaintenaceTasks).HasConstraintName("FK__Maintenac__Asset__2A164134");

            entity.HasOne(d => d.AssignToNavigation).WithMany(p => p.MaintenaceTaskAssignToNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenac__Assig__2B0A656D");

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.MaintenaceTaskCreateByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenac__Creat__2BFE89A6");

            entity.HasOne(d => d.Schedule).WithMany(p => p.MaintenaceTasks).HasConstraintName("FK__Maintenac__Sched__29221CFB");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Maintena__FBDF78E90EB74F96");

            entity.HasOne(d => d.Task).WithMany(p => p.MaintenanceRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Techn__2EDAF651");
        });

        modelBuilder.Entity<MaintenanceSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Maintena__9C8A5B49B8E6184C");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Asset).WithMany(p => p.MaintenanceSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__236943A5");

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.MaintenanceSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Creat__245D67DE");

            entity.HasOne(d => d.Template).WithMany(p => p.MaintenanceSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Templ__22751F6C");
        });

        modelBuilder.Entity<MaintenanceTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK__Maintena__F87ADD2765E61A08");

            entity.HasOne(d => d.AssetType).WithMany(p => p.MaintenanceTemplates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__IsAct__1EA48E88");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12569C0D78");

            entity.Property(e => e.SentDate).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Procurement>(entity =>
        {
            entity.HasKey(e => e.ProcurementId).HasName("PK__Procurem__95B451EC2D5955D5");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.Procurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Procureme__Asset__7F2BE32F");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Procurements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Procureme__Creat__00200768");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Procurements).HasConstraintName("FK__Procureme__Suppl__7E37BEF6");
        });

        modelBuilder.Entity<RepairRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__RepairRe__FBDF78E900871AFD");

            entity.HasOne(d => d.Supplier).WithMany(p => p.RepairRecords).HasConstraintName("FK__RepairRec__Suppl__3F115E1A");

            entity.HasOne(d => d.Task).WithMany(p => p.RepairRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairRec__TaskI__3E1D39E1");
        });

        modelBuilder.Entity<RepairTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__RepairTa__7C6949B169F4F5D8");

            entity.HasOne(d => d.Asset).WithMany(p => p.RepairTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairTas__Asset__3A4CA8FD");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.RepairTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairTas__Asset__3B40CD36");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasKey(e => e.RequestTypeId).HasName("PK__RequestT__4D328B83A0931F1B");

            entity.HasOne(d => d.Workflow).WithMany(p => p.RequestTypes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RequestTy__Workf__5165187F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1A5719F74C");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Roles).HasConstraintName("FK__Role__UpdatedBy__3B75D760");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666B4B331C1E9");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<TransferRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Transfer__FBDF78E9844F09EE");

            entity.HasOne(d => d.Asset).WithMany(p => p.TransferRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Asset__31B762FC");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.TransferRecords)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Asset__32AB8735");

            entity.HasOne(d => d.ExecuteByNavigation).WithMany(p => p.TransferRecordExecuteByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Execu__37703C52");

            entity.HasOne(d => d.FromLocation).WithMany(p => p.TransferRecordFromLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__FromL__339FAB6E");

            entity.HasOne(d => d.FromUser).WithMany(p => p.TransferRecordFromUsers).HasConstraintName("FK__TransferR__FromU__3587F3E0");

            entity.HasOne(d => d.ToLocation).WithMany(p => p.TransferRecordToLocations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__ToLoc__3493CFA7");

            entity.HasOne(d => d.ToUser).WithMany(p => p.TransferRecordToUsers).HasConstraintName("FK__TransferR__ToUse__367C1819");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4CAAAB0262");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasOne(d => d.Role).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__RoleId__3E52440B");

            entity.HasOne(d => d.User).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__UserId__3D5E1FD2");
        });

        modelBuilder.Entity<WarehouseAsset>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFF97DE7268C");
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.WorkflowId).HasName("PK__Workflow__5704A66AD3F9AEB3");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(e => e.StepId).HasName("PK__Workflow__24343357468CA21F");

            entity.HasOne(d => d.Role).WithMany(p => p.WorkflowSteps)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkflowS__RoleI__4E88ABD4");

            entity.HasOne(d => d.Workflow).WithMany(p => p.WorkflowSteps)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkflowS__Workf__4D94879B");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
