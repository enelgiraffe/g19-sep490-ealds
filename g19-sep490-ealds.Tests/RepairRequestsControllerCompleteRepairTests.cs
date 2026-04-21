using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class RepairRequestsControllerCompleteRepairTests
{
    private readonly EaldsDbContext _context;
    private readonly RepairRequestsController _controller;

    public RepairRequestsControllerCompleteRepairTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "App:RepairRequestTypeId", "4" } })
            .Build();

        var mockNotification = new Mock<IAssetRequestNotificationService>();
        _controller = new RepairRequestsController(_context, configuration, mockNotification.Object);
        SetUser(actorUserId: 1);
    }

    // ─── Setup helpers ───────────────────────────────────────────────────────

    private void SetUser(int actorUserId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, actorUserId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>Seeds: Asset, AssetInstance(InRepair), AssetRequest(Type=4,Status=4), RepairTask(Status=1)</summary>
    private async Task<RepairTask> SeedAsync()
    {
        _context.Assets.Add(new Asset
        {
            AssetId = 1,
            Code = "DELL-001",
            Name = "Dell Laptop 001",
            AssetTypeId = 1,
            Status = 1,
            Unit = "pcs",
            CreatedBy = 1
        });

        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 1,
            AssetId = 1,
            WarehouseId = 1,
            InstanceCode = "INS-001",
            Status = (int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.InRepair,
            InUseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-12)),
            OriginalPrice = 15000000m,
            CurrentValue = 12000000m
        });

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 1,
            UserId = 1,
            RequestTypeId = 4,
            Title = "Repair Dell Laptop",
            Status = 4,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 1
        });

        _context.RepairTasks.Add(new RepairTask
        {
            TaskId = 1,
            AssetRequestId = 1,
            AssetInstanceId = 1,
            EstimatedCost = 500000m,
            Reason = "Screen broken",
            Status = 1,
            RepairDate = DateTime.UtcNow.AddDays(-3),
            ExpectedCompletionDate = DateTime.UtcNow.AddDays(3)
        });

        await _context.SaveChangesAsync();
        return (await _context.RepairTasks.FindAsync(1))!;
    }

    /// <summary>Minimal valid DTO — only required fields</summary>
    private RepairCompleteDto MinimalDto(int completedBy = 1) => new RepairCompleteDto
    {
        CompletedBy = completedBy,
        ActualCost = 100000m,
        Result = "Fixed"
    };

    // ─── Null / Invalid Input ──────────────────────────────────────────────────

    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        var result = await _controller.CompleteRepair(taskId: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CompletedByZero_ReturnsBadRequest()
    {
        var dto = new RepairCompleteDto { CompletedBy = 0 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CompletedByNegative_ReturnsBadRequest()
    {
        var dto = new RepairCompleteDto { CompletedBy = -1 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Task not found / wrong status ────────────────────────────────────────

    [Fact]
    public async Task TaskNotFound_ReturnsNotFound()
    {
        var result = await _controller.CompleteRepair(taskId: 999, dto: MinimalDto());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task TaskStatusZero_ReturnsBadRequest()
    {
        await SeedAsync();
        var task = await _context.RepairTasks.FindAsync(1);
        task!.Status = 0;
        await _context.SaveChangesAsync();

        var result = await _controller.CompleteRepair(task.TaskId, MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task TaskStatusTwo_ReturnsBadRequest()
    {
        await SeedAsync();
        var task = await _context.RepairTasks.FindAsync(1);
        task!.Status = 2;
        await _context.SaveChangesAsync();

        var result = await _controller.CompleteRepair(task.TaskId, MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── AssetRequest wrong state ─────────────────────────────────────────────

    [Fact]
    public async Task RequestStatusNot4_ReturnsBadRequest()
    {
        await SeedAsync();
        var req = await _context.AssetRequests.FindAsync(1);
        req!.Status = 3;
        await _context.SaveChangesAsync();

        var result = await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RequestTypeNotRepair_ReturnsBadRequest()
    {
        await SeedAsync();
        var req = await _context.AssetRequests.FindAsync(1);
        req!.RequestTypeId = 1;
        await _context.SaveChangesAsync();

        var result = await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidData_ReturnsOk()
    {
        await SeedAsync();
        var result = await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidData_ReturnsRecordIdAndTaskId()
    {
        await SeedAsync();
        var okResult = (OkObjectResult)await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());
        var response = okResult.Value;
        var recordId = (int)response!.GetType().GetProperty("recordId")!.GetValue(response)!;
        var taskId = (int)response.GetType().GetProperty("taskId")!.GetValue(response)!;

        Assert.True(recordId > 0);
        Assert.Equal(1, taskId);
    }

    [Fact]
    public async Task ValidData_CreatesRepairRecord()
    {
        await SeedAsync();
        await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());

        var record = await _context.RepairRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
    }

    [Fact]
    public async Task ValidData_SetsTaskStatusToCompleted()
    {
        await SeedAsync();
        await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());

        var task = await _context.RepairTasks.FindAsync(1);
        Assert.Equal(2, task!.Status);
    }

    [Fact]
    public async Task ValidData_SetsAssetRequestStatusToCompleted()
    {
        await SeedAsync();
        await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());

        var req = await _context.AssetRequests.FindAsync(1);
        Assert.Equal(5, req!.Status);
    }

    // ─── ReturnToUseDate ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReturnToUseDateToday_RestoresAssetToInUse()
    {
        await SeedAsync();
        var dto = MinimalDto();
        dto.ReturnToUseDate = DateTime.UtcNow;

        await _controller.CompleteRepair(taskId: 1, dto: dto);

        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.InUse, instance!.Status);
    }

    [Fact]
    public async Task ReturnToUseDateFuture_DoesNotRestoreAsset()
    {
        await SeedAsync();
        var dto = MinimalDto();
        dto.ReturnToUseDate = DateTime.UtcNow.AddDays(10);

        await _controller.CompleteRepair(taskId: 1, dto: dto);

        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.InRepair, instance!.Status);
    }

    [Fact]
    public async Task NoReturnToUseDate_DoesNotRestoreAsset()
    {
        await SeedAsync();
        await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());

        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.InRepair, instance!.Status);
    }

    // ─── AssetLifeCycle ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReturnToUseDateToday_CreatesLifeCycleRecord()
    {
        await SeedAsync();
        var dto = MinimalDto();
        dto.ReturnToUseDate = DateTime.UtcNow;

        await _controller.CompleteRepair(taskId: 1, dto: dto);

        var lifecycle = await _context.AssetLifeCycles.FirstOrDefaultAsync(al => al.AssetInstanceId == 1);
        Assert.NotNull(lifecycle);
    }

    // ─── ProposedData ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidData_AppendsRepairCompletionToProposedData()
    {
        await SeedAsync();
        var dto = MinimalDto();
        dto.ReportNumber = "RPR-001";
        dto.ActualCost = 3000000m;

        await _controller.CompleteRepair(taskId: 1, dto: dto);

        var req = await _context.AssetRequests.FindAsync(1);
        Assert.Contains("repairCompletion", req!.ProposedData ?? "");
        Assert.Contains("RPR-001", req.ProposedData);
        Assert.Contains("3000000", req.ProposedData);
    }

    [Fact]
    public async Task ProposedDataNull_InitializesSuccessfully()
    {
        await SeedAsync();
        var req = await _context.AssetRequests.FindAsync(1);
        req!.ProposedData = null;
        await _context.SaveChangesAsync();

        await _controller.CompleteRepair(taskId: 1, dto: MinimalDto());

        var updated = await _context.AssetRequests.FindAsync(1);
        Assert.Contains("repairCompletion", updated!.ProposedData ?? "");
    }

    // ─── Specific Test Cases ─────────────────────────────────────────────────

    /// <summary>
    /// Test case 1 (Normal): Status = InProgress, RepairDate = today, TaskId = 1,
    /// ReturnToUseDate = RepairDate, ActualCost = 1.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteRepair_NormalCase_ReturnsOk()
    {
        // Arrange
        await SeedAsync();
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = DateTime.UtcNow,
            ReturnToUseDate = DateTime.UtcNow,
            ActualCost = 1,
            Result = "Fixed"
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var task = await _context.RepairTasks.FindAsync(1);
        Assert.Equal(2, task!.Status); // Status = completed
    }

    /// <summary>
    /// Test case 2 (Abnormal): Status = InProgress, RepairDate <= today, TaskId = 1,
    /// ReturnToUseDate = RepairDate, ActualCost = 1.
    /// Expected output: 200 OK (no validation on RepairDate)
    /// </summary>
    [Fact]
    public async Task CompleteRepair_RepairDateInPast_ReturnsOk()
    {
        // Arrange
        await SeedAsync();
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = pastDate,
            ReturnToUseDate = pastDate,
            ActualCost = 1,
            Result = "Fixed"
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): Status = InProgress, RepairDate >= today, TaskId = 1,
    /// ReturnToUseDate = RepairDate, ActualCost = 1.
    /// Expected output: 200 OK (no validation on RepairDate)
    /// </summary>
    [Fact]
    public async Task CompleteRepair_RepairDateInFuture_ReturnsOk()
    {
        // Arrange
        await SeedAsync();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = futureDate,
            ReturnToUseDate = futureDate,
            ActualCost = 1,
            Result = "Fixed"
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): Status = InProgress, RepairDate = today, TaskId = 0,
    /// ReturnToUseDate = RepairDate, ActualCost = 1.
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task CompleteRepair_TaskIdZero_ReturnsNotFound()
    {
        // Arrange
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = DateTime.UtcNow,
            ReturnToUseDate = DateTime.UtcNow,
            ActualCost = 1,
            Result = "Fixed"
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 0, dto: dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal): Status = InProgress, RepairDate = today, TaskId = 999,
    /// ReturnToUseDate = RepairDate, ActualCost = 1.
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task CompleteRepair_TaskIdNonExistent_ReturnsNotFound()
    {
        // Arrange
        await SeedAsync();
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = DateTime.UtcNow,
            ReturnToUseDate = DateTime.UtcNow,
            ActualCost = 1,
            Result = "Fixed"
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 999, dto: dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Abnormal): Status = InProgress, RepairDate = today, TaskId = 1,
    /// ReturnToUseDate <= RepairDate, ActualCost = 1.
    /// Expected output: 200 OK (but asset not restored to InUse because ReturnToUseDate is not in the future)
    /// </summary>
    [Fact]
    public async Task CompleteRepair_ReturnToUseDateInPast_DoesNotRestoreAssetToInUse()
    {
        // Arrange
        await SeedAsync();
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = DateTime.UtcNow,
            ReturnToUseDate = pastDate,
            ActualCost = 1,
            Result = "Fixed"
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.InRepair, instance!.Status);
    }

    /// <summary>
    /// Test case 7 (Abnormal): Status = InProgress, RepairDate = today, TaskId = 1,
    /// ReturnToUseDate >= RepairDate, ActualCost = 1.
    /// Expected output: 200 OK (asset restored to InUse because ReturnToUseDate is today or future)
    /// </summary>
    [Fact]
    public async Task CompleteRepair_ReturnToUseDateTodayOrFuture_RestoresAssetToInUse()
    {
        // Arrange
        await SeedAsync();
        var today = DateTime.UtcNow;
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = today,
            ReturnToUseDate = today,
            ActualCost = 1,
            Result = "Fixed"
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.InUse, instance!.Status);
    }

    /// <summary>
    /// Test case 8 (Abnormal): Status = InProgress, RepairDate = today, TaskId = 1,
    /// ReturnToUseDate = RepairDate, ActualCost = 0.
    /// Expected output: 200 OK (saves record with ActualCost = 0)
    /// </summary>
    [Fact]
    public async Task CompleteRepair_ActualCostZero_ReturnsOk()
    {
        // Arrange
        await SeedAsync();
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = DateTime.UtcNow,
            ReturnToUseDate = DateTime.UtcNow,
            ActualCost = 0,
            Result = "Fixed"
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var record = await _context.RepairRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(0, record.ActualCost);
    }

    /// <summary>
    /// Test case 9 (Abnormal): Status = InProgress, RepairDate = today, TaskId = 1,
    /// ReturnToUseDate = RepairDate, ActualCost = -1.
    /// Expected output: 200 OK (saves record with ActualCost = -1, but RepairWarrantyPeriodValue becomes null)
    /// </summary>
    [Fact]
    public async Task CompleteRepair_ActualCostNegative_ReturnsOk()
    {
        // Arrange
        await SeedAsync();
        var dto = new RepairCompleteDto
        {
            CompletedBy = 1,
            RepairDate = DateTime.UtcNow,
            ReturnToUseDate = DateTime.UtcNow,
            ActualCost = -1,
            Result = "Fixed",
            RepairWarrantyPeriodValue = -1 // This should become null
        };

        // Act
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var record = await _context.RepairRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(-1, record.ActualCost);
        Assert.Null(record.RepairWarrantyPeriodValue); // Should be null because it was -1
    }
}
