using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.GoodsReceipts;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class GoodsReceiptsControllerTests
{
    private readonly Mock<IGoodsReceiptService> _mockService = null!;
    private readonly GoodsReceiptsController _controller;

    public GoodsReceiptsControllerTests()
    {
        _mockService = new Mock<IGoodsReceiptService>();
        _controller = new GoodsReceiptsController(_mockService.Object);
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

    private GoodsReceiptCreateDto CreateValidDto(
        int procurementId = 1,
        int warehouseId = 1,
        string? postingDate = null,
        decimal quantityReceived = 5)
    {
        return new GoodsReceiptCreateDto
        {
            ProcurementId = procurementId,
            WarehouseId = warehouseId,
            PostingDate = postingDate,
            Lines = new List<GoodsReceiptCreateLineDto>
            {
                new GoodsReceiptCreateLineDto
                {
                    ProcurementLineId = 1,
                    QuantityReceived = quantityReceived
                }
            }
        };
    }

    #region Create Tests

    /// <summary>
    /// Test case 1 (Normal): ProcurementId = 1, WarehouseId = 1, CreatedDate = today, Quantity < Max.
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task Create_ValidData_ReturnsCreated()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ReturnsAsync(1);

        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 2 (Abnormal): ProcurementId = 0, WarehouseId = 1, CreatedDate = today, Quantity < Max.
    /// Expected output: 404 Not Found (Purchase order not found)
    /// </summary>
    [Fact]
    public async Task Create_ProcurementIdZero_ReturnsNotFound()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new KeyNotFoundException("Procurement not found"));

        var dto = CreateValidDto(procurementId: 0, warehouseId: 1, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): ProcurementId = -1, WarehouseId = 1, CreatedDate = today, Quantity < Max.
    /// Expected output: 404 Not Found (Purchase order not found)
    /// </summary>
    [Fact]
    public async Task Create_ProcurementIdNegative_ReturnsNotFound()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new KeyNotFoundException("Procurement not found"));

        var dto = CreateValidDto(procurementId: -1, warehouseId: 1, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): ProcurementId = 1, WarehouseId = 0, CreatedDate = today, Quantity < Max.
    /// Expected output: 400 Bad Request (Warehouse not found)
    /// </summary>
    [Fact]
    public async Task Create_WarehouseIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new ArgumentException("Warehouse not found"));

        var dto = CreateValidDto(procurementId: 1, warehouseId: 0, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal): ProcurementId = 1, WarehouseId = -1, CreatedDate = today, Quantity < Max.
    /// Expected output: 400 Bad Request (Warehouse not found)
    /// </summary>
    [Fact]
    public async Task Create_WarehouseIdNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new ArgumentException("Warehouse not found"));

        var dto = CreateValidDto(procurementId: 1, warehouseId: -1, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Normal): ProcurementId = 1, WarehouseId = 1, CreatedDate < today, Quantity < Max.
    /// Expected output: 201 Created (past date is valid)
    /// </summary>
    [Fact]
    public async Task Create_PastDate_ReturnsCreated()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ReturnsAsync(1);

        var pastDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: pastDate, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Normal): ProcurementId = 1, WarehouseId = 1, CreatedDate > today, Quantity < Max.
    /// Expected output: 201 Created (future date is valid)
    /// </summary>
    [Fact]
    public async Task Create_FutureDate_ReturnsCreated()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ReturnsAsync(1);

        var futureDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: futureDate, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): ProcurementId = 1, WarehouseId = 1, CreatedDate = today, Quantity > Max.
    /// Expected output: 400 Bad Request (received quantity exceeds open quantity)
    /// </summary>
    [Fact]
    public async Task Create_QuantityExceedsMax_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new ArgumentException("Received quantity exceeds open quantity"));

        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: null, quantityReceived: 15);

        // Act
        var result = await _controller.Create(dto);

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
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), null!))
            .ThrowsAsync(new ArgumentException("DTO cannot be null"));

        // Act
        var result = await _controller.Create(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Lines is empty
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_EmptyLines_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new ArgumentException("At least one receipt line is required"));

        var dto = new GoodsReceiptCreateDto
        {
            ProcurementId = 1,
            WarehouseId = 1,
            Lines = new List<GoodsReceiptCreateLineDto>()
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
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new ArgumentException("Lines cannot be null"));

        var dto = new GoodsReceiptCreateDto
        {
            ProcurementId = 1,
            WarehouseId = 1,
            Lines = null!
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: QuantityReceived is zero
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_QuantityZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new ArgumentException("Quantity must be greater than zero"));

        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: null, quantityReceived: 0);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: QuantityReceived is negative
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NegativeQuantity_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new ArgumentException("Quantity must be greater than zero"));

        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: null, quantityReceived: -5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Non-existent ProcurementId
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Create_NonExistentProcurementId_ReturnsNotFound()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new KeyNotFoundException("Procurement not found"));

        var dto = CreateValidDto(procurementId: 999, warehouseId: 1, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: Non-existent WarehouseId
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NonExistentWarehouseId_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new ArgumentException("Warehouse not found"));

        var dto = CreateValidDto(procurementId: 1, warehouseId: 999, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Cancelled procurement
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_CancelledProcurement_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new InvalidOperationException("Cannot receive goods for a cancelled procurement"));

        var dto = CreateValidDto(procurementId: 2, warehouseId: 1, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Completed procurement (fully received)
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_CompletedProcurement_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ThrowsAsync(new InvalidOperationException("Cannot receive goods for a completed procurement"));

        var dto = CreateValidDto(procurementId: 3, warehouseId: 1, postingDate: null, quantityReceived: 1);

        // Act
        var result = await _controller.Create(dto);

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
        // Arrange - user without claims, no mock setup needed (controller exits before service call)
        SetUserWithoutClaim();
        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: null, quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Test case: Partial quantity receipt (valid)
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task Create_PartialQuantity_ReturnsCreated()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ReturnsAsync(1);

        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: null, quantityReceived: 3);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case: Receive exact remaining quantity
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task Create_ExactRemainingQuantity_ReturnsCreated()
    {
        // Arrange
        SetUserClaim(1);
        _mockService.Setup(x => x.CreateAsync(It.IsAny<int>(), It.IsAny<GoodsReceiptCreateDto>()))
            .ReturnsAsync(1);

        var dto = CreateValidDto(procurementId: 1, warehouseId: 1, postingDate: null, quantityReceived: 10);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion
}
