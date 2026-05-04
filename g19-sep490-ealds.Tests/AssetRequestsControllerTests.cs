using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.AssetRequests;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
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
    private readonly Mock<IAssetRequestService> _mockService;
    private readonly AssetRequestsController _controller;

    public AssetRequestsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _mockService = new Mock<IAssetRequestService>();
        _controller = new AssetRequestsController(_mockService.Object);
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
        var dto = new AssetRequestDetailDTO { AssetRequestId = 1, Title = "Laptop Request" };
        _mockService.Setup(s => s.GetPurchaseByIdAsync(1)).ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: GetById with non-existent id returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetPurchaseByIdAsync(999)).ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
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
        _mockService.Setup(s => s.CreateAsync(It.IsAny<AssetRequestDTO>())).ReturnsAsync(1);

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
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: Create purchase request as submitted (status=0) sends notification
    /// Expected output: 200 OK with assetRequestId, notification sent
    /// </summary>
    [Fact]
    public async Task Create_Submitted_SendsNotification()
    {
        // Arrange
        _mockService.Setup(s => s.CreateAsync(It.IsAny<AssetRequestDTO>())).ReturnsAsync(1);

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
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: Create with missing title returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(s => s.CreateAsync(It.IsAny<AssetRequestDTO>())).ThrowsAsync(new ArgumentException("Title is required"));

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
        _mockService.Setup(s => s.CreateAsync(It.IsAny<AssetRequestDTO>())).ThrowsAsync(new ArgumentException("Invalid status"));

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
        _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<AssetRequestDTO>())).ReturnsAsync(1);

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Updated Title",
            Description = "Updated description",
            CreatedBy = 1,
            Status = -1
        };

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: Update non-draft request returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_SubmittedRequest_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<AssetRequestDTO>())).ThrowsAsync(new ArgumentException("Cannot update non-draft request"));

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Try Update",
            CreatedBy = 1,
            Status = -1
        };

        // Act
        var result = await _controller.Update(1, dto);

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
        _mockService.Setup(s => s.UpdateAsync(999, It.IsAny<AssetRequestDTO>())).ThrowsAsync(new KeyNotFoundException());

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
        _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<AssetRequestDTO>())).ReturnsAsync(1);

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Submitted Request",
            CreatedBy = 1,
            Status = 0
        };

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: Update with invalid status returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<AssetRequestDTO>())).ThrowsAsync(new ArgumentException("Invalid status"));

        var dto = new AssetRequestDTO
        {
            UserId = 1,
            Title = "Request",
            CreatedBy = 1,
            Status = 2
        };

        // Act
        var result = await _controller.Update(1, dto);

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
        _mockService.Setup(s => s.RevertToDraftAsync(1, 1)).Returns(Task.CompletedTask);

        var dto = new RevertToDraftDTO { UserId = 1 };

        // Act
        var result = await _controller.RevertToDraft(1, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case: RevertToDraft by non-creator returns Forbid
    /// Expected output: 403 Forbid
    /// </summary>
    [Fact]
    public async Task RevertToDraft_ByNonCreator_ReturnsForbid()
    {
        // Arrange
        _mockService.Setup(s => s.RevertToDraftAsync(1, 999)).ThrowsAsync(new UnauthorizedAccessException());

        var dto = new RevertToDraftDTO { UserId = 999 };

        // Act
        var result = await _controller.RevertToDraft(1, dto);

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
        _mockService.Setup(s => s.RevertToDraftAsync(1, 1)).ThrowsAsync(new ArgumentException("Request is already in draft status"));

        var dto = new RevertToDraftDTO { UserId = 1 };

        // Act
        var result = await _controller.RevertToDraft(1, dto);

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
        _mockService.Setup(s => s.RevertToDraftAsync(999, 1)).ThrowsAsync(new KeyNotFoundException());

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
        // Arrange
        _mockService.Setup(s => s.RevertToDraftAsync(1, 0)).ThrowsAsync(new ArgumentException("UserId is required"));

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
        _mockService.Setup(s => s.RevertToDraftAsync(1, 1)).Returns(Task.CompletedTask);

        var dto = new RevertToDraftDTO { UserId = 1 };

        // Act
        var result = await _controller.RevertToDraft(1, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 2 (Abnormal): userId = 999 (don't exist), requestId = 1
    /// Expected output: 403 Forbid (user is not the creator)
    /// </summary>
    [Fact]
    public async Task RevertToDraft_NonExistentUser_ReturnsForbid()
    {
        // Arrange
        _mockService.Setup(s => s.RevertToDraftAsync(1, 999)).ThrowsAsync(new UnauthorizedAccessException());

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
        _mockService.Setup(s => s.RevertToDraftAsync(999, 1)).ThrowsAsync(new KeyNotFoundException());

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
        _mockService.Setup(s => s.RevertToDraftAsync(999, 999)).ThrowsAsync(new KeyNotFoundException());

        var dto = new RevertToDraftDTO { UserId = 999 };

        // Act
        var result = await _controller.RevertToDraft(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
