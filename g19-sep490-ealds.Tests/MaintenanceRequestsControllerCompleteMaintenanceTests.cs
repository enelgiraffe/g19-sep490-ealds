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
/// Unit tests for MaintenanceRequestsController.CompleteMaintenance
/// (POST /api/Assets/Requests/maintenance/tasks/{taskId}/complete)
/// </summary>
public class MaintenanceRequestsControllerCompleteMaintenanceTests
{
    private readonly EaldsDbContext _context;
    private readonly MaintenanceRequestsController _controller;
    private readonly Mock<IAssetRequestNotificationService> _mockNotification;

    public MaintenanceRequestsControllerCompleteMaintenanceTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "App:MaintenanceRequestTypeId", "2" }
            })
            .Build();

        _mockNotification = new Mock<IAssetRequestNotificationService>();
        _controller = new MaintenanceRequestsController(_context, configuration, _mockNotification.Object);
        SetUser(actorUserId: 1);
    }

    private void SetUser(int actorUserId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, actorUserId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private async Task SeedWorkflowAsync()
    {
        _context.RequestTypes.Add(new RequestType
        {
            RequestTypeId = 2,
            WorkflowId = 1
        });

        _context.WorkflowSteps.Add(new WorkflowStep
        {
            StepId = 10,
            WorkflowId = 1,
            StepOrder = 1,
            RoleId = 1,
            IsFinalStep = true
        });

        await _context.SaveChangesAsync();
    }

    private async Task SeedMinimalAsync()
    {
        await SeedWorkflowAsync();

        _context.Assets.Add(new Asset
        {
            AssetId = 1,
            Code = "DELL-001",
            Name = "Dell Laptop 001",
            AssetTypeId = 1,
            Status = 1,
            Unit = "pcs"
        });

        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 1,
            AssetId = 1,
            WarehouseId = 1,
            InstanceCode = "INS-001",
            Status = (int)AssetStatus.InMaintenance,
            Note = "Under maintenance"
        });

        _context.Employees.Add(new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Nguyen Van A",
            Code = "NV001",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });

        _context.Departments.Add(new Department
        {
            DepartmentId = 1,
            Name = "IT Department"
        });

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

    private async Task SeedInProgressMaintenanceAsync()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 1,
            UserId = 1,
            RequestTypeId = 2,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Maintenance Dell Laptop",
            Status = 4, // In Progress
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 1,
            AssetRequestId = 1,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(-1),
            AssignTo = 1,
            Address = "IT Department",
            Status = 1, // In Progress
            CreateDate = DateTime.UtcNow.AddDays(-2),
            CreateBy = 1
        });

        await _context.SaveChangesAsync();
    }

    private MaintenanceCompleteDto MinimalDto() => new MaintenanceCompleteDto
    {
        CompletedBy = 1,
        CompletionDate = DateTime.UtcNow,
        ActualCost = 100000m,
        WorkPerformed = "Regular maintenance completed",
        ConditionAfter = "Asset in good condition"
    };

    /// <summary>
    /// Test case 1 (Normal):
    /// ExecutionDate = today, ActualCost = 1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_NormalCase_ExecutionDateToday_ReturnsOk()
    {
        // Arrange
        await SeedInProgressMaintenanceAsync();
        var today = DateTime.UtcNow;
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = today, // ExecutionDate = today
            ActualCost = 1, // ActualCost = 1
            WorkPerformed = "Regular maintenance completed", // Valid
            ConditionAfter = "Asset in good condition" // Valid
        };

        // Act
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(1, record.TotalCost);
        Assert.Equal("Regular maintenance completed", record.WorkPerformed);
        Assert.Equal("Asset in good condition", record.ConditionAfter);
    }

    /// <summary>
    /// Test case 2 (Normal):
    /// ExecutionDate = less than today, ActualCost = 1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_NormalCase_ExecutionDateInPast_ReturnsOk()
    {
        // Arrange
        await SeedInProgressMaintenanceAsync();
        var pastDate = DateTime.UtcNow.AddDays(-2); // Less than today
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = pastDate, // ExecutionDate = less than today
            ActualCost = 1,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        // Act
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(pastDate.Date, record.ExecutionDate.Date);
        Assert.Equal(1, record.TotalCost);
    }

    /// <summary>
    /// Test case 3 (Normal):
    /// ExecutionDate = greater than today, ActualCost = 1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_NormalCase_ExecutionDateInFuture_ReturnsOk()
    {
        // Arrange
        await SeedInProgressMaintenanceAsync();
        var futureDate = DateTime.UtcNow.AddDays(2); // Greater than today
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = futureDate, // ExecutionDate = greater than today
            ActualCost = 1,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        // Act
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(futureDate.Date, record.ExecutionDate.Date);
        Assert.Equal(1, record.TotalCost);
    }

    /// <summary>
    /// Test case 4 (Abnormal):
    /// ExecutionDate = today, ActualCost = 0, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK (no validation on ActualCost being 0)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_AbnormalCase_ActualCostZero_ReturnsOk()
    {
        // Arrange
        await SeedInProgressMaintenanceAsync();
        var today = DateTime.UtcNow;
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = today,
            ActualCost = 0, // ActualCost = 0
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        // Act
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(0, record.TotalCost);
    }

    /// <summary>
    /// Test case 5 (Boundary):
    /// ExecutionDate = today, ActualCost = 0.1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_BoundaryCase_ActualCostMinValue_ReturnsOk()
    {
        // Arrange
        await SeedInProgressMaintenanceAsync();
        var today = DateTime.UtcNow;
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = today,
            ActualCost = 0.1m, // ActualCost = 0.1 (minimum valid)
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        // Act
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(0.1m, record.TotalCost);
    }

    /// <summary>
    /// Test case 6 (Abnormal):
    /// ExecutionDate = today, ActualCost = -1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK (no validation on ActualCost being negative)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_AbnormalCase_ActualCostNegative_ReturnsOk()
    {
        // Arrange
        await SeedInProgressMaintenanceAsync();
        var today = DateTime.UtcNow;
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = today,
            ActualCost = -1, // ActualCost = -1
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        // Act
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(-1, record.TotalCost);
    }

    /// <summary>
    /// Test case 7 (Abnormal):
    /// ExecutionDate = today, ActualCost = 1, WorkPerformed = Empty, ConditionAfter = Valid.
    /// Expected output: 200 OK (no validation on WorkPerformed being empty)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_AbnormalCase_WorkPerformedEmpty_ReturnsOk()
    {
        // Arrange
        await SeedInProgressMaintenanceAsync();
        var today = DateTime.UtcNow;
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = today,
            ActualCost = 1,
            WorkPerformed = "", // Empty
            ConditionAfter = "Asset in good condition"
        };

        // Act
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal("", record.WorkPerformed);
    }

    /// <summary>
    /// Test case 8 (Abnormal):
    /// ExecutionDate = today, ActualCost = 1, WorkPerformed = Valid, ConditionAfter = Empty.
    /// Expected output: 200 OK (no validation on ConditionAfter being empty)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_AbnormalCase_ConditionAfterEmpty_ReturnsOk()
    {
        // Arrange
        await SeedInProgressMaintenanceAsync();
        var today = DateTime.UtcNow;
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = today,
            ActualCost = 1,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "" // Empty
        };

        // Act
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal("", record.ConditionAfter);
    }

    // ─── Additional validation tests ─────────────────────────────────────────

    /// <summary>
    /// Input:  dto = null
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        await SeedInProgressMaintenanceAsync();
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  CompletedBy = 0
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task CompletedByZero_ReturnsBadRequest()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto { CompletedBy = 0 };
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  CompletedBy = -1
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task CompletedByNegative_ReturnsBadRequest()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto { CompletedBy = -1 };
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Task with id = 999 (does not exist)
    /// Expected return: NotFoundObjectResult
    /// </summary>
    [Fact]
    public async Task TaskNotFound_ReturnsNotFound()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto { CompletedBy = 1 };
        var result = await _controller.CompleteMaintenance(taskId: 999, dto: dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Input:  Task with Status = 0 (Pending, not started)
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task TaskStatusPending_ReturnsBadRequest()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 2,
            UserId = 1,
            RequestTypeId = 2,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Maintenance Request",
            Status = 4,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 2,
            AssetRequestId = 2,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(-1),
            AssignTo = 1,
            Status = 0, // Pending - not started
            CreateDate = DateTime.UtcNow.AddDays(-2),
            CreateBy = 1
        });

        await _context.SaveChangesAsync();

        var dto = new MaintenanceCompleteDto { CompletedBy = 1 };
        var result = await _controller.CompleteMaintenance(taskId: 2, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Task with Status = 2 (Completed)
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task TaskStatusCompleted_ReturnsBadRequest()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 3,
            UserId = 1,
            RequestTypeId = 2,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Maintenance Request",
            Status = 5,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 3,
            AssetRequestId = 3,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(-3),
            AssignTo = 1,
            Status = 2, // Completed
            CreateDate = DateTime.UtcNow.AddDays(-5),
            CreateBy = 1
        });

        await _context.SaveChangesAsync();

        var dto = new MaintenanceCompleteDto { CompletedBy = 1 };
        var result = await _controller.CompleteMaintenance(taskId: 3, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  AssetRequest with Status != 4 (not in progress)
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task AssetRequestStatusNotInProgress_ReturnsBadRequest()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 4,
            UserId = 1,
            RequestTypeId = 2,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Maintenance Request",
            Status = 2, // Approved but not started
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 4,
            AssetRequestId = 4,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(-1),
            AssignTo = 1,
            Status = 1, // In Progress
            CreateDate = DateTime.UtcNow.AddDays(-2),
            CreateBy = 1
        });

        await _context.SaveChangesAsync();

        var dto = new MaintenanceCompleteDto { CompletedBy = 1 };
        var result = await _controller.CompleteMaintenance(taskId: 4, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request, sets task status to Completed (2)
    /// Expected return: MaintenanceTask status = 2 (Completed)
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsTaskStatusToCompleted()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var task = await _context.MaintenanceTasks.FindAsync(1);
        Assert.Equal(2, task!.Status); // Completed
    }

    /// <summary>
    /// Input:  Valid request, sets AssetRequest status to Completed (5)
    /// Expected return: AssetRequest status = 5
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsAssetRequestStatusToCompleted()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var request = await _context.AssetRequests.FindAsync(1);
        Assert.Equal(5, request!.Status); // Completed
    }

    /// <summary>
    /// Input:  Valid request, creates MaintenanceRecord
    /// Expected return: MaintenanceRecord is persisted
    /// </summary>
    [Fact]
    public async Task ValidRequest_CreatesMaintenanceRecord()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 150000m,
            WorkPerformed = "Full inspection",
            ConditionAfter = "All systems operational"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(150000m, record.TotalCost);
        Assert.Equal("Full inspection", record.WorkPerformed);
        Assert.Equal("All systems operational", record.ConditionAfter);
        Assert.Equal(1, record.Status); // Record status = active
    }

    /// <summary>
    /// Input:  Valid request, updates asset status to InUse
    /// Expected return: AssetInstance status = InUse
    /// </summary>
    [Fact]
    public async Task ValidRequest_UpdatesAssetStatusToInUse()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)AssetStatus.InUse, instance!.Status);
    }

    /// <summary>
    /// Input:  MaintenanceContent provided instead of WorkPerformed
    /// Expected return: WorkPerformed uses MaintenanceContent value
    /// </summary>
    [Fact]
    public async Task MaintenanceContentProvided_UsesAsWorkPerformed()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            MaintenanceContent = "Oil change and filter replacement", // Used instead of WorkPerformed
            WorkPerformed = null,
            ConditionAfter = "Engine running smooth"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal("Oil change and filter replacement", record.WorkPerformed);
    }

    /// <summary>
    /// Input:  DetailedDescription provided instead of ConditionAfter
    /// Expected return: ConditionAfter uses DetailedDescription value
    /// </summary>
    [Fact]
    public async Task DetailedDescriptionProvided_UsesAsConditionAfter()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            WorkPerformed = "Inspection completed",
            DetailedDescription = "All components checked and working", // Used instead of ConditionAfter
            ConditionAfter = null
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal("All components checked and working", record.ConditionAfter);
    }

    /// <summary>
    /// Input:  ReturnToUseDate provided
    /// Expected return: ProposedData contains returnToUseDate
    /// </summary>
    [Fact]
    public async Task ReturnToUseDateProvided_StoredInProposedData()
    {
        await SeedInProgressMaintenanceAsync();
        var returnDate = DateTime.UtcNow.AddDays(1);
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition",
            ReturnToUseDate = returnDate
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var request = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(request!.ProposedData);
        Assert.Contains("returnToUseDate", request.ProposedData);
    }

    /// <summary>
    /// Input:  TotalCost used when ActualCost is null
    /// Expected return: Record TotalCost = TotalCost value
    /// </summary>
    [Fact]
    public async Task TotalCostUsed_WhenActualCostNull()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = null, // Not provided
            TotalCost = 200000m, // Use this instead
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(200000m, record.TotalCost);
    }

    /// <summary>
    /// Input:  ExecutionDate used when CompletionDate is null
    /// Expected return: Record ExecutionDate = ExecutionDate value
    /// </summary>
    [Fact]
    public async Task ExecutionDateUsed_WhenCompletionDateNull()
    {
        await SeedInProgressMaintenanceAsync();
        var execDate = DateTime.UtcNow.AddDays(-1);
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = null, // Not provided
            ExecutionDate = execDate, // Use this instead
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal(execDate.Date, record.ExecutionDate.Date);
    }

    /// <summary>
    /// Input:  Both CompletionDate and ExecutionDate null
    /// Expected return: ExecutionDate = DateTime.UtcNow
    /// </summary>
    [Fact]
    public async Task BothDatesNull_UsesCurrentTime()
    {
        await SeedInProgressMaintenanceAsync();
        var beforeCall = DateTime.UtcNow;
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = null,
            ExecutionDate = null,
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.True(record.ExecutionDate >= beforeCall);
    }

    /// <summary>
    /// Input:  Valid request, creates AssetRequestRecord
    /// Expected return: Record with Action = 3 (Complete)
    /// </summary>
    [Fact]
    public async Task ValidRequest_CreatesAssetRequestRecord()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.AssetRequestRecords.FirstOrDefaultAsync(r => r.AssetRequestId == 1);
        Assert.NotNull(record);
        Assert.Equal(3, record.Action); // Complete action
        Assert.Equal(4, record.FromStatus); // In Progress
        Assert.Equal(5, record.ToStatus); // Completed
    }

    /// <summary>
    /// Input:  ConditionBefore provided
    /// Expected return: Record ConditionBefore is set
    /// </summary>
    [Fact]
    public async Task ConditionBeforeProvided_StoredInRecord()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionBefore = "Worn components",
            ConditionAfter = "Replaced and working"
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var record = await _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.TaskId == 1);
        Assert.NotNull(record);
        Assert.Equal("Worn components", record.ConditionBefore);
    }

    /// <summary>
    /// Input:  AttachmentUrls provided
    /// Expected return: ProposedData contains attachmentUrls
    /// </summary>
    [Fact]
    public async Task AttachmentUrlsProvided_StoredInProposedData()
    {
        await SeedInProgressMaintenanceAsync();
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition",
            AttachmentUrls = new List<string> { "http://example.com/report.pdf" }
        };

        await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        var request = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(request!.ProposedData);
        Assert.Contains("attachmentUrls", request.ProposedData);
    }
}
