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
/// Unit tests for MaintenanceRequestsController.StartMaintenance
/// (POST /api/Assets/Requests/maintenance/{id}/start)
/// </summary>
public class MaintenanceRequestsControllerStartMaintenanceTests
{
    private readonly EaldsDbContext _context;
    private readonly MaintenanceRequestsController _controller;
    private readonly Mock<IAssetRequestNotificationService> _mockNotification;

    public MaintenanceRequestsControllerStartMaintenanceTests()
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
            Status = (int)AssetStatus.InUse,
            Note = "Scheduled maintenance"
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

    private async Task SeedApprovedMaintenanceRequestAsync()
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
            Status = 2, // Approved
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.Approvals.Add(new Approval
        {
            AssetRequestId = 1,
            StepId = 10,
            ApprovedRoleId = 3,
            Decision = 1, // Approved
            ApprovedByUserId = 1,
            DecisionDate = DateTime.UtcNow.AddDays(-1)
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 1,
            AssetRequestId = 1,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(1),
            AssignTo = 1,
            Address = "IT Department",
            Status = 0, // Pending
            CreateDate = DateTime.UtcNow,
            CreateBy = 1
        });

        await _context.SaveChangesAsync();
    }

    private MaintenanceStartDto MinimalDto() => new MaintenanceStartDto
    {
        StartedBy = 1,
        Comment = "Starting maintenance",
        MaintenanceDate = DateTime.UtcNow,
        ExpectedCompletionDate = DateTime.UtcNow,
        Location = "IT Department",
        LocationType = "at-unit",
        MaintenanceContent = "Regular maintenance",
        MaintenanceProvider = null
    };

    /// <summary>
    /// Test case 1 (Normal):
    /// PlannedDate = today, ExpectedCompletionDate = PlannedDate, Address = at unit.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_PlannedDateToday_ReturnsOk()
    {
        // Arrange
        await SeedApprovedMaintenanceRequestAsync();
        var today = DateTime.UtcNow;
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today, // Equal to today
            ExpectedCompletionDate = today, // Equal to PlannedDate
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        // Act
        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(today.Date, task.PlannedDate.Date);
        Assert.Equal(today.Date, task.ExpectedCompletionDate?.Date);
        Assert.Equal("at-unit", task.LocationType);
        Assert.Equal("IT Department", task.Address);
        Assert.Equal(1, task.Status); // In-progress
    }

    /// <summary>
    /// Test case 2 (Normal):
    /// PlannedDate = less than today, ExpectedCompletionDate = PlannedDate, Address = at unit.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_PlannedDateInPast_ReturnsOk()
    {
        // Arrange
        await SeedApprovedMaintenanceRequestAsync();
        var pastDate = DateTime.UtcNow.AddDays(-2); // Less than today
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = pastDate, // Less than today
            ExpectedCompletionDate = pastDate, // Equal to PlannedDate
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        // Act
        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(pastDate.Date, task.PlannedDate.Date);
        Assert.Equal(pastDate.Date, task.ExpectedCompletionDate?.Date);
        Assert.Equal("at-unit", task.LocationType);
    }

    /// <summary>
    /// Test case 3 (Normal):
    /// PlannedDate = greater than today, ExpectedCompletionDate = PlannedDate, Address = at unit.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_PlannedDateInFuture_ReturnsOk()
    {
        // Arrange
        await SeedApprovedMaintenanceRequestAsync();
        var futureDate = DateTime.UtcNow.AddDays(5); // Greater than today
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = futureDate, // Greater than today
            ExpectedCompletionDate = futureDate, // Equal to PlannedDate
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        // Act
        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(futureDate.Date, task.PlannedDate.Date);
        Assert.Equal(futureDate.Date, task.ExpectedCompletionDate?.Date);
        Assert.Equal("at-unit", task.LocationType);
    }

    /// <summary>
    /// Test case 4 (Normal):
    /// PlannedDate = today, ExpectedCompletionDate = less than PlannedDate, Address = at unit.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_ExpectedCompletionBeforePlannedDate_ReturnsOk()
    {
        // Arrange
        await SeedApprovedMaintenanceRequestAsync();
        var today = DateTime.UtcNow;
        var earlyCompletion = today.AddDays(-1); // Less than PlannedDate
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today, // Today
            ExpectedCompletionDate = earlyCompletion, // Less than PlannedDate
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        // Act
        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(today.Date, task.PlannedDate.Date);
        Assert.Equal(earlyCompletion.Date, task.ExpectedCompletionDate?.Date);
        Assert.Equal("at-unit", task.LocationType);
    }

    /// <summary>
    /// Test case 5 (Abnormal):
    /// PlannedDate = today, ExpectedCompletionDate = greater than PlannedDate, Address = at unit.
    /// Expected output: 200 OK (no validation on date relationships)
    /// </summary>
    [Fact]
    public async Task StartMaintenance_AbnormalCase_ExpectedCompletionAfterPlannedDate_ReturnsOk()
    {
        // Arrange
        await SeedApprovedMaintenanceRequestAsync();
        var today = DateTime.UtcNow;
        var laterCompletion = today.AddDays(3); // Greater than PlannedDate
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today, // Today
            ExpectedCompletionDate = laterCompletion, // Greater than PlannedDate
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        // Act
        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(today.Date, task.PlannedDate.Date);
        Assert.Equal(laterCompletion.Date, task.ExpectedCompletionDate?.Date);
        Assert.Equal("at-unit", task.LocationType);
    }

    /// <summary>
    /// Test case 6 (Normal):
    /// PlannedDate = today, ExpectedCompletionDate = PlannedDate, Address = provider.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_LocationTypeProvider_ReturnsOk()
    {
        // Arrange
        await SeedApprovedMaintenanceRequestAsync();
        var today = DateTime.UtcNow;
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today,
            ExpectedCompletionDate = today,
            LocationType = "provider",
            Location = "External Repair Shop A",
            MaintenanceContent = "External maintenance",
            MaintenanceProvider = "Repair Shop A"
        };

        // Act
        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(today.Date, task.PlannedDate.Date);
        Assert.Equal(today.Date, task.ExpectedCompletionDate?.Date);
        Assert.Equal("provider", task.LocationType);
        Assert.Equal("External Repair Shop A", task.Address);
        Assert.Equal("Repair Shop A", task.MaintenanceProvider);
    }

    // ─── Additional validation tests ─────────────────────────────────────────

    /// <summary>
    /// Input:  dto = null
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var result = await _controller.StartMaintenance(id: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  StartedBy = 0
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task StartedByZero_ReturnsBadRequest()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto { StartedBy = 0 };
        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  StartedBy = -1
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task StartedByNegative_ReturnsBadRequest()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto { StartedBy = -1 };
        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  AssetRequest with id = 999 (does not exist)
    /// Expected return: NotFoundObjectResult
    /// </summary>
    [Fact]
    public async Task RequestNotFound_ReturnsNotFound()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto { StartedBy = 1 };
        var result = await _controller.StartMaintenance(id: 999, dto: dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Input:  AssetRequest with Status = 1 (Pending Approval), not final approved
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task RequestNotApproved_ReturnsBadRequest()
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
            Status = 1, // Pending Approval (not final approved)
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 2,
            AssetRequestId = 2,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(1),
            AssignTo = 1,
            Status = 0,
            CreateDate = DateTime.UtcNow,
            CreateBy = 1
        });

        await _context.SaveChangesAsync();

        var dto = new MaintenanceStartDto { StartedBy = 1 };
        var result = await _controller.StartMaintenance(id: 2, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  User without allowed role
    /// Expected return: StatusCode 403 Forbidden
    /// </summary>
    [Fact]
    public async Task UserWithoutRole_ReturnsForbidden()
    {
        await SeedMinimalAsync();

        _context.Roles.Add(new Role
        {
            RoleId = 10,
            Code = "USER",
            Name = "Regular User"
        });

        _context.UserRoles.Add(new UserRole
        {
            UserId = 1,
            RoleId = 10
        });

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 3,
            UserId = 2, // Different user
            RequestTypeId = 2,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Maintenance Request",
            Status = 2,
            CreatedBy = 2,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.Approvals.Add(new Approval
        {
            AssetRequestId = 3,
            StepId = 10,
            ApprovedRoleId = 3,
            Decision = 1,
            ApprovedByUserId = 1,
            DecisionDate = DateTime.UtcNow.AddDays(-1)
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 3,
            AssetRequestId = 3,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(1),
            AssignTo = 2, // Assigned to user 2
            Status = 0,
            CreateDate = DateTime.UtcNow,
            CreateBy = 2
        });

        await _context.SaveChangesAsync();

        var dto = new MaintenanceStartDto { StartedBy = 1 };
        var result = await _controller.StartMaintenance(id: 3, dto: dto);
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, (result as ObjectResult)?.StatusCode);
    }

    /// <summary>
    /// Input:  User with allowed role DIRECTOR
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task UserWithDirectorRole_ReturnsOk()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  User is the creator of the request
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task UserIsCreator_ReturnsOk()
    {
        await SeedMinimalAsync();

        _context.Roles.Add(new Role
        {
            RoleId = 10,
            Code = "USER",
            Name = "Regular User"
        });

        _context.UserRoles.Add(new UserRole
        {
            UserId = 1,
            RoleId = 10
        });

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 4,
            UserId = 1, // Same user
            RequestTypeId = 2,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Maintenance Request",
            Status = 2,
            CreatedBy = 1, // User 1 created
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.Approvals.Add(new Approval
        {
            AssetRequestId = 4,
            StepId = 10,
            ApprovedRoleId = 3,
            Decision = 1,
            ApprovedByUserId = 1,
            DecisionDate = DateTime.UtcNow.AddDays(-1)
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 4,
            AssetRequestId = 4,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(1),
            AssignTo = 1,
            Status = 0,
            CreateDate = DateTime.UtcNow,
            CreateBy = 1
        });

        await _context.SaveChangesAsync();

        var dto = new MaintenanceStartDto { StartedBy = 1 };
        var result = await _controller.StartMaintenance(id: 4, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request, updates asset status to InMaintenance
    /// Expected return: AssetInstance status changes to InMaintenance
    /// </summary>
    [Fact]
    public async Task ValidRequest_UpdatesAssetStatusToInMaintenance()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)AssetStatus.InMaintenance, instance!.Status);
    }

    /// <summary>
    /// Input:  Valid request, sets task status to InProgress (1)
    /// Expected return: MaintenanceTask status = 1 (InProgress)
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsTaskStatusToInProgress()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(1, task.Status); // InProgress
    }

    /// <summary>
    /// Input:  Valid request, sets AssetRequest status to 4 (In Progress)
    /// Expected return: AssetRequest status = 4
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsAssetRequestStatusToInProgress()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var request = await _context.AssetRequests.FindAsync(1);
        Assert.Equal(4, request!.Status); // In Progress
    }

    /// <summary>
    /// Input:  Valid request with maintenance provider
    /// Expected return: OkObjectResult with provider linked to task
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithMaintenanceProvider_ReturnsOk()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "provider",
            Location = "External Shop",
            MaintenanceProvider = "Repair Shop A"
        };

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal("Repair Shop A", task.MaintenanceProvider);
    }

    /// <summary>
    /// Input:  Valid request with PerformerUserId
    /// Expected return: Task assign updated to performer
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithPerformerUserId_SetsAssignTo()
    {
        await SeedApprovedMaintenanceRequestAsync();

        _context.Employees.Add(new Employee
        {
            EmployeeId = 2,
            UserId = 2,
            DepartmentId = 1,
            Name = "Nguyen Van B",
            Code = "NV002",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });

        _context.UserRoles.Add(new UserRole { UserId = 2, RoleId = 3 });

        await _context.SaveChangesAsync();

        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            PerformerUserId = 2,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(2, task.AssignTo);
        Assert.Equal(2, task.PerformerUserId);
    }

    /// <summary>
    /// Input:  Request with Status = 4 (already in progress)
    /// Expected return: OkObjectResult (idempotent - restarts maintenance)
    /// </summary>
    [Fact]
    public async Task AlreadyInProgress_ReturnsOk()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 5,
            UserId = 1,
            RequestTypeId = 2,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Maintenance Request",
            Status = 4, // Already in progress
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.MaintenanceTasks.Add(new MaintenanceTask
        {
            TaskId = 5,
            AssetRequestId = 5,
            AssetInstanceId = 1,
            PlannedDate = DateTime.UtcNow.AddDays(1),
            AssignTo = 1,
            Status = 1, // In progress
            CreateDate = DateTime.UtcNow,
            CreateBy = 1
        });

        await _context.SaveChangesAsync();

        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow
        };

        var result = await _controller.StartMaintenance(id: 5, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request with ExpectedCompletionTo (range-based completion date)
    /// Expected return: Task uses ExpectedCompletionTo when ExpectedCompletionDate is null
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithExpectedCompletionTo_UsesToDate()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var today = DateTime.UtcNow;
        var completionTo = today.AddDays(7);

        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today,
            ExpectedCompletionDate = null, // Not set
            ExpectedCompletionTo = completionTo, // Set instead
            LocationType = "at-unit",
            Location = "IT Department"
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var task = await _context.MaintenanceTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(completionTo.Date, task.ExpectedCompletionDate?.Date);
    }

    /// <summary>
    /// Input:  Valid request with AttachmentUrls
    /// Expected return: ProposedData contains attachmentUrls
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithAttachmentUrls_StoresInProposedData()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            AttachmentUrls = new List<string> { "http://example.com/doc1.pdf", "http://example.com/doc2.pdf" }
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var request = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(request!.ProposedData);
        Assert.Contains("attachmentUrls", request.ProposedData);
    }

    /// <summary>
    /// Input:  Valid request, creates AssetRequestRecord
    /// Expected return: Record with Action = 2 (Start)
    /// </summary>
    [Fact]
    public async Task ValidRequest_CreatesAssetRequestRecord()
    {
        await SeedApprovedMaintenanceRequestAsync();
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            Comment = "Starting maintenance work"
        };

        await _controller.StartMaintenance(id: 1, dto: dto);

        var record = await _context.AssetRequestRecords.FirstOrDefaultAsync(r => r.AssetRequestId == 1);
        Assert.NotNull(record);
        Assert.Equal(2, record.Action); // Start action
        Assert.Equal(1, record.FromStatus); // Approved
        Assert.Equal(4, record.ToStatus); // In Progress
    }
}
