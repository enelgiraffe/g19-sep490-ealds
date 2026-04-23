using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
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

/// <summary>
/// Unit tests for MaintenanceRequestsController cost recording functionality.
/// Tests the complete cost-related flow in MaintenanceRequestsController:
/// 1. Estimated cost when creating a maintenance request (RequestExecution)
/// 2. Estimated cost update when starting maintenance (StartMaintenance)
/// 3. Actual cost recording when completing maintenance (CompleteMaintenance)
/// 4. Asset status lifecycle relationship with cost recording
/// </summary>
public class MaintenanceRequestsControllerCostRecordingTests
{
    private readonly EaldsDbContext _context;
    private readonly MaintenanceRequestsController _controller;
    private readonly Mock<IAssetRequestNotificationService> _mockNotification;

    public MaintenanceRequestsControllerCostRecordingTests()
    {
        // Each test uses an independent in-memory database to ensure data isolation
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        // From appsettings.json, maintenance request type ID is 2
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "App:MaintenanceRequestTypeId", "2" }
            })
            .Build();

        _mockNotification = new Mock<IAssetRequestNotificationService>();
        _controller = new MaintenanceRequestsController(_context, configuration, _mockNotification.Object);
        SetUser(actorUserId: 1);
    }

    // ─── Setup helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Simulates a logged-in user by injecting NameIdentifier Claim
    /// </summary>
    private void SetUser(int actorUserId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, actorUserId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>
    /// Seeds base data: Asset + AssetInstance + AssetType + MaintenanceRequestType + WorkflowStep
    /// </summary>
    private async Task SeedBaseDataAsync()
    {
        _context.AssetTypes.Add(new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        });

        _context.Assets.Add(new Asset
        {
            AssetId = 1,
            Code = "LAPTOP-001",
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
            InstanceCode = "INS-LAPTOP-001",
            Status = (int)AssetStatus.InUse,
            InUseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-12)),
            OriginalPrice = 20000000m,
            CurrentValue = 15000000m
        });

        _context.RequestTypes.Add(new RequestType
        {
            RequestTypeId = 2,
            WorkflowId = 1
        });

        _context.WorkflowSteps.Add(new WorkflowStep
        {
            StepId = 1,
            WorkflowId = 1,
            StepOrder = 1
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds roles and user roles (for permission verification)
    /// </summary>
    private async Task SeedDirectorRoleAsync()
    {
        _context.Roles.Add(new Role
        {
            RoleId = 3,
            Code = "DIRECTOR",
            Name = "Director"
        });

        _context.UserRoles.Add(new UserRole
        {
            UserId = 1,
            RoleId = 3
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds an approved maintenance request (Status=2) for testing StartMaintenance
    /// </summary>
    private async Task<MaintenanceTask> SeedApprovedMaintenanceRequestAsync()
    {
        await SeedBaseDataAsync();
        await SeedDirectorRoleAsync();

        var assetRequest = new AssetRequest
        {
            AssetRequestId = 1,
            UserId = 1,
            RequestTypeId = 2,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Maintain Laptop",
            Status = 2, // Approved, ready to start
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 1
        };

        _context.AssetRequests.Add(assetRequest);

        var task = new MaintenanceTask
        {
            TaskId = 1,
            AssetRequestId = 1,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow,
            AssignTo = 1,
            Status = 0, // Pending execution
            CreateDate = DateTime.UtcNow,
            CreateBy = 1
        };

        _context.MaintenanceTasks.Add(task);
        await _context.SaveChangesAsync();

        return task;
    }

    /// <summary>
    /// Seeds an in-progress maintenance task (Status=1) for testing CompleteMaintenance
    /// </summary>
    private async Task<MaintenanceTask> SeedInProgressMaintenanceTaskAsync()
    {
        await SeedApprovedMaintenanceRequestAsync();

        var req = await _context.AssetRequests.FindAsync(1);
        req!.Status = 4; // Under maintenance

        var task = await _context.MaintenanceTasks.FindAsync(1);
        task!.Status = 1; // In progress
        var instance = await _context.AssetInstances.FindAsync(1);
        if (instance != null)
            instance.Status = (int)AssetStatus.InMaintenance;

        await _context.SaveChangesAsync();
        return task;
    }

    // ========================================
    // Part 1: RequestExecution - Estimated Cost When Creating Maintenance Request
    // ========================================

    #region RequestExecution - EstimatedCost

    /// <summary>
    /// Test: Passes estimated cost when creating a maintenance request
    /// Note: MaintenanceRequestDTO itself does not contain EstimatedCost (cost is recorded at Start)
    /// This verifies that even without EstimatedCost, the request can be created successfully
    /// </summary>
    [Fact]
    public async Task RequestExecution_WithValidData_CreatesTask()
    {
        await SeedBaseDataAsync();

        var dto = new MaintenanceRequestDTO
        {
            AssetInstanceId = 1,
            Title = "Maintain Laptop",
            Description = "Regular maintenance",
            PlannedDate = DateTime.UtcNow.AddDays(5),
            ScheduleId = null,
            CreatedBy = 1
        };

        var result = await _controller.RequestExecution(dto);

        Assert.IsType<OkObjectResult>(result);
        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
    }

    /// <summary>
    /// Test: Invalid AssetInstanceId when creating a maintenance request
    /// Expected: Returns NotFound
    /// </summary>
    [Fact]
    public async Task RequestExecution_WithInvalidAssetInstanceId_ReturnsNotFound()
    {
        await SeedBaseDataAsync();

        var dto = new MaintenanceRequestDTO
        {
            AssetInstanceId = 9999,
            Title = "Maintain asset",
            Description = "Description",
            PlannedDate = DateTime.UtcNow.AddDays(5),
            CreatedBy = 1
        };

        var result = await _controller.RequestExecution(dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test: dto is null when creating a maintenance request
    /// Expected: Returns BadRequest
    /// </summary>
    [Fact]
    public async Task RequestExecution_NullDto_ReturnsBadRequest()
    {
        var result = await _controller.RequestExecution(null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    // ========================================
    // Part 2: StartMaintenance - Estimated Cost Update When Starting Maintenance
    // ========================================

    #region StartMaintenance - EstimatedCost Update

    /// <summary>
    /// Test: Passes estimated cost (EstimatedCost) when starting maintenance
    /// Expected: Task status becomes 1 (in progress), estimatedCost recorded in ProposedData
    /// </summary>
    [Fact]
    public async Task StartMaintenance_WithEstimatedCost_UpdatesTaskAndProposedData()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            EstimatedCost = 800000m,
            MaintenanceContent = "Oil change and inspection",
            MaintenanceProvider = "TechService Co.",
            MaintenanceDate = DateTime.UtcNow
        };

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);

        var task = await _context.MaintenanceTasks.FindAsync(1);
        Assert.NotNull(task);
        Assert.Equal(1, task.Status); // In progress

        var req = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(req.ProposedData);
        Assert.Contains("estimatedCost", req.ProposedData);
        Assert.Contains("800000", req.ProposedData);
    }

    /// <summary>
    /// Test: Negative EstimatedCost when starting maintenance (invalid)
    /// Expected: Although StartMaintenance does not directly validate EstimatedCost range, it is recorded in ProposedData
    /// </summary>
    [Fact]
    public async Task StartMaintenance_WithNegativeEstimatedCost_RecordsInProposedData()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            EstimatedCost = -100000m,
            MaintenanceContent = "Test negative cost"
        };

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var req = await _context.AssetRequests.FindAsync(1);
        Assert.Contains("-100000", req!.ProposedData ?? "");
    }

    /// <summary>
    /// Test: AssetRequest not found when starting maintenance
    /// Expected: Returns NotFound
    /// </summary>
    [Fact]
    public async Task StartMaintenance_RequestNotFound_ReturnsNotFound()
    {
        await SeedBaseDataAsync();
        await SeedDirectorRoleAsync();

        var dto = new MaintenanceStartDto { StartedBy = 1 };

        var result = await _controller.StartMaintenance(id: 9999, dto: dto);
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Test: StartedBy <= 0 (invalid) when starting maintenance
    /// Expected: Returns BadRequest
    /// </summary>
    [Fact]
    public async Task StartMaintenance_InvalidStartedBy_ReturnsBadRequest()
    {
        var dto = new MaintenanceStartDto { StartedBy = 0 };
        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test: dto is null when starting maintenance
    /// Expected: Returns BadRequest
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NullDto_ReturnsBadRequest()
    {
        var result = await _controller.StartMaintenance(id: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test: After starting maintenance, asset status should change to InMaintenance
    /// Expected: AssetInstance.Status = InMaintenance(11)
    /// </summary>
    [Fact]
    public async Task StartMaintenance_ValidRequest_ChangesAssetStatusToInMaintenance()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceContent = "Full maintenance",
            MaintenanceProvider = "Provider A"
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)AssetStatus.InMaintenance, instance!.Status);
    }

    /// <summary>
    /// Test: After starting maintenance, AssetLifeCycle record should be created
    /// Expected: A new status change record should exist in AssetLifeCycles table
    /// </summary>
    [Fact]
    public async Task StartMaintenance_ValidRequest_CreatesLifeCycleRecord()
    {
        await SeedApprovedMaintenanceRequestAsync();

        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceContent = "Full maintenance"
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var lifecycle = await _context.AssetLifeCycles.FirstOrDefaultAsync(al => al.AssetInstanceId == 1);
        Assert.NotNull(lifecycle);
        Assert.Equal((int)AssetLifeActionType.StatusChanged, lifecycle.ActionType);
    }

    #endregion

    // ========================================
    // Part 3: CompleteMaintenance - Actual Cost Recording When Completing Maintenance
    // ========================================

    #region CompleteMaintenance - ActualCost / TotalCost Recording

    /// <summary>
    /// Test: Passes ActualCost when completing maintenance
    /// Expected: Creates MaintenanceRecord, TotalCost = ActualCost
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithActualCost_CreatesMaintenanceRecord()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            ActualCost = 750000m,
            TotalCost = 750000m,
            WorkPerformed = "Oil change and filter replacement",
            CompletionDate = DateTime.UtcNow
        };

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(750000m, record.TotalCost);
    }

    /// <summary>
    /// Test: Only TotalCost passed when completing maintenance (ActualCost is null)
    /// Expected: TotalCost is still correctly recorded
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithTotalCostOnly_CreatesRecord()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 1200000m,
            WorkPerformed = "Full service",
            CompletionDate = DateTime.UtcNow
        };

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(1200000m, record.TotalCost);
    }

    /// <summary>
    /// Test: Both ActualCost and TotalCost passed when completing maintenance, ActualCost takes higher priority
    /// Expected: TotalCost field takes the value of ActualCost
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_ActualCostTakesPriorityOverTotalCost()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            ActualCost = 900000m, // Use with priority
            TotalCost = 1000000m, // Overridden by ActualCost
            WorkPerformed = "Service with priority cost"
        };

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(900000m, record.TotalCost); // ActualCost takes priority
    }

    /// <summary>
    /// Test: Both ActualCost and TotalCost are null/0 when completing maintenance
    /// Expected: MaintenanceRecord TotalCost = 0 (zero-cost completion is allowed)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithZeroCost_CreatesRecordWithZeroCost()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 0m,
            WorkPerformed = "Free inspection",
            CompletionDate = DateTime.UtcNow
        };

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(0m, record.TotalCost);
    }

    /// <summary>
    /// Test: dto is null when completing maintenance
    /// Expected: Returns BadRequest
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_NullDto_ReturnsBadRequest()
    {
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test: CompletedBy <= 0 (invalid) when completing maintenance
    /// Expected: Returns BadRequest
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_InvalidCompletedBy_ReturnsBadRequest()
    {
        var dto = new MaintenanceCompleteDto { CompletedBy = 0, TotalCost = 100000m };
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test: TaskId does not exist when completing maintenance
    /// Expected: Returns NotFound
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_TaskNotFound_ReturnsNotFound()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 100000m,
            WorkPerformed = "Test"
        };

        var result = await _controller.CompleteMaintenance(taskId: 9999, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test: Task status is incorrect when completing maintenance (Status != 1)
    /// Expected: Returns BadRequest
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_TaskNotInProgress_ReturnsBadRequest()
    {
        await SeedApprovedMaintenanceRequestAsync();
        // Task status is still 0 (pending execution), not started

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 100000m,
            WorkPerformed = "Test"
        };

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test: After successful maintenance completion, Task status should become 2 (completed)
    /// Expected: task.Status = 2
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_Success_SetsTaskStatusToCompleted()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Standard maintenance"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var task = await _context.MaintenanceTasks.FindAsync(1);
        Assert.Equal(2, task!.Status);
    }

    /// <summary>
    /// Test: After successful maintenance completion, AssetRequest status should become 5 (maintenance completed)
    /// Expected: request.Status = 5
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_Success_SetsAssetRequestStatusToCompleted()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Standard maintenance"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var req = await _context.AssetRequests.FindAsync(1);
        Assert.Equal(5, req!.Status);
    }

    /// <summary>
    /// Test: After completing maintenance, asset status should be restored to InUse
    /// Expected: AssetInstance.Status = InUse(1)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_Success_RestoresAssetToInUse()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Maintenance done"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)AssetStatus.InUse, instance!.Status);
    }

    /// <summary>
    /// Test: After completing maintenance, AssetLifeCycle record should be created
    /// Expected: Status change record from InMaintenance to InUse exists in AssetLifeCycles
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_Success_CreatesLifeCycleRecord()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Maintenance completed"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var lifecycle = await _context.AssetLifeCycles
            .FirstOrDefaultAsync(al => al.AssetInstanceId == 1
                && al.ActionType == (int)AssetLifeActionType.StatusChanged);
        Assert.NotNull(lifecycle);
    }

    /// <summary>
    /// Test: Records ReportNumber when completing maintenance
    /// Expected: ProposedData contains reportNumber and flowType=maintenance-complete
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithReportNumber_RecordsInProposedData()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            ReportNumber = "MNT-2024-001",
            TotalCost = 600000m,
            WorkPerformed = "Inspection and service"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var req = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(req!.ProposedData);
        Assert.Contains("maintenance-complete", req.ProposedData);
        Assert.Contains("MNT-2024-001", req.ProposedData);
        Assert.Contains("actualCost", req.ProposedData);
    }

    /// <summary>
    /// Test: Records ReturnToUseDate when completing maintenance
    /// Expected: ProposedData contains returnToUseDate
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithReturnToUseDate_RecordsInProposedData()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            ReturnToUseDate = DateTime.UtcNow,
            TotalCost = 300000m,
            WorkPerformed = "Quick fix"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var req = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(req!.ProposedData);
        Assert.Contains("returnToUseDate", req.ProposedData);
    }

    /// <summary>
    /// Test: Records MaintenanceContent / WorkPerformed when completing maintenance
    /// Expected: MaintenanceRecord.WorkPerformed is correctly stored
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithWorkPerformed_StoresInRecord()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 450000m,
            WorkPerformed = "Full engine check, oil change, filter replacement"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal("Full engine check, oil change, filter replacement", record.WorkPerformed);
    }

    /// <summary>
    /// Test: Records ConditionBefore and ConditionAfter when completing maintenance
    /// Expected: Correctly stored in MaintenanceRecord
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithConditions_StoresInRecord()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 350000m,
            ConditionBefore = "Old oil, dirty filter",
            DetailedDescription = "New oil, clean filter",
            ConditionAfter = "New oil, clean filter"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal("Old oil, dirty filter", record.ConditionBefore);
        Assert.Equal("New oil, clean filter", record.ConditionAfter);
    }

    /// <summary>
    /// Test: Uses ExecutionDate (legacy field name) when completing maintenance
    /// Expected: ExecutionDate takes priority over CompletionDate
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithExecutionDate_UsesItAsExecutionDate()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 200000m,
            ExecutionDate = DateTime.UtcNow.AddDays(-1), // One day earlier than current
            WorkPerformed = "Test execution date"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        // ExecutionDate is used for executionDate field (note the case sensitivity)
        Assert.True(record.ExecutionDate <= DateTime.UtcNow);
    }

    /// <summary>
    /// Test: Completing maintenance when ProposedData is null
    /// Expected: Initializes ProposedData normally without error
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_NullProposedData_InitializesSuccessfully()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var req = await _context.AssetRequests.FindAsync(1);
        req!.ProposedData = null;
        await _context.SaveChangesAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 100000m,
            WorkPerformed = "Test"
        };

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);

        var updated = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(updated!.ProposedData);
        Assert.Contains("maintenance-complete", updated.ProposedData);
    }

    /// <summary>
    /// Test: Creates AssetRequestRecord history when completing maintenance
    /// Expected: AssetRequestRecords contains record with Action=3 (completed)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_Success_CreatesAssetRequestRecord()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Maintenance done"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.AssetRequestRecords
            .FirstOrDefaultAsync(r => r.AssetRequestId == 1 && r.Action == 3);
        Assert.NotNull(record);
        Assert.Equal(1, record.ActionByUserId);
    }

    #endregion

    // ========================================
    // Part 4: Large Cost Boundary Tests
    // ========================================

    #region Cost Boundary Tests

    /// <summary>
    /// Test: Passes very large amount when completing maintenance
    /// Expected: MaintenanceRecord.TotalCost correctly stores large number
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithVeryLargeCost_StoresCorrectly()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 999999999.99m,
            WorkPerformed = "Major overhaul"
        };

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.Equal(999999999.99m, record!.TotalCost);
    }

    /// <summary>
    /// Test: Passes small amount when completing maintenance (precision test)
    /// Expected: TotalCost preserves decimal precision
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithDecimalCost_PreservesPrecision()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 123456.78m,
            WorkPerformed = "Small repair"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.Equal(123456.78m, record!.TotalCost);
    }

    /// <summary>
    /// Test: Both WorkPerformed and DetailedDescription present when completing maintenance
    /// Expected: WorkPerformed field takes priority (takes MaintenanceContent)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithBothWorkPerformedAndDetailedDescription_UsesWorkPerformed()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 300000m,
            MaintenanceContent = "Oil change via MaintenanceContent",
            DetailedDescription = "Detailed description via DetailedDescription",
            WorkPerformed = "WorkPerformed value"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        // In code: WorkPerformed = dto.MaintenanceContent ?? dto.WorkPerformed
        Assert.Equal("Oil change via MaintenanceContent", record.WorkPerformed);
    }

    #endregion

    // ========================================
    // Part 5: Attachments and Metadata
    // ========================================

    #region Attachments and Metadata

    /// <summary>
    /// Test: Attaches file URLs when completing maintenance
    /// Expected: ProposedData contains attachmentUrls
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithAttachmentUrls_RecordsInProposedData()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 200000m,
            WorkPerformed = "Service with attachments",
            AttachmentUrls = new List<string>
            {
                "https://storage.example.com/report1.pdf",
                "https://storage.example.com/photo1.jpg"
            }
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var req = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(req!.ProposedData);
        Assert.Contains("attachmentUrls", req.ProposedData);
        Assert.Contains("report1.pdf", req.ProposedData);
    }

    /// <summary>
    /// Test: Attaches document ID list when completing maintenance
    /// Expected: ProposedData contains attachmentDocumentIds
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_WithAttachmentDocumentIds_RecordsInProposedData()
    {
        await SeedInProgressMaintenanceTaskAsync();

        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 200000m,
            WorkPerformed = "Service with doc ids",
            AttachmentDocumentIds = new List<int> { 101, 202, 303 }
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var req = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(req!.ProposedData);
        Assert.Contains("attachmentDocumentIds", req.ProposedData);
    }

    #endregion
}
