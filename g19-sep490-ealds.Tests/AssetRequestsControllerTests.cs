using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests for purchase request (AssetRequest) creation and management
/// via AssetRequestsController, simulating DEPARTMENT_HEAD / ADMIN role.
/// </summary>
public class AssetRequestsControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly Mock<IAssetRequestNotificationService> _mockNotificationService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AssetRequestsController _controller;

    public AssetRequestsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        _mockNotificationService = new Mock<IAssetRequestNotificationService>();
        _mockNotificationService
            .Setup(x => x.NotifyFirstApproversAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockNotificationService
            .Setup(x => x.NotifySenderDecisionAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(x => x["App:PurchaseRequestTypeId"]).Returns("1");

        _controller = new AssetRequestsController(
            _context,
            _mockConfiguration.Object,
            _mockNotificationService.Object);
    }


    #region GetById Tests

    /// <summary>
    /// Test case: GetById with valid id returns the purchase request with approvals
    /// Expected output: 200 OK with request details
    /// </summary>
    [Fact]
    public async Task GetById_WithValidId_ReturnsPurchaseRequest()
    {
        // Arrange
        var request = await SeedPurchaseRequest(userId: 1, status: 0, title: "Laptop Request");

        // Act
        var result = await _controller.GetById(request.AssetRequestId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: GetById with non-existent id returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Test case: Create purchase request as draft (status=-1) succeeds without notification
    /// Expected output: 200 OK with new assetRequestId, no notification sent
    /// </summary>
    [Fact]
    public async Task Create_AsDraft_ReturnsOkWithoutNotification()
    {
        // Arrange
        await SeedRequestType(requestTypeId: 1, workflowId: 1);
        await SeedWorkflowStep(workflowId: 1, stepOrder: 1, roleId: 3, isFinalStep: false);
        await SeedUser(1, status: 1);

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "New Laptop Request",
            Description = "Need a laptop for development",
            CreatedBy = 1,
            Status = -1
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var saved = await _context.AssetRequests.FirstOrDefaultAsync(r => r.Title == "New Laptop Request");
        Assert.NotNull(saved);
        Assert.Equal(-1, saved.Status);
        _mockNotificationService.Verify(
            x => x.NotifyFirstApproversAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Test case: Create purchase request as submitted (status=0) sends notification
    /// Expected output: 200 OK with assetRequestId, notification sent
    /// </summary>
    [Fact]
    public async Task Create_Submitted_SendsNotification()
    {
        // Arrange
        await SeedRequestType(requestTypeId: 1, workflowId: 1);
        await SeedWorkflowStep(workflowId: 1, stepOrder: 1, roleId: 3, isFinalStep: true);
        await SeedUser(1, status: 1);

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Office Desk Request",
            Description = "Need a standing desk",
            CreatedBy = 1,
            Status = 0
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockNotificationService.Verify(
            x => x.NotifyFirstApproversAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test case: Create with missing title returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "",
            CreatedBy = 1
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Create with invalid status returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        await SeedRequestType(requestTypeId: 1, workflowId: 1);
        await SeedUser(1, status: 1);

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Request",
            CreatedBy = 1,
            Status = 5
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Test case: Update draft request with new data succeeds
    /// Expected output: 200 OK with updated request
    /// </summary>
    [Fact]
    public async Task Update_DraftRequest_ReturnsUpdatedRequest()
    {
        // Arrange
        var request = await SeedPurchaseRequest(userId: 1, status: -1, title: "Original Title");

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Updated Title",
            Description = "Updated description",
            CreatedBy = 1,
            Status = -1
        };

        // Act
        var result = await _controller.Update(request.AssetRequestId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = await _context.AssetRequests.FindAsync(request.AssetRequestId);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
    }

    /// <summary>
    /// Test case: Update non-draft request returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_SubmittedRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = await SeedPurchaseRequest(userId: 1, status: 0, title: "Submitted Request");

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Try Update",
            CreatedBy = 1,
            Status = -1
        };

        // Act
        var result = await _controller.Update(request.AssetRequestId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Update non-existent request returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Request",
            CreatedBy = 1
        };

        // Act
        var result = await _controller.Update(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: Update draft to submitted sends notification
    /// Expected output: NotifyFirstApproversAsync called once
    /// </summary>
    [Fact]
    public async Task Update_DraftToSubmitted_SendsNotification()
    {
        // Arrange
        var request = await SeedPurchaseRequest(userId: 1, status: -1, title: "Draft Request");

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Submitted Request",
            CreatedBy = 1,
            Status = 0
        };

        // Act
        await _controller.Update(request.AssetRequestId, dto);

        // Assert
        _mockNotificationService.Verify(
            x => x.NotifyFirstApproversAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test case: Update with invalid status returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var request = await SeedPurchaseRequest(userId: 1, status: -1, title: "Draft Request");

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Request",
            CreatedBy = 1,
            Status = 2
        };

        // Act
        var result = await _controller.Update(request.AssetRequestId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region RevertToDraft Tests

    /// <summary>
    /// Test case: RevertToDraft on submitted request by its creator succeeds
    /// Expected output: 200 OK with status=-1
    /// </summary>
    [Fact]
    public async Task RevertToDraft_ByCreator_ReturnsOkWithDraftStatus()
    {
        // Arrange
        var request = await SeedPurchaseRequest(userId: 1, status: 0, title: "Submitted Request");

        var dto = new RevertToDraftDTO { UserId = 1 };

        // Act
        var result = await _controller.RevertToDraft(request.AssetRequestId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = await _context.AssetRequests.FindAsync(request.AssetRequestId);
        Assert.NotNull(updated);
        Assert.Equal(-1, updated.Status);
    }

    /// <summary>
    /// Test case: RevertToDraft by non-creator returns Forbid
    /// Expected output: 403 Forbid
    /// </summary>
    [Fact]
    public async Task RevertToDraft_ByNonCreator_ReturnsForbid()
    {
        // Arrange
        var request = await SeedPurchaseRequest(userId: 1, status: 0, title: "Submitted Request");

        var dto = new RevertToDraftDTO { UserId = 999 };

        // Act
        var result = await _controller.RevertToDraft(request.AssetRequestId, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    /// <summary>
    /// Test case: RevertToDraft on draft request returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task RevertToDraft_OnDraftRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = await SeedPurchaseRequest(userId: 1, status: -1, title: "Draft Request");

        var dto = new RevertToDraftDTO { UserId = 1 };

        // Act
        var result = await _controller.RevertToDraft(request.AssetRequestId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: RevertToDraft with invalid request id returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task RevertToDraft_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new RevertToDraftDTO { UserId = 1 };

        // Act
        var result = await _controller.RevertToDraft(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: RevertToDraft with zero user id returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task RevertToDraft_WithZeroUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.RevertToDraft(1, new RevertToDraftDTO { UserId = 0 });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 1 (Normal): userId = 1, requestId = 1
    /// Expected output: 200 OK with status=-1 (draft)
    /// </summary>
    [Fact]
    public async Task RevertToDraft_ValidUserAndRequest_ReturnsOk()
    {
        // Arrange
        await SeedPurchaseRequest(userId: 1, status: 0, title: "Test Request");

        var dto = new RevertToDraftDTO { UserId = 1 };

        // Act
        var result = await _controller.RevertToDraft(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = await _context.AssetRequests.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(-1, updated.Status);
    }

    /// <summary>
    /// Test case 2 (Abnormal): userId = 999 (don't exist), requestId = 1
    /// Expected output: 403 Forbid (user is not the creator)
    /// </summary>
    [Fact]
    public async Task RevertToDraft_NonExistentUser_ReturnsForbid()
    {
        // Arrange
        await SeedPurchaseRequest(userId: 1, status: 0, title: "Test Request");

        var dto = new RevertToDraftDTO { UserId = 999 };

        // Act
        var result = await _controller.RevertToDraft(1, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): userId = 1, requestId = 999 (don't exist)
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task RevertToDraft_NonExistentRequest_ReturnsNotFound()
    {
        // Arrange
        await SeedUser(1, status: 1);

        var dto = new RevertToDraftDTO { UserId = 1 };

        // Act
        var result = await _controller.RevertToDraft(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): userId = 999, requestId = 999 (both don't exist)
    /// Expected output: 404 Not Found (request not found takes precedence)
    /// </summary>
    [Fact]
    public async Task RevertToDraft_NonExistentUserAndRequest_ReturnsNotFound()
    {
        // Arrange
        var dto = new RevertToDraftDTO { UserId = 999 };

        // Act
        var result = await _controller.RevertToDraft(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region Helper Methods

    private async Task<AssetRequest> SeedPurchaseRequest(int userId, int status, string title, int requestTypeId = 1)
    {
        await SeedUser(userId, status: 1);
        await SeedRequestType(requestTypeId, workflowId: 1);
        await SeedWorkflowStep(1, 1, 3, false);

        var request = new AssetRequest
        {
            AssetRequestId = _context.AssetRequests.Count() == 0 ? 1 : _context.AssetRequests.Max(r => r.AssetRequestId) + 1,
            UserId = userId,
            RequestTypeId = requestTypeId,
            Title = title,
            Status = status,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
            StepId = 1
        };

        _context.AssetRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    private async Task SeedRequestType(int requestTypeId, int workflowId)
    {
        if (!await _context.RequestTypes.AnyAsync(rt => rt.RequestTypeId == requestTypeId))
        {
            _context.RequestTypes.Add(new RequestType
            {
                RequestTypeId = requestTypeId,
                WorkflowId = workflowId
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedWorkflowStep(int workflowId, int stepOrder, int roleId, bool isFinalStep)
    {
        if (!await _context.WorkflowSteps.AnyAsync(ws => ws.WorkflowId == workflowId && ws.StepOrder == stepOrder))
        {
            _context.WorkflowSteps.Add(new WorkflowStep
            {
                StepId = _context.WorkflowSteps.Count() == 0 ? 1 : _context.WorkflowSteps.Max(ws => ws.StepId) + 1,
                WorkflowId = workflowId,
                StepOrder = stepOrder,
                RoleId = roleId,
                IsFinalStep = isFinalStep
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedUser(int userId, int status)
    {
        if (!await _context.Users.AnyAsync(u => u.UserId == userId))
        {
            _context.Users.Add(new User
            {
                UserId = userId,
                Email = $"user{userId}@test.com",
                Password = "hashed",
                Status = status
            });
            await _context.SaveChangesAsync();
        }
    }

    #endregion
}
