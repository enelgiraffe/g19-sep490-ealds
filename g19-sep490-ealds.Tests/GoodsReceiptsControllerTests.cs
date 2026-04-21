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

public class GoodsReceiptsControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly Mock<IAssetRequestNotificationService> _mockNotificationService;
    private readonly Mock<IMaintenanceTemplateService> _mockMaintenanceService;
    private readonly GoodsReceiptsController _controller;

    public GoodsReceiptsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        _mockNotificationService = new Mock<IAssetRequestNotificationService>();
        _mockNotificationService
            .Setup(x => x.NotifyFirstApproversAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMaintenanceService = new Mock<IMaintenanceTemplateService>();
        _mockMaintenanceService
            .Setup(x => x.EnsureSchedulesForNewInstanceAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "App:AllocationRequestTypeId", "6" }
            })
            .Build();

        _controller = new GoodsReceiptsController(
            _context,
            _mockNotificationService.Object,
            _mockMaintenanceService.Object,
            configuration);

        SeedTestData().Wait();
        SetUserClaim(1);
    }

    private async Task SeedTestData()
    {
        // Seed Supplier
        _context.Suppliers.Add(new Supplier
        {
            SupplierId = 1,
            Code = "SUP001",
            Name = "Test Supplier",
            Status = 1,
            CreateDate = DateTime.UtcNow
        });

        // Seed Warehouse
        _context.Warehouses.Add(new Warehouse
        {
            WarehouseId = 1,
            Code = "WH001",
            Name = "Main Warehouse",
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

        // Seed Procurement (Purchase Order) with lines
        var procurement = new Procurement
        {
            ProcurementId = 1,
            SupplierId = 1,
            ContractNo = "PO-2025-001",
            Title = "Purchase Order",
            Currency = "VND",
            TotalAmount = 50000000m,
            RemainingAmount = 50000000m,
            Status = 0, // StatusCreated
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow
        };
        _context.Procurements.Add(procurement);

        // Seed Procurement Line with max quantity of 10
        _context.ProcurementLines.Add(new ProcurementLine
        {
            LineId = 1,
            ProcurementId = 1,
            LineIndex = 0,
            Description = "Laptop",
            Quantity = 10,
            UnitPrice = 5000000m,
            ReceivedQuantity = 0,
            AssetId = 1
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
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 1,
            postingDate: null, // today
            quantityReceived: 5); // less than max (10)

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.Equal(201, createdResult.StatusCode);
    }

    /// <summary>
    /// Test case 2 (Abnormal): ProcurementId = 0, WarehouseId = 1, CreatedDate = today, Quantity < Max.
    /// Expected output: 404 Not Found (Purchase order not found)
    /// </summary>
    [Fact]
    public async Task Create_ProcurementIdZero_ReturnsNotFound()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: 0,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Test case 3 (Abnormal): ProcurementId = -1, WarehouseId = 1, CreatedDate = today, Quantity < Max.
    /// Expected output: 404 Not Found (Purchase order not found)
    /// </summary>
    [Fact]
    public async Task Create_ProcurementIdNegative_ReturnsNotFound()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: -1,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Test case 4 (Abnormal): ProcurementId = 1, WarehouseId = 0, CreatedDate = today, Quantity < Max.
    /// Expected output: 400 Bad Request (Warehouse not found)
    /// </summary>
    [Fact]
    public async Task Create_WarehouseIdZero_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 0,
            postingDate: null,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test case 5 (Abnormal): ProcurementId = 1, WarehouseId = -1, CreatedDate = today, Quantity < Max.
    /// Expected output: 400 Bad Request (Warehouse not found)
    /// </summary>
    [Fact]
    public async Task Create_WarehouseIdNegative_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: -1,
            postingDate: null,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test case 6 (Normal): ProcurementId = 1, WarehouseId = 1, CreatedDate < today, Quantity < Max.
    /// Expected output: 201 Created (past date is valid)
    /// </summary>
    [Fact]
    public async Task Create_PastDate_ReturnsCreated()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 1,
            postingDate: pastDate,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>
    /// Test case 7 (Normal): ProcurementId = 1, WarehouseId = 1, CreatedDate > today, Quantity < Max.
    /// Expected output: 201 Created (future date is valid)
    /// </summary>
    [Fact]
    public async Task Create_FutureDate_ReturnsCreated()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 1,
            postingDate: futureDate,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): ProcurementId = 1, WarehouseId = 1, CreatedDate = today, Quantity > Max.
    /// Expected output: 400 Bad Request (received quantity exceeds open quantity)
    /// </summary>
    [Fact]
    public async Task Create_QuantityExceedsMax_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 15); // exceeds max (10)

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
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
        // Act
        var result = await _controller.Create(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Lines is empty
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_EmptyLines_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GoodsReceiptCreateDto
        {
            ProcurementId = 1,
            WarehouseId = 1,
            Lines = new List<GoodsReceiptCreateLineDto>()
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Lines is null
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullLines_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GoodsReceiptCreateDto
        {
            ProcurementId = 1,
            WarehouseId = 1,
            Lines = null!
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: QuantityReceived is zero
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_QuantityZero_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 0);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: QuantityReceived is negative
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NegativeQuantity_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: -5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Non-existent ProcurementId
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Create_NonExistentProcurementId_ReturnsNotFound()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: 999,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Non-existent WarehouseId
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NonExistentWarehouseId_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 999,
            postingDate: null,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Cancelled procurement
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_CancelledProcurement_ReturnsBadRequest()
    {
        // Arrange
        var cancelledProcurement = new Procurement
        {
            ProcurementId = 2,
            SupplierId = 1,
            ContractNo = "PO-2025-002",
            Title = "Cancelled PO",
            Currency = "VND",
            TotalAmount = 10000000m,
            RemainingAmount = 10000000m,
            Status = 2, // StatusCancelled
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow
        };
        _context.Procurements.Add(cancelledProcurement);
        _context.ProcurementLines.Add(new ProcurementLine
        {
            LineId = 2,
            ProcurementId = 2,
            LineIndex = 0,
            Description = "Item",
            Quantity = 5,
            UnitPrice = 2000000m,
            ReceivedQuantity = 0,
            AssetId = 1
        });
        await _context.SaveChangesAsync();

        var dto = CreateValidDto(
            procurementId: 2,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Completed procurement (fully received)
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_CompletedProcurement_ReturnsBadRequest()
    {
        // Arrange
        var completedProcurement = new Procurement
        {
            ProcurementId = 3,
            SupplierId = 1,
            ContractNo = "PO-2025-003",
            Title = "Completed PO",
            Currency = "VND",
            TotalAmount = 10000000m,
            RemainingAmount = 0m,
            Status = 3, // StatusCompleted
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow
        };
        _context.Procurements.Add(completedProcurement);
        _context.ProcurementLines.Add(new ProcurementLine
        {
            LineId = 3,
            ProcurementId = 3,
            LineIndex = 0,
            Description = "Item",
            Quantity = 5,
            UnitPrice = 2000000m,
            ReceivedQuantity = 5, // Fully received
            AssetId = 1
        });
        await _context.SaveChangesAsync();

        var dto = CreateValidDto(
            procurementId: 3,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
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
            procurementId: 1,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 5);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    /// <summary>
    /// Test case: Partial quantity receipt (valid)
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task Create_PartialQuantity_ReturnsCreated()
    {
        // Arrange - first receipt of 3 (partial of 10)
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 3);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        // Verify open quantity is now 7
        var line = await _context.ProcurementLines.FindAsync(1);
        Assert.Equal(3, line!.ReceivedQuantity);
    }

    /// <summary>
    /// Test case: Receive exact remaining quantity
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task Create_ExactRemainingQuantity_ReturnsCreated()
    {
        // Arrange - receive remaining 10 (full remaining from fresh PO)
        var dto = CreateValidDto(
            procurementId: 1,
            warehouseId: 1,
            postingDate: null,
            quantityReceived: 10);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        // Verify procurement status is updated to Completed
        var procurement = await _context.Procurements.FindAsync(1);
        Assert.Equal(3, procurement!.Status); // StatusCompleted
    }

    #endregion
}
