using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Models;

public partial class EaldsDbContext : DbContext
{
    public EaldsDbContext(DbContextOptions<EaldsDbContext> options)
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

    public virtual DbSet<AssetRequestPurchaseLine> AssetRequestPurchaseLines { get; set; }

    public virtual DbSet<AssetRevaluation> AssetRevaluations { get; set; }

    public virtual DbSet<AssetType> AssetTypes { get; set; }

    public virtual DbSet<AssetUsage> AssetUsages { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<BudgetAllocation> BudgetAllocations { get; set; }

    public virtual DbSet<AssetAllocationOrder> AssetAllocationOrders { get; set; }

    public virtual DbSet<AssetAllocationOrderLine> AssetAllocationOrderLines { get; set; }

    public virtual DbSet<DepreciationPolicy> DepreciationPolicies { get; set; }

    public virtual DbSet<DepreciationRecord> DepreciationRecords { get; set; }

    public virtual DbSet<DisposalRecord> DisposalRecords { get; set; }

    public virtual DbSet<DisposalExecution> DisposalExecutions { get; set; }

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

    public virtual DbSet<ProcurementLine> ProcurementLines { get; set; }

    public virtual DbSet<GoodsReceipt> GoodsReceipts { get; set; }

    public virtual DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; }

    public virtual DbSet<SupplierInvoice> SupplierInvoices { get; set; }

    public virtual DbSet<SupplierInvoiceLine> SupplierInvoiceLines { get; set; }

    public virtual DbSet<RepairRecord> RepairRecords { get; set; }

    public virtual DbSet<RepairTask> RepairTasks { get; set; }

    public virtual DbSet<RequestType> RequestTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<TransferRecord> TransferRecords { get; set; }

    public virtual DbSet<TransferHandoverRecord> TransferHandoverRecords { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseAsset> WarehouseAssets { get; set; }

    public virtual DbSet<Workflow> Workflows { get; set; }

    public virtual DbSet<WorkflowStep> WorkflowSteps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcceptanceRecord>(entity =>
        {
            entity.HasKey(e => e.AcceptanceId).HasName("PK__Acceptan__747806F62C2558E5");

            entity.ToTable("AcceptanceRecord");

            entity.Property(e => e.AcceptanceDate).HasColumnType("datetime");
            entity.Property(e => e.TrialEndDate).HasColumnType("datetime");
            entity.Property(e => e.TrialStartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Procurement).WithMany(p => p.AcceptanceRecords)
                .HasForeignKey(d => d.ProcurementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Acceptanc__Procu__4E53A1AA");
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity.HasKey(e => e.ApprovalId).HasName("PK__Approval__328477F444E54CDB");

            entity.ToTable("Approval");

            entity.Property(e => e.DecisionDate).HasColumnType("datetime");

            entity.HasOne(d => d.ApprovedRole).WithMany(p => p.Approvals)
                .HasForeignKey(d => d.ApprovedRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__Approv__46B27FE2");

            entity.HasOne(d => d.ApprovedUser).WithMany(p => p.Approvals)
                .HasForeignKey(d => d.ApprovedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__Approv__45BE5BA9");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.Approvals)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__AssetR__44CA3770");

            entity.HasOne(d => d.Step).WithMany(p => p.Approvals)
                .HasForeignKey(d => d.StepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approval__StepId__43D61337");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.AssetId).HasName("PK__Asset__434923524F15EE1A");

            entity.ToTable("Asset");

            entity.HasIndex(e => e.Code, "UQ__Asset__A25C5AA7870F74A4").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Unit).HasMaxLength(50);

            entity.HasOne(d => d.AssetType).WithMany(p => p.Assets)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__AssetType__5BE2A6F2");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Assets)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asset__CreatedBy__5CD6CB2B");
        });

        modelBuilder.Entity<AssetCapitalization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetCap__3214EC073EF1F07D");

            entity.ToTable("AssetCapitalization");

            entity.Property(e => e.CapitalizedDate).HasColumnType("datetime");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetCapitalizations)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetCapi__Asset__22751F6C");

            entity.HasOne(d => d.CapitalizedByNavigation).WithMany(p => p.AssetCapitalizations)
                .HasForeignKey(d => d.CapitalizedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetCapi__Capit__236943A5");
        });

        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__AssetCat__19093A0BC0D69447");

