using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class SupplierInvoicesControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly SupplierInvoicesController _controller;

    public SupplierInvoicesControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new SupplierInvoicesController(_context);
        SetUserClaim(1);
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

    private async Task SeedSupplierAndProcurement(int supplierId = 1, int procurementId = 1, int status = 0)
    {
        _context.Suppliers.Add(new Supplier
        {
            SupplierId = supplierId,
            Code = $"SUP{supplierId}",
            Name = $"Supplier {supplierId}"
        });

        _context.Procurements.Add(new Procurement
        {
            ProcurementId = procurementId,
            SupplierId = supplierId,
            ContractNo = "PO-001",
            ContractDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Title = "Test Purchase Order",
            Currency = "VND",
            TotalAmount = 1000000m,
            AdvanceAmount = 0m,
            RemainingAmount = 1000000m,
            Status = status,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow
        });

        _context.ProcurementLines.Add(new ProcurementLine
        {
            LineId = 1,
            ProcurementId = procurementId,
            AssetId = 1,
            Quantity = 10,
            Unit = "pcs",
            UnitPrice = 100000m,
            TotalPrice = 1000000m,
            Status = 1
        });

        await _context.SaveChangesAsync();
    }

    private SupplierInvoiceCreateDto CreateValidDto(int procurementId = 1, DateOnly? invoiceDate = null)
    {
        return new SupplierInvoiceCreateDto
        {
            ProcurementId = procurementId,
            InvoiceNumber = "INV-001",
            InvoiceDate = invoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<SupplierInvoiceCreateLineDto>
            {
                new SupplierInvoiceCreateLineDto
                {
                    ProcurementLineId = 1,
                    Quantity = 5,
                    UnitPrice = 100000m
                }
            }
        };
    }

    #region Create Tests

    /// <summary>
    /// Test case 1 (Normal): PurchaseOrderId = 1, SupplierId = 1, InvoiceId = 1, Date = today.
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task Create_ValidData_ReturnsCreated()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var dto = CreateValidDto(procurementId: 1, invoiceDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        var createdResult = (CreatedAtActionResult)result;
        Assert.Equal(201, createdResult.StatusCode);
    }

    /// <summary>
    /// Test case 2 (Abnormal): PurchaseOrderId = 0, SupplierId = 1, InvoiceId = 1, Date = today.
    /// Expected output: 404 Not Found (Purchase order not found)
    /// </summary>
    [Fact]
    public async Task Create_PurchaseOrderIdZero_ReturnsNotFound()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var dto = CreateValidDto(procurementId: 0, invoiceDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        var notFoundResult = (NotFoundObjectResult)result;
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Test case 3 (Abnormal): PurchaseOrderId = -1, SupplierId = 1, InvoiceId = 1, Date = today.
    /// Expected output: 404 Not Found (Purchase order not found)
    /// </summary>
    [Fact]
    public async Task Create_PurchaseOrderIdNegative_ReturnsNotFound()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var dto = CreateValidDto(procurementId: -1, invoiceDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): PurchaseOrderId = 1, SupplierId = 0, InvoiceId = 1, Date = today.
    /// Expected output: 400 Bad Request (Purchase order has no supplier)
    /// </summary>
    [Fact]
    public async Task Create_SupplierIdZero_ReturnsBadRequest()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        // Update procurement to have no supplier (null or 0)
        var procurement = await _context.Procurements.FindAsync(1);
        procurement!.SupplierId = null;
        await _context.SaveChangesAsync();

        var dto = CreateValidDto(procurementId: 1, invoiceDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test case 5 (Abnormal): PurchaseOrderId = 1, SupplierId = 1, InvoiceId = Empty, Date = today.
    /// Expected output: 400 Bad Request (Invoice number is required)
    /// Note: InvoiceNumber is required (string), interpreted as empty/null InvoiceNumber
    /// </summary>
    [Fact]
    public async Task Create_InvoiceNumberEmpty_ReturnsBadRequest()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var dto = CreateValidDto(procurementId: 1, invoiceDate: DateOnly.FromDateTime(DateTime.UtcNow));
        dto.InvoiceNumber = ""; // Empty InvoiceNumber

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test case 5b (Abnormal): PurchaseOrderId = 1, SupplierId = 1, InvoiceId = null, Date = today.
    /// Expected output: 400 Bad Request (Invoice number is required)
    /// </summary>
    [Fact]
    public async Task Create_InvoiceNumberNull_ReturnsBadRequest()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var dto = CreateValidDto(procurementId: 1, invoiceDate: DateOnly.FromDateTime(DateTime.UtcNow));
        dto.InvoiceNumber = null!; // Null InvoiceNumber

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5c (Abnormal): PurchaseOrderId = 1, SupplierId = 1, InvoiceId = whitespace, Date = today.
    /// Expected output: 400 Bad Request (Invoice number is required)
    /// </summary>
    [Fact]
    public async Task Create_InvoiceNumberWhitespace_ReturnsBadRequest()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var dto = CreateValidDto(procurementId: 1, invoiceDate: DateOnly.FromDateTime(DateTime.UtcNow));
        dto.InvoiceNumber = "   "; // Whitespace InvoiceNumber

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Abnormal): PurchaseOrderId = 1, SupplierId = 1, InvoiceId = 1, Date >= today.
    /// Expected output: 201 Created (no validation on InvoiceDate)
    /// </summary>
    [Fact]
    public async Task Create_InvoiceDateInFuture_ReturnsCreated()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = CreateValidDto(procurementId: 1, invoiceDate: futureDate);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): PurchaseOrderId = 1, SupplierId = 1, InvoiceId = 1, Date <= today.
    /// Expected output: 201 Created (no validation on InvoiceDate)
    /// </summary>
    [Fact]
    public async Task Create_InvoiceDateInPast_ReturnsCreated()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var dto = CreateValidDto(procurementId: 1, invoiceDate: pastDate);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
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
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Lines is empty
    /// Expected output: 400 Bad Request (At least one line is required)
    /// </summary>
    [Fact]
    public async Task Create_EmptyLines_ReturnsBadRequest()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var dto = CreateValidDto(procurementId: 1);
        dto.Lines = new List<SupplierInvoiceCreateLineDto>(); // Empty lines

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Cancelled purchase order
    /// Expected output: 400 Bad Request (Cannot create invoice for cancelled purchase order)
    /// </summary>
    [Fact]
    public async Task Create_CancelledPurchaseOrder_ReturnsBadRequest()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1, status: 1); // Status = cancelled
        var dto = CreateValidDto(procurementId: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test case: Duplicate invoice number for same supplier
    /// Expected output: 400 Bad Request (An active invoice with this number already exists)
    /// </summary>
    [Fact]
    public async Task Create_DuplicateInvoiceNumber_ReturnsBadRequest()
    {
        // Arrange
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);

        // Create first invoice
        var dto1 = CreateValidDto(procurementId: 1);
        dto1.InvoiceNumber = "INV-DUP";
        await _controller.Create(dto1);

        // Try to create second invoice with same number
        var dto2 = CreateValidDto(procurementId: 1);
        dto2.InvoiceNumber = "INV-DUP";

        // Act
        var result = await _controller.Create(dto2);

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
        await SeedSupplierAndProcurement(supplierId: 1, procurementId: 1);
        var dto = CreateValidDto(procurementId: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion
}
