using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Allocation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services;
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

public class HandoverRequestsControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly Mock<IAssetRequestNotificationService> _mockNotificationService;
    private readonly HandoverRequestsController _controller;

    public HandoverRequestsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "App:HandoverRequestTypeId", "7" },
                { "App:DepartmentHeadRoleId", "4" }
            })
            .Build();

        _mockNotificationService = new Mock<IAssetRequestNotificationService>();
        _mockNotificationService
            .Setup(x => x.NotifyFirstApproversAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _controller = new HandoverRequestsController(
            _context,
            configuration,
            _mockNotificationService.Object);

        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        // Seed User
        _context.Users.Add(new User
        {
            UserId = 1,
            Email = "head@test.com",
            Password = "hashed",
            Status = 1
        });

        // Seed Department
        _context.Departments.Add(new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            Code = "IT",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });

        // Seed Employee with User and Department
        _context.Employees.Add(new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Department Head",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });

        // Seed Role for Department Head
        _context.Roles.Add(new Role
        {
            RoleId = 4,
            Code = "DEPARTMENT_HEAD",
            Name = "Department Head"
        });

        // Seed UserRole for department head
        _context.UserRoles.Add(new UserRole
        {
            UserId = 1,
            RoleId = 4
        });

        // Seed AssetType
        _context.AssetTypes.Add(new AssetType
        {
            AssetTypeId = 1,
            Name = "Computer",
            Code = "COMP"
        });

        // Seed Asset
        _context.Assets.Add(new Asset
        {
            AssetId = 1,
            AssetTypeId = 1,
            Code = "PC001",
            Name = "Desktop PC",
            Status = 1,
            Unit = "pcs",
            CreatedBy = 1
        });

        // Seed Warehouse
        _context.Warehouses.Add(new Warehouse
        {
            WarehouseId = 1,
            Name = "Main Warehouse",
            Code = "WH001",
            Status = 1
        });

        // Seed AssetInstance (assigned to department)
        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 1,
            AssetId = 1,
            WarehouseId = null,
            DepartmentId = 1,
            InstanceCode = "INS001",
            Status = (int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.InUse,
            InUseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalPrice = 10000000m
        });

        // Seed RequestType
        _context.RequestTypes.Add(new RequestType
        {
            RequestTypeId = 7,
            WorkflowId = 1,
            Name = "Handover Request"
        });

        // Seed Workflow
        _context.Workflows.Add(new Workflow
        {
            WorkflowId = 1,
            Name = "Handover Workflow",
            Status = 1
        });

        // Seed WorkflowStep
        _context.WorkflowSteps.Add(new WorkflowStep
        {
            StepId = 1,
            WorkflowId = 1,
            StepOrder = 1,
            RoleId = 5, // Accountant role
            IsFinalStep = false
        });

        await _context.SaveChangesAsync();
    }

    private void SetUserClaim(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetUserWithoutClaim()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private CreateDepartmentAllocationRequestDto CreateValidDto()
    {
        return new CreateDepartmentAllocationRequestDto
        {
            Title = "Valid Title",
            Lines = new List<AllocationLineInputDto>
            {
                new AllocationLineInputDto
                {
                    AssetTypeId = 1,
                    AssetId = 1,
                    Quantity = 1
                }
            }
        };
    }

    #region Create Tests

    /// <summary>
    /// Test case 1 (Normal): Title = Valid title, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_ValidData_ReturnsOk()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.Equal(200, okResult.StatusCode);
    }

    /// <summary>
    /// Test case 2 (Abnormal): Title = Empty, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Nhập tiêu đề yêu cầu)
    /// </summary>
    [Fact]
    public async Task Create_EmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = "";

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequest = (BadRequestObjectResult)result;
        Assert.Equal(400, badRequest.StatusCode);
    }

    /// <summary>
    /// Test case 2b (Abnormal): Title = null, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullTitle_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = null!;

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 2c (Abnormal): Title = whitespace, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WhitespaceTitle_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = "   ";

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): Title = Valid title, AssetType = 0, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetTypeIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetTypeId = 0;

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): Title = Valid title, AssetType = -1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetTypeIdNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetTypeId = -1;

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal): Title = Valid title, AssetType = 1, Asset = 0, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetId = 0;

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Abnormal): Title = Valid title, AssetType = 1, Asset = -1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetIdNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetId = -1;

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): Title = Valid title, AssetType = 1, Asset = 1, Quantity = 0.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_QuantityZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].Quantity = 0;

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): Title = Valid title, AssetType = 1, Asset = 1, Quantity = -1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_QuantityNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].Quantity = -1;

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Additional Edge Cases

    /// <summary>
    /// Test case: Lines is empty
    /// Expected output: 400 Bad Request (Cần ít nhất một dòng tài sản)
    /// </summary>
    [Fact]
    public async Task Create_EmptyLines_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = new CreateDepartmentAllocationRequestDto
        {
            Title = "Valid Title",
            Lines = new List<AllocationLineInputDto>()
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Lines is null
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullLines_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = new CreateDepartmentAllocationRequestDto
        {
            Title = "Valid Title",
            Lines = null!
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: DTO is null
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullDto_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);

        // Act
        var result = await _controller.Create(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Without user claim (unauthorized)
    /// Expected output: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task Create_WithoutUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        SetUserWithoutClaim();
        var dto = CreateValidDto();

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Test case: User without department head role
    /// Expected output: 403 Forbidden
    /// </summary>
    [Fact]
    public async Task Create_UserNotDepartmentHead_ReturnsForbidden()
    {
        // Arrange
        // Add a user without department head role
        _context.Users.Add(new User
        {
            UserId = 2,
            Email = "regular@test.com",
            Password = "hashed",
            Status = 1
        });
        _context.Employees.Add(new Employee
        {
            EmployeeId = 2,
            UserId = 2,
            DepartmentId = 1,
            Name = "Regular Employee",
            Code = "EMP002",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });
        await _context.SaveChangesAsync();

        SetUserClaim(2);
        var dto = CreateValidDto();

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<ObjectResult>(result);
        var objectResult = (ObjectResult)result;
        Assert.Equal(403, objectResult.StatusCode);
    }

    /// <summary>
    /// Test case: User without employee/department association
    /// Expected output: 400 Bad Request (Tài khoản chưa gắn phòng ban)
    /// </summary>
    [Fact]
    public async Task Create_UserWithoutDepartment_ReturnsBadRequest()
    {
        // Arrange
        // User 1 exists but remove their employee record
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == 1);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }

        SetUserClaim(1);
        var dto = CreateValidDto();

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
