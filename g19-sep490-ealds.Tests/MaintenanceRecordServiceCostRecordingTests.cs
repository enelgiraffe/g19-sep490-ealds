using g19_sep490_ealds.Server.DTOs.Maintenance;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Implementation;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests: MaintenanceRecordService cost query service
/// Tests complete functionality related to cost query in MaintenanceRecordService:
/// 1. Query maintenance and repair cost records by asset ID (GetRecordsByAssetAsync)
/// 2. Query maintenance and repair cost records by asset instance ID (GetRecordsByInstanceAsync)
/// 3. Cost record sorting, pagination boundaries, and data integrity
/// </summary>
public class MaintenanceRecordServiceCostRecordingTests
{
    private readonly EaldsDbContext _context;
    private readonly MaintenanceRecordService _service;

    public MaintenanceRecordServiceCostRecordingTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        var mockMapper = new Mock<IMaintenanceRecordMapper>();
        mockMapper.Setup(m => m.EntityToResponse(It.IsAny<MaintenanceRecord>()))
            .Returns((MaintenanceRecord r) => new MaintenanceRecordResponseDTO());
        mockMapper.Setup(m => m.ListEntityToResponse(It.IsAny<IEnumerable<MaintenanceRecord>>()))
            .Returns((IEnumerable<MaintenanceRecord> r) => r.Select(x => new MaintenanceRecordResponseDTO()));

