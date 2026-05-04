using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.SupplierInvoices;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class SupplierInvoicesControllerTests
{
    private readonly Mock<ISupplierInvoiceService> _mockService = null!;
    private readonly SupplierInvoicesController _controller = null!;

    public SupplierInvoicesControllerTests()
    {
        _mockService = new Mock<ISupplierInvoiceService>();
        _controller = new SupplierInvoicesController(_mockService.Object);
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

    private static SupplierInvoiceCreateDto CreateValidDto(
        int procurementId = 1,
        DateOnly? invoiceDate = null,
        string invoiceNumber = "INV-001")
    {
        return new SupplierInvoiceCreateDto
        {
            ProcurementId = procurementId,
            InvoiceNumber = invoiceNumber,
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

    [Fact]
    public async Task Create_ValidData_ReturnsOk()
    {
        var dto = CreateValidDto(procurementId: 1, invoiceDate: DateOnly.FromDateTime(DateTime.UtcNow));

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ReturnsAsync(1);

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_PurchaseOrderIdZero_ReturnsNotFound()
    {
        var dto = CreateValidDto(procurementId: 0);

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new KeyNotFoundException("Purchase order not found"));

        var result = await _controller.Create(dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_PurchaseOrderIdNegative_ReturnsNotFound()
    {
        var dto = CreateValidDto(procurementId: -1);

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new KeyNotFoundException("Purchase order not found"));

        var result = await _controller.Create(dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_SupplierIdZero_ReturnsBadRequest()
    {
        var dto = CreateValidDto(procurementId: 1);

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new Exception("Purchase order has no supplier"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvoiceNumberEmpty_ReturnsBadRequest()
    {
        var dto = CreateValidDto(procurementId: 1, invoiceNumber: "");

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new Exception("Invoice number is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvoiceNumberNull_ReturnsBadRequest()
    {
        var dto = CreateValidDto(procurementId: 1, invoiceNumber: "");

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new Exception("Invoice number is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvoiceNumberWhitespace_ReturnsBadRequest()
    {
        var dto = CreateValidDto(procurementId: 1, invoiceNumber: "   ");

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new Exception("Invoice number is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvoiceDateInFuture_ReturnsOk()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = CreateValidDto(procurementId: 1, invoiceDate: futureDate);

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ReturnsAsync(1);

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvoiceDateInPast_ReturnsOk()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var dto = CreateValidDto(procurementId: 1, invoiceDate: pastDate);

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ReturnsAsync(1);

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public async Task Create_NullDto_ReturnsBadRequest()
    {
        SetUserClaim(1);
        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new ArgumentException("Request body is required"));

        var result = await _controller.Create(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_EmptyLines_ReturnsBadRequest()
    {
        var dto = CreateValidDto(procurementId: 1);
        dto.Lines = new List<SupplierInvoiceCreateLineDto>();

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new Exception("At least one line is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_CancelledPurchaseOrder_ReturnsBadRequest()
    {
        var dto = CreateValidDto(procurementId: 1);

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new Exception("Cannot create invoice for cancelled purchase order"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_DuplicateInvoiceNumber_ReturnsBadRequest()
    {
        var dto = CreateValidDto(procurementId: 1, invoiceNumber: "INV-DUP");

        _mockService.Setup(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<SupplierInvoiceCreateDto>()))
            .ThrowsAsync(new Exception("An active invoice with this number already exists"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();
        var dto = CreateValidDto(procurementId: 1);

        var result = await _controller.Create(dto);

        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_ValidId_ReturnsNoContent()
    {
        _mockService.Setup(s => s.CancelAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Cancel(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Cancel_InvoiceNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.CancelAsync(It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Invoice not found"));

        var result = await _controller.Cancel(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();

        var result = await _controller.Cancel(1);

        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region GetList Tests

    [Fact]
    public async Task GetList_ReturnsOk()
    {
        var response = new SupplierInvoiceListResponseDto
        {
            Items = new List<SupplierInvoiceListItemDto>(),
            Total = 0,
            Page = 1,
            PageSize = 20,
            TotalPages = 0
        };

        _mockService.Setup(s => s.GetListAsync(
            It.IsAny<string?>(), It.IsAny<int?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(response);

        var result = await _controller.GetList(null, null, null, null, 1, 20);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetList_ServiceThrowsKeyNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetListAsync(
            It.IsAny<string?>(), It.IsAny<int?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));

        var result = await _controller.GetList(null, null, null, null, 1, 20);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetList_ServiceThrowsException_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.GetListAsync(
            It.IsAny<string?>(), It.IsAny<int?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service error"));

        var result = await _controller.GetList(null, null, null, null, 1, 20);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOk()
    {
        var detail = new SupplierInvoiceDetailDto
        {
            SupplierInvoiceId = 1,
            InvoiceNumber = "INV-001",
            SupplierId = 1,
            SupplierName = "Test Supplier",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Currency = "VND",
            TotalAmount = 500000m,
            Status = 0,
            ProcurementId = 1,
            Lines = new List<SupplierInvoiceDetailLineDto>()
        };

        _mockService.Setup(s => s.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(detail);

        var result = await _controller.GetById(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_InvoiceNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Invoice not found"));

        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ServiceThrowsException_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service error"));

        var result = await _controller.GetById(1);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion
}
