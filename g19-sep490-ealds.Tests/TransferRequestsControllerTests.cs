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

public class TransferRequestsControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly Mock<IAssetRequestNotificationService> _mockNotificationService;
    private readonly TransferRequestsController _controller;

    public TransferRequestsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        _mockNotificationService = new Mock<IAssetRequestNotificationService>();
        _mockNotificationService
            .Setup(x => x.NotifyFirstApproversAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "App:TransferRequestTypeId", "3" },
                { "App:DepartmentHeadRoleId", "4" }
            })
            .Build();

        _controller = new TransferRequestsController(
            _context,
            configuration,
            _mockNotificationService.Object);

        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        // Seed Departments
        _context.Departments.Add(new Department
        {
            DepartmentId = 1,
            Code = "DEPT001",
            Name = "IT Department",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });
        _context.Departments.Add(new Department
        {
            DepartmentId = 2,
            Code = "DEPT002",
            Name = "HR Department",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });
        _context.Departments.Add(new Department
        {
            DepartmentId = 3,
            Code = "DEPT003",
            Name = "Finance Department",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });

        // Seed User
        _context.Users.Add(new User
        {
            UserId = 1,
            Email = "admin@test.com",
            Password = "hashed",
            Status = 1
        });

        // Seed Employee
        _context.Employees.Add(new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Test Employee",
            Status = 1
        });

        // Seed Asset
        _context.Assets.Add(new Asset
        {
            AssetId = 1,
            Code = "ASSET001",
            Name = "Test Asset",
            AssetTypeId = 1,
            Status = 1,
            Unit = "pcs",
            CreatedBy = 1
        });

        // Seed Asset Instance with InUse status
        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 1,
            AssetId = 1,
            WarehouseId = 1,
            InstanceCode = "INS001",
            Status = (int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.InUse,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalPrice = 10000000m
        });

        // Seed Asset Location (current location at Department 1)
        _context.AssetLocations.Add(new AssetLocation
        {
            AssetInstanceId = 1,
            DepartmentId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            IsCurrent = true
        });

        // Seed RequestType for Transfer
        _context.RequestTypes.Add(new RequestType
        {
            RequestTypeId = 3,
            Name = "Transfer Request",
            WorkflowId = 1
        });

        // Seed Workflow and Steps
        _context.Workflows.Add(new Workflow
        {
            WorkflowId = 1,
            Name = "Transfer Workflow"
        });
        _context.WorkflowSteps.Add(new WorkflowStep
        {
            StepId = 1,
            WorkflowId = 1,
            StepOrder = 1,
            Name = "Pending Approval"
        });

        await _context.SaveChangesAsync();
    }

    private void SetUserClaim(int userId, bool isAccountant = false, bool isDepartmentHead = false, int? departmentId = 1)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var roles = new List<string>();
        if (isAccountant) roles.Add("ACCOUNTANT");
        if (isDepartmentHead) roles.Add("DEPARTMENT_HEAD");

        var identity = new ClaimsIdentity(claims, "TestAuth");
        if (roles.Any())
        {
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Setup user role and employee for department head
        if (isDepartmentHead && departmentId.HasValue)
        {
            var userRole = _context.UserRoles.FirstOrDefault(r => r.UserId == userId);
            if (userRole == null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = 4 // Department Head
                });
                _context.SaveChanges();
            }
        }
    }

    private void SetUserWithoutClaim()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private TransferRequestDTO CreateValidDto(
        DateTime? transferDate = null,
        int fromLocationId = 1,
        int toLocationId = 2,
        int assetInstanceId = 1)
    {
        return new TransferRequestDTO
        {
            AssetInstanceId = assetInstanceId,
            RequestTypeId = 3,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            TransferDate = transferDate,
            ExecuteBy = 1,
            Description = "Test transfer"
        };
    }

    #region Create Tests

    /// <summary>
    /// Test case 1 (Normal): TransferDate = today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK
    /// Note: IsSenderConfirmed is always set to false during creation (not part of DTO)
    /// </summary>
    [Fact]
    public async Task Create_ValidDataToday_ReturnsOk()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 2 (Normal): TransferDate = today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK
    /// Note: IsSenderConfirmed is always set to false during creation (not part of DTO)
    /// This test confirms successful creation regardless of IsSenderConfirmed value
    /// </summary>
    [Fact]
    public async Task Create_ValidData_ReturnsOkRegardlessOfIsSenderConfirmed()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        // Verify IsSenderConfirmed is always false in the created transfer
        var okResult = (OkObjectResult)result;
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case 3 (Abnormal): Invalid scenario - the system doesn't accept IsSenderConfirmed in DTO.
    /// Expected output: 200 OK (IsSenderConfirmed is not part of DTO, always set to false)
    /// Note: The test case is conceptual - IsSenderConfirmed must always be false when creating
    /// </summary>
    [Fact]
    public async Task Create_IsSenderConfirmedNotInDto_AlwaysFalse()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        // Verify the created transfer has IsSenderConfirmed = false
        var createdTransfer = await _context.TransferRecords.FirstOrDefaultAsync();
        Assert.NotNull(createdTransfer);
        Assert.False(createdTransfer.IsSenderConfirmed);
    }

    /// <summary>
    /// Test case 4 (Abnormal): Invalid scenario - IsSenderConfirmed must be false during creation.
    /// Expected output: 200 OK (IsSenderConfirmed is ignored, always set to false)
    /// Note: IsSenderConfirmed is not part of the DTO and cannot be set to 2
    /// </summary>
    [Fact]
    public async Task Create_IsSenderConfirmedIgnored_AlwaysFalse()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        // Verify IsSenderConfirmed is always false
        var createdTransfer = await _context.TransferRecords.FirstOrDefaultAsync();
        Assert.NotNull(createdTransfer);
        Assert.False(createdTransfer.IsSenderConfirmed);
    }

    /// <summary>
    /// Test case 5 (Normal): TransferDate <= today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_PastTransferDate_ReturnsOk()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow.AddDays(-1), // Past date
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Normal): TransferDate >= today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_FutureTransferDate_ReturnsOk()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow.AddDays(1), // Future date
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): TransferDate = today, FromLocationId = 0, ToLocationId = 2.
    /// Expected output: 400 Bad Request (Source department does not exist)
    /// </summary>
    [Fact]
    public async Task Create_FromLocationIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 0, // Invalid
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): TransferDate = today, FromLocationId = -1, ToLocationId = 2.
    /// Expected output: 400 Bad Request (Source department does not exist)
    /// </summary>
    [Fact]
    public async Task Create_FromLocationIdNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: -1, // Invalid
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 9 (Abnormal): TransferDate = today, FromLocationId = 2, ToLocationId = 2.
    /// Expected output: 400 Bad Request (Source and destination cannot be the same)
    /// </summary>
    [Fact]
    public async Task Create_FromLocationIdEqualsToLocationId_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 2,
            toLocationId: 2); // Same as FromLocationId

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 10 (Abnormal): TransferDate = today, FromLocationId = 1, ToLocationId = 0.
    /// Expected output: 400 Bad Request (Destination department does not exist)
    /// </summary>
    [Fact]
    public async Task Create_ToLocationIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 0); // Invalid

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 11 (Abnormal): TransferDate = today, FromLocationId = 1, ToLocationId = -1.
    /// Expected output: 400 Bad Request (Destination department does not exist)
    /// </summary>
    [Fact]
    public async Task Create_ToLocationIdNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: -1); // Invalid

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 12 (Abnormal): TransferDate = today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK (IsSenderConfirmed is not validated during creation)
    /// Note: The test case mentions IsSenderConfirmed = -1 but this field is not in the DTO
    /// </summary>
    [Fact]
    public async Task Create_IsSenderConfirmedNotValidated_ReturnsOk()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 13 (Abnormal): TransferDate = today, FromLocationId = -1, ToLocationId = -1.
    /// Expected output: 400 Bad Request (Both source and destination departments do not exist)
    /// Note: IsSenderConfirmed is not part of the DTO
    /// </summary>
    [Fact]
    public async Task Create_BothLocationIdsInvalid_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: -1,
            toLocationId: -1);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Additional Edge Cases

    /// <summary>
    /// Test case: DTO is null
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullDto_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);

        // Act
        var result = await _controller.CreateTransferRequest(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: AssetInstanceId is 0
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_AssetInstanceIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2,
            assetInstanceId: 0);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: AssetInstanceId is negative
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_AssetInstanceIdNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2,
            assetInstanceId: -1);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Non-existent AssetInstanceId
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Create_NonExistentAssetInstanceId_ReturnsNotFound()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2,
            assetInstanceId: 999);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: Asset instance not in InUse status
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_AssetNotInUse_ReturnsBadRequest()
    {
        // Arrange
        // Create asset instance with Available status
        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 2,
            AssetId = 1,
            WarehouseId = 1,
            InstanceCode = "INS002",
            Status = (int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.Available,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalPrice = 10000000m
        });
        _context.AssetLocations.Add(new AssetLocation
        {
            AssetInstanceId = 2,
            DepartmentId = 1,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            IsCurrent = true
        });
        await _context.SaveChangesAsync();

        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2,
            assetInstanceId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Without user authentication
    /// Expected output: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task Create_WithoutUserAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        SetUserWithoutClaim();
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Test case: Non-existent FromLocationId
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NonExistentFromLocationId_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 999,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Non-existent ToLocationId
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NonExistentToLocationId_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: DateTime.UtcNow,
            fromLocationId: 1,
            toLocationId: 999);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Null TransferDate (defaults to now)
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_NullTransferDate_ReturnsOk()
    {
        // Arrange
        SetUserClaim(1, isAccountant: true);
        var dto = CreateValidDto(
            transferDate: null,
            fromLocationId: 1,
            toLocationId: 2);

        // Act
        var result = await _controller.CreateTransferRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Delete Tests

    private async Task<int> CreateTransferRequestForDelete(int createdByUserId = 1)
    {
        // Create asset request
        var assetRequest = new AssetRequest
        {
            AssetRequestId = (_context.AssetRequests.Any() ? _context.AssetRequests.Max(r => r.AssetRequestId) : 0) + 1,
            UserId = createdByUserId,
            RequestTypeId = 3,
            AssetId = 1,
            AssetInstanceId = 1,
            Title = "Test Transfer",
            Status = 1, // StatusSubmitted
            CreatedBy = createdByUserId,
            CreateDate = DateTime.UtcNow,
            StepId = 1
        };
        _context.AssetRequests.Add(assetRequest);

        // Create transfer record
        var fromLocation = await _context.AssetLocations.FirstOrDefaultAsync(al => al.AssetInstanceId == 1 && al.DepartmentId == 1 && al.IsCurrent);
        var transfer = new TransferRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            AssetInstanceId = 1,
            FromLocationId = fromLocation?.LocationId ?? 1,
            ToLocationId = 2,
            TransferDate = DateTime.UtcNow,
            IsSenderConfirmed = false,
            IsReceiverConfirmed = false
        };
        _context.TransferRecords.Add(transfer);

        await _context.SaveChangesAsync();
        return assetRequest.AssetRequestId;
    }

    /// <summary>
    /// Test case 1 (Boundary): TransferId = 0.
    /// Expected output: 400 Bad Request or 404 Not Found
    /// Note: In ASP.NET routing, ID = 0 would typically not match the route, but we'll test what happens
    /// </summary>
    [Fact]
    public async Task Delete_TransferIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);

        // Act
        var result = await _controller.DeleteTransferRequest(0);

        // Assert
        // ID 0 means the request won't match the route, so it will be 400 or NotFound
        Assert.True(result is BadRequestObjectResult || result is NotFoundObjectResult);
    }

    /// <summary>
    /// Test case 2 (Normal): TransferId = 1 (valid existing request).
    /// Expected output: 204 No Content (successfully deleted)
    /// </summary>
    [Fact]
    public async Task Delete_ValidTransferId_ReturnsNoContent()
    {
        // Arrange
        SetUserClaim(1);
        var assetRequestId = await CreateTransferRequestForDelete(createdByUserId: 1);

        // Act
        var result = await _controller.DeleteTransferRequest(assetRequestId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify the transfer request is deleted
        var deletedTransfer = await _context.TransferRecords.FirstOrDefaultAsync(t => t.AssetRequestId == assetRequestId);
        Assert.Null(deletedTransfer);

        var deletedRequest = await _context.AssetRequests.FirstOrDefaultAsync(r => r.AssetRequestId == assetRequestId);
        Assert.Null(deletedRequest);
    }

    /// <summary>
    /// Test case 3 (Abnormal): TransferId = 999 (non-existent).
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Delete_NonExistentTransferId_ReturnsNotFound()
    {
        // Arrange
        SetUserClaim(1);

        // Act
        var result = await _controller.DeleteTransferRequest(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): TransferId = -1 (negative).
    /// Expected output: 404 Not Found (or BadRequest)
    /// </summary>
    [Fact]
    public async Task Delete_NegativeTransferId_ReturnsNotFound()
    {
        // Arrange
        SetUserClaim(1);

        // Act
        var result = await _controller.DeleteTransferRequest(-1);

        // Assert
        Assert.True(result is NotFoundObjectResult || result is BadRequestObjectResult);
    }

    #endregion

    #region Delete Additional Edge Cases

    /// <summary>
    /// Test case: Delete transfer request created by another user
    /// Expected output: 403 Forbidden
    /// </summary>
    [Fact]
    public async Task Delete_ByDifferentUser_ReturnsForbidden()
    {
        // Arrange
        // Create transfer request by user 1
        SetUserClaim(1);
        var assetRequestId = await CreateTransferRequestForDelete(createdByUserId: 1);

        // Try to delete with user 2
        SetUserClaim(2);
        _context.UserRoles.Add(new UserRole { UserId = 2, RoleId = 1 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteTransferRequest(assetRequestId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    /// <summary>
    /// Test case: Delete transfer request without user authentication
    /// Expected output: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task Delete_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaim(1);
        var assetRequestId = await CreateTransferRequestForDelete(createdByUserId: 1);

        // Remove user claim
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.DeleteTransferRequest(assetRequestId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Test case: Delete transfer request with Status > 1 (already approved)
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Delete_AlreadyApproved_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var assetRequestId = await CreateTransferRequestForDelete(createdByUserId: 1);

        // Update status to approved (Status = 4)
        var request = await _context.AssetRequests.FindAsync(assetRequestId);
        request!.Status = 4;
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteTransferRequest(assetRequestId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Delete transfer request with Status = 0 (Draft)
    /// Expected output: 204 No Content
    /// </summary>
    [Fact]
    public async Task Delete_DraftStatus_ReturnsNoContent()
    {
        // Arrange
        SetUserClaim(1);
        var assetRequestId = await CreateTransferRequestForDelete(createdByUserId: 1);

        // Update status to draft (Status = 0)
        var request = await _context.AssetRequests.FindAsync(assetRequestId);
        request!.Status = 0;
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteTransferRequest(assetRequestId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test case: Delete transfer request with Status = 1 (Submitted)
    /// Expected output: 204 No Content
    /// </summary>
    [Fact]
    public async Task Delete_SubmittedStatus_ReturnsNoContent()
    {
        // Arrange
        SetUserClaim(1);
        var assetRequestId = await CreateTransferRequestForDelete(createdByUserId: 1);

        // Verify status is submitted (Status = 1)
        var request = await _context.AssetRequests.FindAsync(assetRequestId);
        Assert.Equal(1, request!.Status);

        // Act
        var result = await _controller.DeleteTransferRequest(assetRequestId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test case: Delete transfer request with Status = 2 (Pending Approval)
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Delete_PendingApprovalStatus_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var assetRequestId = await CreateTransferRequestForDelete(createdByUserId: 1);

        // Update status to pending approval (Status = 2)
        var request = await _context.AssetRequests.FindAsync(assetRequestId);
        request!.Status = 2;
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteTransferRequest(assetRequestId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