        _service = new MaintenanceRecordService(mockMapper.Object, _context);
    }

    // ========================================
    // Part 1: Base Data Seeding
    // ========================================

    /// <summary>
    /// Seeds base asset data: Asset + AssetInstance
    /// </summary>
    private async Task<(Asset asset, AssetInstance instance)> SeedAssetAsync(
        int assetId = 1,
        int instanceId = 1,
        string code = "LAPTOP-001",
        string name = "Dell Laptop 001",
        decimal originalPrice = 20000000m)
    {
        _context.AssetTypes.Add(new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        });

        var asset = new Asset
        {
            AssetId = assetId,
            Code = code,
            Name = name,
            AssetTypeId = 1,
            Status = 1,
            Unit = "pcs",
            CreatedBy = 1
        };

        var instance = new AssetInstance
        {
            AssetInstanceId = instanceId,
            AssetId = assetId,
            WarehouseId = 1,
            InstanceCode = $"INS-{code}",
            Status = (int)AssetStatus.InUse,
            InUseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-12)),
            OriginalPrice = originalPrice,
            CurrentValue = originalPrice * 0.8m
        };

        _context.Assets.Add(asset);
        _context.AssetInstances.Add(instance);
        await _context.SaveChangesAsync();

        return (asset, instance);
    }

    /// <summary>
    /// Seeds repair task + repair record
    /// </summary>
    private async Task<RepairRecord> SeedRepairRecordAsync(
        int taskId,
        int instanceId,
        decimal actualCost,
        string result = "Fixed",
        string? detailedDescription = null,
        DateTime? repairDate = null)
    {
        var record = new RepairRecord
        {
            RepairId = taskId,
            TaskId = taskId,
            ActualCost = actualCost,
            RepairDate = repairDate ?? DateTime.UtcNow.AddDays(-5),
            Result = result,
            DetailedDescription = detailedDescription
        };

        _context.RepairRecords.Add(record);
        await _context.SaveChangesAsync();

        return record;
    }

    /// <summary>
    /// Seeds repair task (RepairTask) and links repair record
    /// </summary>
    private async Task SeedRepairTaskWithRecordAsync(
        int taskId,
        int instanceId,
        int assetId,
        decimal estimatedCost,
        decimal actualCost,
        string reason = "Screen broken",
        DateTime? repairDate = null,
        string? repairProgressStatus = null)
    {
        var task = new RepairTask
        {
            TaskId = taskId,
            AssetInstanceId = instanceId,
            AssetRequestId = taskId,
            EstimatedCost = estimatedCost,
            Reason = reason,
            Status = 2,
            RepairDate = repairDate ?? DateTime.UtcNow.AddDays(-5),
            ExpectedCompletionDate = DateTime.UtcNow,
            RepairProgressStatus = repairProgressStatus
        };

        _context.RepairTasks.Add(task);
        await SeedRepairRecordAsync(taskId, instanceId, actualCost, "Repaired", null, repairDate);
    }

    /// <summary>
    /// Seeds maintenance task (MaintenanceTask) and links maintenance record (MaintenanceRecord)
    /// </summary>
    private async Task SeedMaintenanceRecordAsync(
        int taskId,
        int instanceId,
        decimal totalCost,
        string workPerformed = "Regular maintenance",
        DateTime? executionDate = null,
        string? conditionBefore = null,
        string? conditionAfter = null)
    {
        var task = new MaintenanceTask
        {
            TaskId = taskId,
            AssetInstanceId = instanceId,
            AssetRequestId = taskId,
            PlannedDate = DateTime.UtcNow.AddDays(-3),
            Status = 2,
            CreateDate = DateTime.UtcNow,
            CreateBy = 1
        };

        var record = new MaintenanceRecord
        {
            RecordId = taskId,
            TaskId = taskId,
            AssetInstanceId = instanceId,
            ExecutionDate = executionDate ?? DateTime.UtcNow.AddDays(-1),
            TotalCost = totalCost,
            WorkPerformed = workPerformed,
            ConditionBefore = conditionBefore,
            ConditionAfter = conditionAfter,
            Status = 1
        };

        _context.MaintenanceTasks.Add(task);
        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync();
    }

    // ========================================
    // Part 2: GetRecordsByAssetAsync - Query Cost Records by Asset ID
    // ========================================

    #region GetRecordsByAssetAsync Tests

    /// <summary>
    /// Test: Query an asset with no repair/maintenance records
    /// Expected: Returns empty list
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_NoRecords_ReturnsEmptyList()
    {
        var (asset, instance) = await SeedAssetAsync();

        var result = await _service.GetRecordsByAssetAsync(assetId: asset.AssetId);

        Assert.Empty(result);
    }

    /// <summary>
    /// Test: Query asset with one maintenance record
    /// Expected: Returns list containing 1 MaintenanceRecord
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_SingleMaintenanceRecord_ReturnsCorrectRecord()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            totalCost: 500000m,
            workPerformed: "Oil change",
            executionDate: DateTime.UtcNow.AddDays(-3)
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.Equal("maintenance", result[0].RecordSource);
        Assert.Equal(500000m, result[0].TotalCost);
        Assert.Equal("Oil change", result[0].WorkPerformed);
        Assert.Equal(instance.AssetInstanceId, result[0].AssetInstanceId);
    }

    /// <summary>
    /// Test: Query asset with one repair record (RepairRecord)
    /// Expected: Returns list containing 1 repair record, RecordSource = "repair"
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_SingleRepairRecord_ReturnsCorrectRecord()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedRepairTaskWithRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 1000000m,
            actualCost: 800000m,
            reason: "Screen broken"
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.Equal("repair", result[0].RecordSource);
        Assert.Equal(800000m, result[0].TotalCost);
        Assert.Equal("Screen broken", result[0].ConditionBefore);
    }

    /// <summary>
    /// Test: Query asset with both repair and maintenance records
    /// Expected: Returns all records, including both sources
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_MixedRecords_ReturnsAllRecords()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            totalCost: 200000m,
            workPerformed: "Inspection"
        );

        await SeedRepairTaskWithRecordAsync(
            taskId: 2,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 300000m,
            actualCost: 250000m,
            reason: "Keyboard broken"
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.RecordSource == "maintenance");
        Assert.Contains(result, r => r.RecordSource == "repair");
    }

    /// <summary>
    /// Test: Query non-existent asset ID
    /// Expected: Returns empty list (does not throw exception)
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_NonExistentAssetId_ReturnsEmptyList()
    {
        var result = await _service.GetRecordsByAssetAsync(assetId: 9999);
        Assert.Empty(result);
    }

    /// <summary>
    /// Test: Repair record with ActualCost = 0 (zero-cost repair)
    /// Expected: MaintenanceRecordResponseDTO.TotalCost = 0
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_ZeroCostRepair_ShowsZeroTotalCost()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedRepairTaskWithRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 0m,
            actualCost: 0m,
            reason: "Free warranty repair"
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.Equal(0m, result[0].TotalCost);
    }

    /// <summary>
    /// Test: Maintenance record with very large TotalCost
    /// Expected: TotalCost correctly stored and returned
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_LargeTotalCost_StoredCorrectly()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            totalCost: 999999999.99m,
            workPerformed: "Major overhaul"
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.Equal(999999999.99m, result[0].TotalCost);
    }

    #endregion

    // ========================================
    // Part 3: GetRecordsByInstanceAsync - Query Cost Records by Asset Instance ID
    // ========================================

    #region GetRecordsByInstanceAsync Tests

    /// <summary>
    /// Test: Query by asset instance ID, one repair record
    /// Expected: Returns that repair record
    /// </summary>
    [Fact]
    public async Task GetRecordsByInstanceAsync_SingleRepairRecord_ReturnsCorrectRecord()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedRepairTaskWithRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 2000000m,
            actualCost: 1800000m,
            reason: "Battery replacement"
        );

        var result = (await _service.GetRecordsByInstanceAsync(assetInstanceId: instance.AssetInstanceId)).ToList();

        Assert.Single(result);
        Assert.Equal("repair", result[0].RecordSource);
        Assert.Equal(1800000m, result[0].TotalCost);
        Assert.Equal(instance.AssetInstanceId, result[0].AssetInstanceId);
    }

    /// <summary>
    /// Test: Query by asset instance ID, one maintenance record
    /// Expected: Returns that maintenance record
    /// </summary>
    [Fact]
    public async Task GetRecordsByInstanceAsync_SingleMaintenanceRecord_ReturnsCorrectRecord()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            totalCost: 750000m,
            workPerformed: "Annual service",
            conditionBefore: "Worn parts",
            conditionAfter: "New parts installed"
        );

        var result = (await _service.GetRecordsByInstanceAsync(assetInstanceId: instance.AssetInstanceId)).ToList();

        Assert.Single(result);
        Assert.Equal("maintenance", result[0].RecordSource);
        Assert.Equal(750000m, result[0].TotalCost);
        Assert.Equal("Worn parts", result[0].ConditionBefore);
        Assert.Equal("New parts installed", result[0].ConditionAfter);
    }

    /// <summary>
    /// Test: Query by asset instance ID, with both repair and maintenance records
    /// Expected: Returns all records
    /// </summary>
    [Fact]
    public async Task GetRecordsByInstanceAsync_MixedRecords_ReturnsAllRecords()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            totalCost: 300000m,
            workPerformed: "Regular service"
        );

        await SeedRepairTaskWithRecordAsync(
            taskId: 2,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 500000m,
            actualCost: 450000m,
            reason: "Screen damage"
        );

        var result = (await _service.GetRecordsByInstanceAsync(assetInstanceId: instance.AssetInstanceId)).ToList();

        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// Test: Query non-existent asset instance ID
    /// Expected: Returns empty list
    /// </summary>
    [Fact]
    public async Task GetRecordsByInstanceAsync_NonExistentInstanceId_ReturnsEmptyList()
    {
        var result = await _service.GetRecordsByInstanceAsync(assetInstanceId: 9999);
        Assert.Empty(result);
    }

    #endregion

    // ========================================
    // Part 4: Sorting Validation - Cost Records Sorted by Execution Date Descending
    // ========================================

    #region Sorting Tests

    /// <summary>
    /// Test: Multiple cost records sorted by execution date descending
    /// Expected: First record is the most recent (latest date)
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_MultipleRecords_SortedByExecutionDateDescending()
    {
        var (asset, instance) = await SeedAssetAsync();

        // Record 1: Older maintenance record
        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            totalCost: 100000m,
            workPerformed: "Old maintenance",
            executionDate: DateTime.UtcNow.AddDays(-30)
        );

        // Record 2: More recent repair record
        await SeedRepairTaskWithRecordAsync(
            taskId: 2,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 200000m,
            actualCost: 150000m,
            reason: "Recent repair",
            repairDate: DateTime.UtcNow.AddDays(-5)
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Equal(2, result.Count);
        // First should be the most recent (repair record, date is -5 days)
        Assert.Equal("repair", result[0].RecordSource);
        Assert.True(result[0].ExecutionDate >= result[1].ExecutionDate);
    }

    /// <summary>
    /// Test: Records with the same execution date are also correctly sorted
    /// </summary>
    [Fact]
    public async Task GetRecordsByInstanceAsync_SameDateRecords_StableOrder()
    {
        var (asset, instance) = await SeedAssetAsync();
        var sameDate = DateTime.UtcNow;

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            totalCost: 100000m,
            workPerformed: "First record",
            executionDate: sameDate
        );

        await SeedRepairTaskWithRecordAsync(
            taskId: 2,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 200000m,
            actualCost: 200000m,
            reason: "Second record",
            repairDate: sameDate
        );

        var result = (await _service.GetRecordsByInstanceAsync(assetInstanceId: instance.AssetInstanceId)).ToList();

        Assert.Equal(2, result.Count);
    }

    #endregion

    // ========================================
    // Part 5: Multi-Asset Scenarios - Ensuring Only Returns Records for the Specified Asset/Instance
    // ========================================

    #region Multi-Asset Isolation Tests

    /// <summary>
    /// Test: Two different assets each have repair records
    /// Expected: GetRecordsByAssetAsync only returns records for the requested asset
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_MultiAsset_OnlyReturnsRequestedAssetRecords()
    {
        var (asset1, instance1) = await SeedAssetAsync(
            assetId: 1, instanceId: 1, code: "LAPTOP-001", name: "Dell Laptop");

        var (asset2, instance2) = await SeedAssetAsync(
            assetId: 2, instanceId: 2, code: "LAPTOP-002", name: "HP Laptop");

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance1.AssetInstanceId,
            totalCost: 100000m,
            workPerformed: "Maintenance for asset 1"
        );

        await SeedMaintenanceRecordAsync(
            taskId: 2,
            instanceId: instance2.AssetInstanceId,
            totalCost: 200000m,
            workPerformed: "Maintenance for asset 2"
        );

        var result1 = (await _service.GetRecordsByAssetAsync(assetId: asset1.AssetId)).ToList();
        var result2 = (await _service.GetRecordsByAssetAsync(assetId: asset2.AssetId)).ToList();

        Assert.Single(result1);
        Assert.Equal(100000m, result1[0].TotalCost);
        Assert.Contains("asset 1", result1[0].WorkPerformed);

        Assert.Single(result2);
        Assert.Equal(200000m, result2[0].TotalCost);
        Assert.Contains("asset 2", result2[0].WorkPerformed);
    }

    /// <summary>
    /// Test: One asset has multiple instances, each with repair records
    /// Expected: GetRecordsByAssetAsync returns summary of all instances' repair records
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_MultiInstance_ReturnsAllInstanceRecords()
    {
        var (asset, instance1) = await SeedAssetAsync(
            assetId: 1, instanceId: 1, code: "LAPTOP-001", name: "Dell Laptop");

        // Add second asset instance
        var instance2 = new AssetInstance
        {
            AssetInstanceId = 2,
            AssetId = asset.AssetId,
            WarehouseId = 1,
            InstanceCode = "INS-LAPTOP-002",
            Status = (int)AssetStatus.InUse,
            InUseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            OriginalPrice = 15000000m,
            CurrentValue = 12000000m
        };

        _context.AssetInstances.Add(instance2);
        await _context.SaveChangesAsync();

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance1.AssetInstanceId,
            totalCost: 50000m,
            workPerformed: "Maintenance instance 1"
        );

        await SeedMaintenanceRecordAsync(
            taskId: 2,
            instanceId: instance2.AssetInstanceId,
            totalCost: 60000m,
            workPerformed: "Maintenance instance 2"
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.AssetInstanceId == instance1.AssetInstanceId);
        Assert.Contains(result, r => r.AssetInstanceId == instance2.AssetInstanceId);
    }

    /// <summary>
    /// Test: GetRecordsByInstanceAsync only returns records for the specified instance, not other instances
    /// </summary>
    [Fact]
    public async Task GetRecordsByInstanceAsync_MultiInstance_OnlyReturnsRequestedInstanceRecords()
    {
        var (asset, instance1) = await SeedAssetAsync(
            assetId: 1, instanceId: 1);

        var instance2 = new AssetInstance
        {
            AssetInstanceId = 2,
            AssetId = asset.AssetId,
            WarehouseId = 1,
            InstanceCode = "INS-LAPTOP-002",
            Status = (int)AssetStatus.InUse,
            InUseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
            OriginalPrice = 15000000m,
            CurrentValue = 14000000m
        };

        _context.AssetInstances.Add(instance2);
        await _context.SaveChangesAsync();

        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance1.AssetInstanceId,
            totalCost: 100000m,
            workPerformed: "Instance 1 record"
        );

        await SeedMaintenanceRecordAsync(
            taskId: 2,
            instanceId: instance2.AssetInstanceId,
            totalCost: 200000m,
            workPerformed: "Instance 2 record"
        );

        var result = (await _service.GetRecordsByInstanceAsync(assetInstanceId: instance1.AssetInstanceId)).ToList();

        Assert.Single(result);
        Assert.Equal(instance1.AssetInstanceId, result[0].AssetInstanceId);
        Assert.Equal(100000m, result[0].TotalCost);
    }

    #endregion

    // ========================================
    // Part 6: Repair Record Field Mapping Validation
    // ========================================

    #region Repair Record Field Mapping Tests

    /// <summary>
    /// Test: Repair record WorkPerformed field comes from RepairTask.RepairProgressStatus (priority)
    /// Expected: WorkPerformed = RepairProgressStatus
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_RepairRecordWithRepairProgressStatus_UsesProgressStatus()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedRepairTaskWithRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 500000m,
            actualCost: 450000m,
            reason: "Minor damage",
            repairProgressStatus: "Replaced screen, tested OK"
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.Equal("Replaced screen, tested OK", result[0].WorkPerformed);
    }

    /// <summary>
    /// Test: Repair record without RepairProgressStatus, WorkPerformed comes from Result
    /// Expected: WorkPerformed = Result
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_RepairRecordWithoutProgressStatus_UsesResult()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedRepairTaskWithRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 500000m,
            actualCost: 400000m,
            reason: "Hardware issue"
        );
        // Above defaults Result = "Repaired"

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.Equal("Repaired", result[0].WorkPerformed);
    }

    /// <summary>
    /// Test: Repair record ConditionBefore comes from RepairTask.Reason
    /// Expected: ConditionBefore = "Screen broken"
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_RepairRecord_ConditionBeforeIsReason()
    {
        var (asset, instance) = await SeedAssetAsync();

        await SeedRepairTaskWithRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            assetId: asset.AssetId,
            estimatedCost: 300000m,
            actualCost: 280000m,
            reason: "Keyboard malfunction"
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.Equal("Keyboard malfunction", result[0].ConditionBefore);
    }

    /// <summary>
    /// Test: Repair record with DetailedDescription, TechnicalNote should contain that value
    /// Expected: TechnicalNote = DetailedDescription
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_RepairRecordWithDetailedDescription_HasTechnicalNote()
    {
        var (asset, instance) = await SeedAssetAsync();

        var task = new RepairTask
        {
            TaskId = 1,
            AssetInstanceId = instance.AssetInstanceId,
            AssetRequestId = 1,
            EstimatedCost = 500000m,
            Reason = "Display issue",
            Status = 2,
            RepairDate = DateTime.UtcNow.AddDays(-3),
            ExpectedCompletionDate = DateTime.UtcNow
        };

        var record = new RepairRecord
        {
            RepairId = 1,
            TaskId = 1,
            ActualCost = 450000m,
            RepairDate = DateTime.UtcNow.AddDays(-3),
            Result = "Fixed",
            DetailedDescription = "Replaced LCD panel. All functions normal. 6 months warranty."
        };

        _context.RepairTasks.Add(task);
        _context.RepairRecords.Add(record);
        await _context.SaveChangesAsync();

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.NotNull(result[0].TechnicalNote);
        Assert.Contains("LCD panel", result[0].TechnicalNote);
    }

    #endregion

    // ========================================
    // Part 7: Maintenance Record Field Mapping Validation
    // ========================================

    #region Maintenance Record Field Mapping Tests

    /// <summary>
    /// Test: Maintenance record field completeness validation
    /// Expected: All fields correctly mapped
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_MaintenanceRecord_FullFieldMapping()
    {
        var (asset, instance) = await SeedAssetAsync();

        var executionDate = DateTime.UtcNow.AddDays(-7);
        await SeedMaintenanceRecordAsync(
            taskId: 1,
            instanceId: instance.AssetInstanceId,
            totalCost: 350000m,
            workPerformed: "Full system check",
            executionDate: executionDate,
            conditionBefore: "Dusty internals",
            conditionAfter: "Cleaned and tested"
        );

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        Assert.Equal("maintenance", result[0].RecordSource);
        Assert.Equal(350000m, result[0].TotalCost);
        Assert.Equal("Full system check", result[0].WorkPerformed);
        Assert.Equal("Dusty internals", result[0].ConditionBefore);
        Assert.Equal("Cleaned and tested", result[0].ConditionAfter);
        Assert.Equal(instance.AssetInstanceId, result[0].AssetInstanceId);
        Assert.Equal($"INS-LAPTOP-001", result[0].InstanceCode);
        Assert.Equal(MaintenanceRecordStatus.Completed, result[0].Status);
    }

    #endregion

    // ========================================
    // Part 8: Performance and Boundary Scenarios
    // ========================================

    #region Performance and Boundary Tests

    /// <summary>
    /// Test: Large number of cost records (100+) can still be queried correctly
    /// Expected: Returns all records, sorted by date descending
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_LargeNumberOfRecords_ReturnsAllRecords()
    {
        var (asset, instance) = await SeedAssetAsync();

        for (int i = 1; i <= 50; i++)
        {
            await SeedMaintenanceRecordAsync(
                taskId: i,
                instanceId: instance.AssetInstanceId,
                totalCost: i * 10000m,
                workPerformed: $"Maintenance #{i}",
                executionDate: DateTime.UtcNow.AddDays(-i)
            );
        }

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Equal(50, result.Count);
        Assert.True(result[0].ExecutionDate >= result[1].ExecutionDate);
    }

    /// <summary>
    /// Test: Repair record with empty string Result
    /// Expected: Handled gracefully without errors
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_EmptyResult_HandlesGracefully()
    {
        var (asset, instance) = await SeedAssetAsync();

        var task = new RepairTask
        {
            TaskId = 1,
            AssetInstanceId = instance.AssetInstanceId,
            AssetRequestId = 1,
            EstimatedCost = 100000m,
            Reason = "Minor issue",
            Status = 2,
            RepairDate = DateTime.UtcNow.AddDays(-3)
        };

        var record = new RepairRecord
        {
            RepairId = 1,
            TaskId = 1,
            ActualCost = 80000m,
            RepairDate = DateTime.UtcNow.AddDays(-3),
            Result = ""
        };

        _context.RepairTasks.Add(task);
        _context.RepairRecords.Add(record);
        await _context.SaveChangesAsync();

        var result = await _service.GetRecordsByAssetAsync(assetId: asset.AssetId);

        Assert.Single(result);
    }

    /// <summary>
    /// Test: RepairRecord.Result contains ReportNumber and ReturnToUseDate (legacy format)
    /// Expected: These metadata are filtered out, only the actual description is retained
    /// </summary>
    [Fact]
    public async Task GetRecordsByAssetAsync_RepairRecordWithMetadata_FiltersMetadata()
    {
        var (asset, instance) = await SeedAssetAsync();

        var task = new RepairTask
        {
            TaskId = 1,
            AssetInstanceId = instance.AssetInstanceId,
            AssetRequestId = 1,
            EstimatedCost = 200000m,
            Reason = "Broken key",
            Status = 2,
            RepairDate = DateTime.UtcNow.AddDays(-3)
        };

        var record = new RepairRecord
        {
            RepairId = 1,
            TaskId = 1,
            ActualCost = 180000m,
            RepairDate = DateTime.UtcNow.AddDays(-3),
            Result = "Replaced keycap\nReportNumber: RPR-2024-001\nReturnToUseDate: 2024-03-15\nAll keys working now"
        };

        _context.RepairTasks.Add(task);
        _context.RepairRecords.Add(record);
        await _context.SaveChangesAsync();

        var result = (await _service.GetRecordsByAssetAsync(assetId: asset.AssetId)).ToList();

        Assert.Single(result);
        // The extracted narrative should not contain ReportNumber and ReturnToUseDate
        Assert.DoesNotContain("ReportNumber:", result[0].ConditionAfter ?? "");
        Assert.DoesNotContain("ReturnToUseDate:", result[0].ConditionAfter ?? "");
    }

    #endregion
}
