using g19_sep490_ealds.Server.Mappers.Implementation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Implementation;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class MaintenanceRecordServiceCostRecordingTests_New
{
    private readonly EaldsDbContext _context;
    private readonly MaintenanceRecordService _service;

    public MaintenanceRecordServiceCostRecordingTests_New()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EaldsDbContext(options);
        _service = new MaintenanceRecordService(new MaintenanceRecordMapper(), _context);
    }

    private async Task<int> SeedAssetWithInstanceAsync(int assetId, int instanceId)
    {
        if (!await _context.AssetCategories.AnyAsync(x => x.CategoryId == 1))
            _context.AssetCategories.Add(new AssetCategory { CategoryId = 1, Name = "IT" });
        if (!await _context.AssetTypes.AnyAsync(x => x.AssetTypeId == 1))
            _context.AssetTypes.Add(new AssetType { AssetTypeId = 1, CategoryId = 1, Name = "Laptop" });

        _context.Assets.Add(new Asset
        {
            AssetId = assetId,
            AssetTypeId = 1,
            Code = $"AS-{assetId}",
            Name = $"Asset {assetId}",
            Unit = "pcs",
            Status = 1,
            CreatedBy = 1
        });
        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = instanceId,
            AssetId = assetId,
            WarehouseId = 1,
            InstanceCode = $"INS-{instanceId}",
            Status = (int)AssetStatus.InUse,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalPrice = 1000000m,
            CurrentValue = 900000m
        });
        await _context.SaveChangesAsync();
        return instanceId;
    }

    private async Task SeedMaintenanceRecordAsync(int taskId, int instanceId, decimal cost, DateTime executionDate)
    {
        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = taskId,
            AssetRequestId = taskId,
            AssetInstanceId = instanceId,
            PlannedDate = executionDate.AddDays(-1),
            Status = 2,
            CreateDate = DateTime.UtcNow,
            CreateBy = 1
        });
        _context.MaintenanceRecords.Add(new MaintenanceRecord
        {
            RecordId = taskId,
            TaskId = taskId,
            AssetInstanceId = instanceId,
            ExecutionDate = executionDate,
            TotalCost = cost,
            WorkPerformed = "Periodic maintenance",
            ConditionBefore = "Before",
            ConditionAfter = "After",
            Status = 1
        });
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetRecordsByAssetAsync_ReturnsOnlyMaintenanceRecords()
    {
        var instanceId = await SeedAssetWithInstanceAsync(1, 1);
        await SeedMaintenanceRecordAsync(1, instanceId, 250000m, DateTime.UtcNow);

        var result = (await _service.GetRecordsByAssetAsync(1)).ToList();
        Assert.Single(result);
        Assert.Equal("maintenance", result[0].RecordSource);
    }

    [Fact]
    public async Task GetRecordsByAssetAsync_SortsByExecutionDateDescending()
    {
        var instanceId = await SeedAssetWithInstanceAsync(1, 1);
        await SeedMaintenanceRecordAsync(1, instanceId, 100000m, DateTime.UtcNow.AddDays(-2));
        await SeedMaintenanceRecordAsync(2, instanceId, 200000m, DateTime.UtcNow.AddDays(-1));

        var result = (await _service.GetRecordsByAssetAsync(1)).ToList();
        Assert.Equal(2, result.Count);
        Assert.True(result[0].ExecutionDate >= result[1].ExecutionDate);
    }

    [Fact]
    public async Task GetRecordsByInstanceAsync_IsolatesByInstance()
    {
        await SeedAssetWithInstanceAsync(1, 1);
        await SeedAssetWithInstanceAsync(2, 2);
        await SeedMaintenanceRecordAsync(1, 1, 111000m, DateTime.UtcNow);

        var result = (await _service.GetRecordsByInstanceAsync(1)).ToList();
        Assert.Single(result);
        Assert.Equal(1, result[0].AssetInstanceId);
    }
}