            entity.ToTable("AssetCategory");

            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<AssetInstance>(entity =>
        {
            entity.HasKey(e => e.AssetInstanceId).HasName("PK__AssetIns__77D4DF30B2B01E14");

            entity.ToTable("AssetInstance");

            entity.HasIndex(e => e.InstanceCode, "UQ__AssetIns__5850F4DD9DE29C5B").IsUnique();

            entity.Property(e => e.Condition).HasMaxLength(255);
            entity.Property(e => e.ContractNo).HasMaxLength(100);
            entity.Property(e => e.CurrentValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InstanceCode).HasMaxLength(100);
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SerialNumber).HasMaxLength(100);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetInstances)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetInst__Asset__60A75C0F");

            entity.HasOne(d => d.DepreciationPolicy).WithMany(p => p.AssetInstances)
                .HasForeignKey(d => d.DepreciationPolicyId)
                .HasConstraintName("FK__AssetInst__Depre__628FA481");

            entity.HasOne(d => d.Supplier).WithMany(p => p.AssetInstances)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__AssetInst__Suppl__6383C8BA");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.AssetInstances)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetInst__Wareh__619B8048");

            entity.HasOne(d => d.GoodsReceiptLine).WithMany(l => l.AssetInstances)
                .HasForeignKey(d => d.GoodsReceiptLineId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AssetInstance_GoodsReceiptLine");
        });

        modelBuilder.Entity<AssetLifeCycle>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AssetLif__A17F2398E8C47F27");

            entity.ToTable("AssetLifeCycle");

            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.ActorRole).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(d => d.ActorRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Actor__40F9A68C");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(d => d.ActorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Actor__40058253");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetLifeCycles)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLife__Asset__3E1D39E1");
        });

        modelBuilder.Entity<AssetLocation>(entity =>
        {
            entity.HasKey(e => e.LocationId).HasName("PK__AssetLoc__E7FEA497F14528B5");

            entity.ToTable("AssetLocation");

            entity.Property(e => e.Note).HasMaxLength(255);

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetLocations)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLoca__Asset__2A164134");

            entity.HasOne(d => d.Department).WithMany(p => p.AssetLocations)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetLoca__Depar__2B0A656D");
        });

        modelBuilder.Entity<AssetRequest>(entity =>
        {
            entity.HasKey(e => e.AssetRequestId).HasName("PK__AssetReq__0CA9D384149A863E");

            entity.ToTable("AssetRequest");

            entity.Property(e => e.ApproveDate).HasColumnType("datetime");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasIndex(e => e.AllocationTargetDepartmentId);
            entity.HasIndex(e => e.SourcePurchaseRequestId);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetRequests)
                .HasForeignKey(d => d.AssetId)
                .HasConstraintName("FK__AssetRequ__Asset__72C60C4A");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetRequests)
                .HasForeignKey(d => d.AssetInstanceId)
                .HasConstraintName("FK__AssetRequ__AssetInst");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AssetRequestCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Creat__75A278F5");

            entity.HasOne(d => d.RequestType).WithMany(p => p.AssetRequests)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Reque__73BA3083");

            entity.HasOne(d => d.Step).WithMany(p => p.AssetRequests)
                .HasForeignKey(d => d.StepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__StepI__76969D2E");

            entity.HasOne(d => d.User).WithMany(p => p.AssetRequestUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__UserI__71D1E811");

            entity.HasOne(d => d.AssetAllocationOrder)
                .WithOne(p => p.AssetRequest)
                .HasForeignKey<AssetAllocationOrder>(p => p.AssetRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<AssetRequest>()
                .WithMany()
                .HasForeignKey(e => e.SourcePurchaseRequestId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AssetRequestRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__AssetReq__FBDF78E98E724F48");

            entity.ToTable("AssetRequestRecord");

            entity.Property(e => e.OccurredAt).HasColumnType("datetime");

            entity.HasOne(d => d.ActionByUser).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(d => d.ActionByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Actio__7A672E12");

            entity.HasOne(d => d.ActionRole).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(d => d.ActionRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Actio__7B5B524B");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.AssetRequestRecords)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetRequ__Asset__797309D9");
        });

        modelBuilder.Entity<AssetRequestPurchaseLine>(entity =>
        {
            entity.HasKey(e => e.LineId);

            entity.ToTable("AssetRequestPurchaseLine");

            entity.Property(e => e.ItemName).HasMaxLength(500);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.ModelCode).HasMaxLength(100);
            entity.Property(e => e.EstimatedPrice).HasMaxLength(100);
            entity.Property(e => e.CapitalizedAt).HasColumnType("datetime2");

            entity.HasIndex(e => e.AssetRequestId, "IX_ARPL_AssetRequestId");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.PurchaseLines)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Asset).WithMany()
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AssetRevaluation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetRev__3214EC07A8625110");

            entity.ToTable("AssetRevaluation");

            entity.Property(e => e.EffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.NewValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OldValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetRevaluations)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetReva__Asset__2739D489");
        });

        modelBuilder.Entity<AssetType>(entity =>
        {
            entity.HasKey(e => e.AssetTypeId).HasName("PK__AssetTyp__FD33C2C2272E2154");

            entity.ToTable("AssetType");

            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.Category).WithMany(p => p.AssetTypes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetType__Categ__5535A963");
        });

        modelBuilder.Entity<AssetUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__AssetUsa__29B19720AFF0A6B2");

            entity.ToTable("AssetUsage");

            entity.Property(e => e.Note).HasMaxLength(255);

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.AssetUsages)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetUsag__Asset__2DE6D218");

            entity.HasOne(d => d.Employee).WithMany(p => p.AssetUsages)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AssetUsag__Emplo__2EDAF651");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED26A9D39F");

            entity.ToTable("Department");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.DepartmentCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Departmen__Creat__440B1D61");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.DepartmentUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK__Departmen__Updat__44FF419A");
        });

        modelBuilder.Entity<BudgetAllocation>(entity =>
        {
            entity.HasKey(e => e.BudgetAllocationId);

            entity.ToTable("BudgetAllocation");

            entity.Property(e => e.TransactionDate).HasColumnType("date");
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.SubmittedByDisplayName).HasMaxLength(255);
            entity.Property(e => e.Note).HasMaxLength(4000);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2");

            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.AssetInstanceId);
            entity.HasIndex(e => e.AssetCategoryId);
            entity.HasIndex(e => e.SubmittedByUserId);

            entity.HasOne(d => d.Department).WithMany(p => p.BudgetAllocations)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.BudgetAllocations)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.AssetCategory).WithMany(p => p.BudgetAllocations)
                .HasForeignKey(d => d.AssetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SubmittedByUser).WithMany(p => p.BudgetAllocationsSubmitted)
                .HasForeignKey(d => d.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssetAllocationOrder>(entity =>
        {
            entity.HasKey(e => e.AssetAllocationOrderId);
            entity.ToTable("AssetAllocationOrder");
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.Kind).HasConversion<byte>();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2");
            entity.Property(e => e.ConfirmedAt).HasColumnType("datetime2");
            entity.Property(e => e.RequestSubmittedAt).HasColumnType("datetime2");
            entity.HasIndex(e => e.AssetRequestId).IsUnique();
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.RequestedByUserId);

            entity.HasOne(d => d.Department).WithMany(p => p.AssetAllocationOrders)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.RequestedByUser).WithMany()
                .HasForeignKey(d => d.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ConfirmedByUser).WithMany()
                .HasForeignKey(d => d.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssetAllocationOrderLine>(entity =>
        {
            entity.HasKey(e => e.AssetAllocationOrderLineId);
            entity.ToTable("AssetAllocationOrderLine");
            entity.Property(e => e.Reason).HasMaxLength(2000);

            entity.HasOne(d => d.Order).WithMany(p => p.Lines)
                .HasForeignKey(d => d.AssetAllocationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.AssetType).WithMany(p => p.AssetAllocationOrderLines)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetAllocationOrderLines)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DepreciationPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__Deprecia__2E1339A4232D15BE");

            entity.ToTable("DepreciationPolicy");

            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.SalvageValue).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<DepreciationRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Deprecia__FBDF78E9E2167956");

            entity.ToTable("DepreciationRecord");

            entity.Property(e => e.AccumulatedDepreciation).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DepreciationAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OriginalValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RemainingValue).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.DepreciationRecords)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Depreciat__Asset__1CBC4616");

            entity.HasOne(d => d.Policy).WithMany(p => p.DepreciationRecords)
                .HasForeignKey(d => d.PolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Depreciat__Polic__1DB06A4F");
        });

        modelBuilder.Entity<DisposalRecord>(entity =>
        {
            entity.HasKey(e => e.DiposalId).HasName("PK__Disposal__EF94B94C724A5094");

            entity.ToTable("DisposalRecord");

            entity.Property(e => e.DiposalDate).HasColumnType("datetime");
            entity.Property(e => e.DiposalValue).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.DisposalRecords)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DisposalR__Asset__395884C4");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.DisposalRecords)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DisposalR__Asset__3A4CA8FD");

            entity.HasOne(d => d.ExecutedByNavigation).WithMany(p => p.DisposalRecords)
                .HasForeignKey(d => d.ExecutedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DisposalR__Execu__3B40CD36");
        });

        modelBuilder.Entity<DisposalExecution>(entity =>
        {
            entity.HasKey(e => e.DisposalExecutionId);
            entity.ToTable("DisposalExecution");

            entity.Property(e => e.PlannedExecutionDate).HasColumnType("datetime");
            entity.Property(e => e.ExecutedDate).HasColumnType("datetime");
            entity.Property(e => e.BuyerName).HasMaxLength(255);
            entity.Property(e => e.BuyerContact).HasMaxLength(255);
            entity.Property(e => e.ContractNo).HasMaxLength(100);
            entity.Property(e => e.InvoiceNo).HasMaxLength(100);
            entity.Property(e => e.MinutesNo).HasMaxLength(100);
            entity.Property(e => e.ActualDisposalValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExpenseValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SubmittedDate).HasColumnType("datetime");
            entity.Property(e => e.ApprovedDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.AssetRequest).WithMany()
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.DisposalRecord).WithMany()
                .HasForeignKey(d => d.DisposalRecordId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__Document__1ABEEF0F5ED0199F");

            entity.ToTable("Document");

            entity.Property(e => e.FileUrl).HasMaxLength(2000);
            entity.Property(e => e.UploadedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Asset).WithMany(p => p.Documents)
                .HasForeignKey(d => d.AssetId)
                .HasConstraintName("FK__Document__AssetI__681373AD");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.Documents)
                .HasForeignKey(d => d.AssetInstanceId)
                .HasConstraintName("FK__Document__AssetI__690797E6");

            entity.HasOne(d => d.Procurement).WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProcurementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .IsRequired(false)
                .HasConstraintName("FK__Document__Procur__671F4F74");

            entity.HasOne(d => d.GoodsReceipt).WithMany(r => r.Documents)
                .HasForeignKey(d => d.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.SupplierInvoice).WithMany(i => i.Documents)
                .HasForeignKey(d => d.SupplierInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Documents)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document__Upload__69FBBC1F");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F11C9C10578");

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
                .HasConstraintName("FK__Employee__Create__4AB81AF0");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__Depart__48CFD27E");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.EmployeeUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK__Employee__Update__4BAC3F29");

            entity.HasOne(d => d.User).WithMany(p => p.EmployeeUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Employee__UserId__47DBAE45");
        });

        modelBuilder.Entity<Guarantee>(entity =>
        {
            entity.HasKey(e => e.GuaranteeId).HasName("PK__Guarante__7EC2C760D57AA5BF");

            entity.ToTable("Guarantee");

            entity.Property(e => e.WarrantyPeriodUnit).HasMaxLength(20);

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.Guarantees)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Guarantee__Asset__66603565");
        });

        modelBuilder.Entity<InventoryDiscrepancy>(entity =>
        {
            entity.HasKey(e => e.DiscrepancyId).HasName("PK__Inventor__7462A8927CE32C55");

            entity.ToTable("InventoryDiscrepancy");

            entity.Property(e => e.ActualValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BookValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ResolvedAt).HasColumnType("datetime2");

            entity.HasOne(d => d.ActualLocation).WithMany(p => p.InventoryDiscrepancyActualLocations)
                .HasForeignKey(d => d.ActualLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Actua__5CA1C101");

            entity.HasOne(d => d.ActualUser).WithMany(p => p.InventoryDiscrepancyActualUsers)
                .HasForeignKey(d => d.ActualUserId)
                .HasConstraintName("FK__Inventory__Actua__5D95E53A");

            entity.HasOne(d => d.BookLocation).WithMany(p => p.InventoryDiscrepancyBookLocations)
                .HasForeignKey(d => d.BookLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__BookL__5AB9788F");

            entity.HasOne(d => d.BookUser).WithMany(p => p.InventoryDiscrepancyBookUsers)
                .HasForeignKey(d => d.BookUserId)
                .HasConstraintName("FK__Inventory__BookU__5BAD9CC8");

            entity.HasOne(d => d.Task).WithMany(p => p.InventoryDiscrepancies)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__TaskI__59C55456");
        });

        modelBuilder.Entity<InventoryRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Inventor__FBDF78E9849746D0");

            entity.ToTable("InventoryRecord");

            entity.Property(e => e.CheckedDate).HasColumnType("datetime");
            entity.Property(e => e.DateCheckCompleted).HasColumnType("datetime");

            entity.HasOne(d => d.ActualLocation).WithMany(p => p.InventoryRecords)
                .HasForeignKey(d => d.ActualLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Actua__55009F39");

            entity.HasOne(d => d.ActualUser).WithMany(p => p.InventoryRecordActualUsers)
                .HasForeignKey(d => d.ActualUserId)
                .HasConstraintName("FK__Inventory__Actua__55F4C372");

            entity.HasOne(d => d.CheckedByNavigation).WithMany(p => p.InventoryRecordCheckedByNavigations)
                .HasForeignKey(d => d.CheckedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Check__56E8E7AB");

            entity.HasOne(d => d.Task).WithMany(p => p.InventoryRecords)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__TaskI__540C7B00");
        });

        modelBuilder.Entity<InventorySession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Inventor__C9F492905C02132E");

            entity.ToTable("InventorySession");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Purpose).HasMaxLength(200);
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.AssetCategory).WithMany(p => p.InventorySessions)
                .HasForeignKey(d => d.AssetCategoryId)
                .HasConstraintName("FK__Inventory__Asset__6166761E");

            entity.HasOne(d => d.AssetType).WithMany(p => p.InventorySessions)
                .HasForeignKey(d => d.AssetTypeId)
                .HasConstraintName("FK__Inventory__Asset__625A9A57");

            entity.HasOne(d => d.Department).WithMany(p => p.InventorySessions)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Depar__607251E5");
        });

        modelBuilder.Entity<InventoryTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Inventor__7C6949B18D0D9264");

            entity.ToTable("InventoryTask");

            entity.Property(e => e.CheckDate).HasColumnType("datetime");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.InventoryTasks)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Asset__51300E55");

            entity.HasOne(d => d.Session).WithMany(p => p.InventoryTasks)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Sessi__489AC854");

            entity.HasOne(d => d.AssignedUser).WithMany()
                .HasForeignKey(d => d.AssignedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Assig__4B7734FF");

            entity.HasOne(d => d.Department).WithMany()
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Depar__4A8310C6");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__Maintena__FBDF78E9761393DB");

            entity.ToTable("MaintenanceRecord");

            entity.Property(e => e.ExecutionDate).HasColumnType("datetime");
            entity.Property(e => e.PerformedBy).HasMaxLength(255);
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__123EB7A3");

            entity.HasOne(d => d.Task).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__TaskI__114A936A");
        });

        modelBuilder.Entity<MaintenanceSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Maintena__9C8A5B4973846E72");

            entity.ToTable("MaintenanceSchedule");

            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NextDueDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Asset).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.AssetId)
                .HasConstraintName("FK__Maintenan__Asset__01142BA1");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.AssetInstanceId)
                .HasConstraintName("FK__Maintenan__Asset__02084FDA");

            entity.HasOne(d => d.Template).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Templ__02FC7413");
        });

        modelBuilder.Entity<MaintenanceTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Maintena__7C6949B13D47FD9E");

            entity.ToTable("MaintenanceTask");

            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpectedCompletionDate).HasColumnType("datetime");
            entity.Property(e => e.PlannedDate).HasColumnType("datetime");

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.MaintenanceTasks)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__0A9D95DB");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.MaintenanceTasks)
                .HasForeignKey(d => d.AssetRequestId)
                .HasConstraintName("FK__Maintenan__Asset__09A971A2");

            entity.HasOne(d => d.AssignToNavigation).WithMany(p => p.MaintenanceTaskAssignToNavigations)
                .HasForeignKey(d => d.AssignTo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Assig__0B91BA14");

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.MaintenanceTaskCreateByNavigations)
                .HasForeignKey(d => d.CreateBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Creat__0E6E26BF");

            entity.HasOne(d => d.PerformerUser).WithMany(p => p.MaintenanceTaskPerformerUsers)
                .HasForeignKey(d => d.PerformerUserId)
                .HasConstraintName("FK__Maintenan__Perfo__0C85DE4D");

            entity.HasOne(d => d.Schedule).WithMany(p => p.MaintenanceTasks)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK__Maintenan__Sched__08B54D69");
        });

        modelBuilder.Entity<MaintenanceTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK__Maintena__F87ADD2767695634");

            entity.ToTable("MaintenanceTemplate");

            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RepeatIntervalUnit).HasMaxLength(100);

            entity.HasOne(d => d.AssetType).WithMany(p => p.MaintenanceTemplates)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Asset__7E37BEF6");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E120220E04F");

            entity.ToTable("Notification");

            entity.Property(e => e.Content).HasMaxLength(100);
            entity.Property(e => e.SentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Ref).WithMany(p => p.NotificationRefs)
                .HasForeignKey(d => d.RefId)
                .HasConstraintName("FK__Notificat__RefId__6CD828CA");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__6DCC4D03");
        });

        modelBuilder.Entity<Procurement>(entity =>
        {
            entity.HasKey(e => e.ProcurementId).HasName("PK__Procurem__95B451EC0358F47C");

            entity.ToTable("Procurement");

            entity.Property(e => e.AdvanceAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ContractNo).HasMaxLength(100);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(e => e.RemainingAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.Procurements)
                .HasForeignKey(d => d.AssetRequestId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Procurement_AssetRequest_AssetRequestId");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Procurements)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Procureme__Creat__4B7734FF");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Procurements)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__Procureme__Suppl__4A8310C6");
        });

        modelBuilder.Entity<ProcurementLine>(entity =>
        {
            entity.HasKey(e => e.LineId).HasName("PK_ProcurementLine");

            entity.ToTable("ProcurementLine");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ReceivedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Procurement).WithMany(p => p.Lines)
                .HasForeignKey(d => d.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProcurementLine_Procurement");

            entity.HasOne(d => d.Asset).WithMany()
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcurementLine_Asset");
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.HasKey(e => e.GoodsReceiptId).HasName("PK_GoodsReceipt");

            entity.ToTable("GoodsReceipt");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(d => d.Procurement).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.ProcurementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GoodsReceipt_Procurement");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GoodsReceipt_User_CreatedBy");
        });

        modelBuilder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.HasKey(e => e.GoodsReceiptLineId).HasName("PK_GoodsReceiptLine");

            entity.ToTable("GoodsReceiptLine");

            entity.Property(e => e.QuantityReceived).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.GoodsReceipt).WithMany(r => r.Lines)
                .HasForeignKey(d => d.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_GoodsReceiptLine_GoodsReceipt");

            entity.HasOne(d => d.ProcurementLine).WithMany(l => l.GoodsReceiptLines)
                .HasForeignKey(d => d.ProcurementLineId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GoodsReceiptLine_ProcurementLine");

            entity.HasOne(d => d.Asset).WithMany()
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GoodsReceiptLine_Asset");
        });

        modelBuilder.Entity<SupplierInvoice>(entity =>
        {
            entity.HasKey(e => e.SupplierInvoiceId).HasName("PK_SupplierInvoice");

            entity.ToTable("SupplierInvoice");

            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");

            entity.HasIndex(e => e.InvoiceNumber).HasDatabaseName("IX_SupplierInvoice_InvoiceNumber");

            entity.HasOne(d => d.Procurement).WithMany(p => p.SupplierInvoices)
                .HasForeignKey(d => d.ProcurementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierInvoice_Procurement");

            entity.HasOne(d => d.GoodsReceipt).WithMany(r => r.SupplierInvoices)
                .HasForeignKey(d => d.GoodsReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierInvoice_GoodsReceipt");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(u => u.SupplierInvoicesCreated)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierInvoice_User_CreatedBy");
        });

        modelBuilder.Entity<SupplierInvoiceLine>(entity =>
        {
            entity.HasKey(e => e.SupplierInvoiceLineId).HasName("PK_SupplierInvoiceLine");

            entity.ToTable("SupplierInvoiceLine");

            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LineTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ChargeDescription).HasMaxLength(500);

            entity.HasOne(d => d.SupplierInvoice).WithMany(h => h.Lines)
                .HasForeignKey(d => d.SupplierInvoiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SupplierInvoiceLine_SupplierInvoice");

            entity.HasOne(d => d.ProcurementLine).WithMany(l => l.SupplierInvoiceLines)
                .HasForeignKey(d => d.ProcurementLineId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false)
                .HasConstraintName("FK_SupplierInvoiceLine_ProcurementLine");

            entity.HasOne(d => d.GoodsReceiptLine).WithMany(l => l.SupplierInvoiceLines)
                .HasForeignKey(d => d.GoodsReceiptLineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierInvoiceLine_GoodsReceiptLine");
        });

        modelBuilder.Entity<RepairRecord>(entity =>
        {
            entity.HasKey(e => e.RepairId).HasName("PK__RepairRe__07D0BC2DF4109EAC");

            entity.ToTable("RepairRecord");

            entity.Property(e => e.ActualCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DamageDate).HasColumnType("datetime");
            entity.Property(e => e.RepairDate).HasColumnType("datetime");
            entity.Property(e => e.ReturnToUseDate).HasColumnType("datetime2");
            entity.Property(e => e.RepairWarrantyPeriodUnit).HasMaxLength(20);
            entity.Property(e => e.RepairWarrantyNote).HasMaxLength(2000);

            entity.HasOne(d => d.Supplier).WithMany(p => p.RepairRecords)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK__RepairRec__Suppl__19DFD96B");

            entity.HasOne(d => d.Task).WithMany(p => p.RepairRecords)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairRec__TaskI__18EBB532");
        });

        modelBuilder.Entity<RepairTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__RepairTa__7C6949B198BA3996");

            entity.ToTable("RepairTask");

            entity.Property(e => e.EstimatedCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExpectedCompletionDate).HasColumnType("datetime");
            entity.Property(e => e.RepairDate).HasColumnType("datetime");
            entity.Property(e => e.SupplierId).IsRequired(false);

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.RepairTasks)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairTas__Asset__160F4887");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.RepairTasks)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RepairTas__Asset__151B244E");

            entity.HasOne(d => d.Supplier).WithMany(p => p.RepairTasks)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RepairTask_Supplier");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasKey(e => e.RequestTypeId).HasName("PK__RequestT__4D328B83A2AB96A0");

            entity.ToTable("RequestType");

            entity.HasOne(d => d.Workflow).WithMany(p => p.RequestTypes)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RequestTy__Workf__6B24EA82");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1A563411F2");

            entity.ToTable("Role");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RoleCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Role__CreatedBy__398D8EEE");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.RoleUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK__Role__UpdatedBy__3A81B327");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666B4EB2A600C");

            entity.ToTable("Supplier");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.ContactName).HasMaxLength(255);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.TaxCode).HasMaxLength(50);
        });

        modelBuilder.Entity<TransferRecord>(entity =>
        {
            entity.HasKey(e => e.TransferId).HasName("PK__Transfer__9549009117796871");

            entity.ToTable("TransferRecord");

            entity.Property(e => e.TransferDate).HasColumnType("datetime");
            entity.Property(e => e.IsSenderConfirmed);
            entity.Property(e => e.IsReceiverConfirmed);
            entity.Property(e => e.SenderConfirmedAt).HasColumnType("datetime2");
            entity.Property(e => e.ReceiverConfirmedAt).HasColumnType("datetime2");

            entity.HasOne(d => d.AssetRequest).WithMany(p => p.TransferRecords)
                .HasForeignKey(d => d.AssetRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.AssetInstance).WithMany(p => p.TransferRecords)
                .HasForeignKey(d => d.AssetInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Asset__31B762FC");

            entity.HasOne(d => d.ExecutedByNavigation).WithMany(p => p.TransferRecordExecutedByNavigations)
                .HasForeignKey(d => d.ExecutedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__Execu__367C1819");

            entity.HasOne(d => d.FromLocation).WithMany(p => p.TransferRecordFromLocations)
                .HasForeignKey(d => d.FromLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__FromL__32AB8735");

            entity.HasOne(d => d.FromUser).WithMany(p => p.TransferRecordFromUsers)
                .HasForeignKey(d => d.FromUserId)
                .HasConstraintName("FK__TransferR__FromU__3493CFA7");

            entity.HasOne(d => d.ToLocation).WithMany(p => p.TransferRecordToLocations)
                .HasForeignKey(d => d.ToLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TransferR__ToLoc__339FAB6E");

            entity.HasOne(d => d.ToUser).WithMany(p => p.TransferRecordToUsers)
                .HasForeignKey(d => d.ToUserId)
                .HasConstraintName("FK__TransferR__ToUse__3587F3E0");
        });

        modelBuilder.Entity<TransferHandoverRecord>(entity =>
        {
            entity.HasKey(e => e.TransferHandoverRecordId);

            entity.ToTable("TransferHandoverRecord");

            entity.Property(e => e.Side).HasMaxLength(20);
            entity.Property(e => e.DetailsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UserNote).HasMaxLength(2000);
            entity.Property(e => e.OccurredAt).HasColumnType("datetime2");

            entity.HasOne(d => d.Transfer)
                .WithMany(p => p.HandoverRecords)
                .HasForeignKey(d => d.TransferId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ActionByUser)
                .WithMany()
                .HasForeignKey(d => d.ActionByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4C972967A3");

            entity.ToTable("User");

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.RefreshToken).HasMaxLength(255);
            entity.Property(e => e.RefreshTokenExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.ResetPasswordToken).HasMaxLength(255);
            entity.Property(e => e.ResetPasswordTokenExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.AccessFailedCount).HasDefaultValue(0);
            entity.Property(e => e.LockoutEnd).HasColumnType("datetime2");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("PK__UserRole__AF2760AD8BB657A6");

            entity.ToTable("UserRole");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__RoleId__3E52440B");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__UserId__3D5E1FD2");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFF9DCDCCD11");

            entity.ToTable("Warehouse");

            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<WarehouseAsset>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFF9DBC6D596");

            entity.ToTable("WarehouseAsset");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.WorkflowId).HasName("PK__Workflow__5704A66A04CD835A");

            entity.ToTable("Workflow");

            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(e => e.StepId).HasName("PK__Workflow__243433572DEEF651");

            entity.ToTable("WorkflowStep");

            entity.HasOne(d => d.Role).WithMany(p => p.WorkflowSteps)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkflowS__RoleI__6EF57B66");

            entity.HasOne(d => d.Workflow).WithMany(p => p.WorkflowSteps)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkflowS__Workf__6E01572D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
