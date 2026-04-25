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
/// Unit tests for RepairRequestsController.StartRepair
/// (POST /api/Assets/Requests/repair/{id}/start)
/// </summary>
public class RepairRequestsControllerStartRepairTests
{
    private readonly EaldsDbContext _context;
    private readonly RepairRequestsController _controller;
    private readonly Mock<IAssetRequestNotificationService> _mockNotification;

    public RepairRequestsControllerStartRepairTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "App:RepairRequestTypeId", "4" }
            })
            .Build();

        _mockNotification = new Mock<IAssetRequestNotificationService>();
        _controller = new RepairRequestsController(_context, configuration, _mockNotification.Object);
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
            RequestTypeId = 4,
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
            Status = (int)AssetStatus.Damaged,
            Note = "Screen broken"
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

    private async Task SeedApprovedRepairRequestAsync()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 1,
            UserId = 1,
            RequestTypeId = 4,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Repair Dell Laptop",
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
            ApprovedUserId = 1,
            DecisionDate = DateTime.UtcNow.AddDays(-1)
        });

        await _context.SaveChangesAsync();
    }

    private RepairStartDto MinimalDto() => new RepairStartDto
    {
        StartedBy = 1,
        Comment = "Starting repair",
        DamageCondition = "Screen broken",
        EstimatedCost = 500000m,
        RepairProgressStatus = "InProgress",
        SupplierId = null
    };

    /// <summary>
    /// Test case 1 (Normal):
    /// RepairDate = equal to Date damage, ExpectedCompletionDate = equal to RepairDate, RepairProgressStatus = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartRepair_NormalCase_RepairDateEqualsDamageDate_ReturnsOk()
    {
        // Arrange
        await SeedApprovedRepairRequestAsync();
        var damageDate = DateTime.UtcNow;
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = damageDate, // Equal to DamageDate
            ExpectedCompletionDate = damageDate, // Equal to RepairDate
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        // Act
        var result = await _controller.StartRepair(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(damageDate, task.RepairDate);
        Assert.Equal(damageDate, task.ExpectedCompletionDate);
        Assert.Equal("InProgress", task.RepairProgressStatus);
        Assert.Equal(1, task.Status); // In-progress
    }

    /// <summary>
    /// Test case 2 (Abnormal):
    /// RepairDate = less than Date damage, ExpectedCompletionDate = equal to RepairDate, RepairProgressStatus = Valid.
    /// Expected output: 200 OK (no validation on date relationships)
    /// </summary>
    [Fact]
    public async Task StartRepair_RepairDateBeforeDamageDate_ReturnsOk()
    {
        // Arrange
        await SeedApprovedRepairRequestAsync();
        var damageDate = DateTime.UtcNow;
        var repairDate = damageDate.AddDays(-1); // Less than DamageDate
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate, // Less than DamageDate
            ExpectedCompletionDate = repairDate, // Equal to RepairDate
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        // Act
        var result = await _controller.StartRepair(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(repairDate, task.RepairDate);
        Assert.Equal(repairDate, task.ExpectedCompletionDate);
    }

    /// <summary>
    /// Test case 3 (Normal):
    /// RepairDate = greater than Date damage, ExpectedCompletionDate = equal to RepairDate, RepairProgressStatus = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartRepair_RepairDateAfterDamageDate_ReturnsOk()
    {
        // Arrange
        await SeedApprovedRepairRequestAsync();
        var damageDate = DateTime.UtcNow.AddDays(-5);
        var repairDate = DateTime.UtcNow; // Greater than DamageDate
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate, // Greater than DamageDate
            ExpectedCompletionDate = repairDate, // Equal to RepairDate
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        // Act
        var result = await _controller.StartRepair(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(repairDate, task.RepairDate);
        Assert.Equal(repairDate, task.ExpectedCompletionDate);
    }

    /// <summary>
    /// Test case 4 (Abnormal):
    /// RepairDate = greater than Date damage, ExpectedCompletionDate = less than RepairDate, RepairProgressStatus = Valid.
    /// Expected output: 200 OK (no validation on ExpectedCompletionDate vs RepairDate)
    /// </summary>
    [Fact]
    public async Task StartRepair_ExpectedCompletionBeforeRepairDate_ReturnsOk()
    {
        // Arrange
        await SeedApprovedRepairRequestAsync();
        var damageDate = DateTime.UtcNow.AddDays(-10);
        var repairDate = DateTime.UtcNow;
        var expectedCompletion = repairDate.AddDays(-1); // Less than RepairDate
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate, // Greater than DamageDate
            ExpectedCompletionDate = expectedCompletion, // Less than RepairDate
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        // Act
        var result = await _controller.StartRepair(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(repairDate, task.RepairDate);
        Assert.Equal(expectedCompletion, task.ExpectedCompletionDate);
    }

    /// <summary>
    /// Test case 5 (Normal):
    /// RepairDate = greater than Date damage, ExpectedCompletionDate = greater than RepairDate, RepairProgressStatus = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartRepair_ExpectedCompletionAfterRepairDate_ReturnsOk()
    {
        // Arrange
        await SeedApprovedRepairRequestAsync();
        var damageDate = DateTime.UtcNow.AddDays(-5);
        var repairDate = DateTime.UtcNow;
        var expectedCompletion = repairDate.AddDays(7); // Greater than RepairDate
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate, // Greater than DamageDate
            ExpectedCompletionDate = expectedCompletion, // Greater than RepairDate
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        // Act
        var result = await _controller.StartRepair(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(repairDate, task.RepairDate);
        Assert.Equal(expectedCompletion, task.ExpectedCompletionDate);
        Assert.Equal("InProgress", task.RepairProgressStatus);
    }

    /// <summary>
    /// Test case 6 (Abnormal):
    /// RepairDate = greater than Date damage, ExpectedCompletionDate = equal to RepairDate, RepairProgressStatus = Empty.
    /// Expected output: 200 OK (no validation on RepairProgressStatus being empty)
    /// </summary>
    [Fact]
    public async Task StartRepair_EmptyRepairProgressStatus_ReturnsOk()
    {
        // Arrange
        await SeedApprovedRepairRequestAsync();
        var damageDate = DateTime.UtcNow.AddDays(-5);
        var repairDate = DateTime.UtcNow;
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate, // Greater than DamageDate
            ExpectedCompletionDate = repairDate, // Equal to RepairDate
            RepairProgressStatus = "", // Empty
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        // Act
        var result = await _controller.StartRepair(id: 1, dto: dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(repairDate, task.RepairDate);
        Assert.Equal(repairDate, task.ExpectedCompletionDate);
        Assert.Equal("", task.RepairProgressStatus);
    }

    // ─── Additional validation tests ─────────────────────────────────────────

    /// <summary>
    /// Input:  dto = null
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        await SeedApprovedRepairRequestAsync();
        var result = await _controller.StartRepair(id: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  StartedBy = 0
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task StartedByZero_ReturnsBadRequest()
    {
        await SeedApprovedRepairRequestAsync();
        var dto = new RepairStartDto { StartedBy = 0 };
        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  StartedBy = -1
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task StartedByNegative_ReturnsBadRequest()
    {
        await SeedApprovedRepairRequestAsync();
        var dto = new RepairStartDto { StartedBy = -1 };
        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  AssetRequest with id = 999 (does not exist)
    /// Expected return: NotFoundObjectResult
    /// </summary>
    [Fact]
    public async Task RequestNotFound_ReturnsNotFound()
    {
        await SeedApprovedRepairRequestAsync();
        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 999, dto: dto);
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
            RequestTypeId = 4,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Repair Dell Laptop",
            Status = 1, // Pending Approval (not final approved)
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        await _context.SaveChangesAsync();

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 2, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  User with invalid role (no permission to start repair)
    /// Expected return: Forbid()
    /// </summary>
    [Fact]
    public async Task UserWithInvalidRole_ReturnsForbid()
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
            UserId = 1,
            RequestTypeId = 4,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Repair Dell Laptop",
            Status = 2,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.Approvals.Add(new Approval
        {
            AssetRequestId = 3,
            StepId = 10,
            ApprovedRoleId = 3,
            Decision = 1,
            ApprovedUserId = 1,
            DecisionDate = DateTime.UtcNow.AddDays(-1)
        });

        await _context.SaveChangesAsync();

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 3, dto: dto);
        Assert.IsType<ForbidResult>(result);
    }

    /// <summary>
    /// Input:  Valid request with valid role DIRECTOR
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithDirectorRole_ReturnsOk()
    {
        await SeedApprovedRepairRequestAsync();
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request with role DEPARTMENT_MANAGER
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithDepartmentManagerRole_ReturnsOk()
    {
        await SeedMinimalAsync();

        _context.Roles.Add(new Role
        {
            RoleId = 4,
            Code = "DEPARTMENT_MANAGER",
            Name = "Department Manager"
        });

        _context.UserRoles.Add(new UserRole
        {
            UserId = 1,
            RoleId = 4
        });

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 4,
            UserId = 1,
            RequestTypeId = 4,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Repair Dell Laptop",
            Status = 2,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.Approvals.Add(new Approval
        {
            AssetRequestId = 4,
            StepId = 10,
            ApprovedRoleId = 4,
            Decision = 1,
            ApprovedUserId = 1,
            DecisionDate = DateTime.UtcNow.AddDays(-1)
        });

        await _context.SaveChangesAsync();

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 4, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request with role ACCOUNTANT
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithAccountantRole_ReturnsOk()
    {
        await SeedMinimalAsync();

        _context.Roles.Add(new Role
        {
            RoleId = 5,
            Code = "ACCOUNTANT",
            Name = "Accountant"
        });

        _context.UserRoles.Add(new UserRole
        {
            UserId = 1,
            RoleId = 5
        });

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 5,
            UserId = 1,
            RequestTypeId = 4,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Repair Dell Laptop",
            Status = 2,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.Approvals.Add(new Approval
        {
            AssetRequestId = 5,
            StepId = 10,
            ApprovedRoleId = 5,
            Decision = 1,
            ApprovedUserId = 1,
            DecisionDate = DateTime.UtcNow.AddDays(-1)
        });

        await _context.SaveChangesAsync();

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 5, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request, updates asset status to InRepair
    /// Expected return: AssetInstance status changes from Damaged to InRepair
    /// </summary>
    [Fact]
    public async Task ValidRequest_UpdatesAssetStatusToInRepair()
    {
        await SeedApprovedRepairRequestAsync();
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        await _controller.StartRepair(id: 1, dto: dto);

        var instance = await _context.AssetInstances.FindAsync(1);
        Assert.Equal((int)AssetStatus.InRepair, instance!.Status);
    }

    /// <summary>
    /// Input:  Valid request, sets task status to InProgress (1)
    /// Expected return: RepairTask status = 1 (InProgress)
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsTaskStatusToInProgress()
    {
        await SeedApprovedRepairRequestAsync();
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        await _controller.StartRepair(id: 1, dto: dto);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == 1);
        Assert.NotNull(task);
        Assert.Equal(1, task.Status); // InProgress
    }

    /// <summary>
    /// Input:  Valid request, sets AssetRequest status to 4 (In Repair)
    /// Expected return: AssetRequest status = 4
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsAssetRequestStatusToInRepair()
    {
        await SeedApprovedRepairRequestAsync();
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        await _controller.StartRepair(id: 1, dto: dto);

        var request = await _context.AssetRequests.FindAsync(1);
        Assert.Equal(4, request!.Status); // In Repair
    }

    /// <summary>
    /// Input:  Valid request with supplier ID
    /// Expected return: OkObjectResult with supplier linked to task
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithSupplierId_ReturnsOk()
    {
        await SeedApprovedRepairRequestAsync();

        _context.Suppliers.Add(new Supplier
        {
            SupplierId = 1,
            Code = "SUP-001",
            Name = "Repair Shop A",
            Status = 1,
            CreateDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress",
            SupplierId = 1
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Request with Status = 4 (already in repair)
    /// Expected return: OkObjectResult (idempotent - restarts repair)
    /// </summary>
    [Fact]
    public async Task AlreadyInRepair_ReturnsOk()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 6,
            UserId = 1,
            RequestTypeId = 4,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Repair Dell Laptop",
            Status = 4, // Already in repair
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        await _context.SaveChangesAsync();

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 6, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  AssetRequest with no AssetId linked
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task RequestWithoutAssetId_ReturnsBadRequest()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 7,
            UserId = 1,
            RequestTypeId = 4,
            AssetId = null, // No asset linked
            Title = "Repair Request",
            Status = 2,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow,
            StepId = 10
        });

        _context.Approvals.Add(new Approval
        {
            AssetRequestId = 7,
            StepId = 10,
            ApprovedRoleId = 3,
            Decision = 1,
            ApprovedUserId = 1,
            DecisionDate = DateTime.UtcNow.AddDays(-1)
        });

        await _context.SaveChangesAsync();

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 7, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
