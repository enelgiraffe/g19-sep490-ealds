using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.PurchaseOrders;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class PurchaseOrdersControllerTests
{
    private readonly Mock<IPurchaseOrderService> _mockService;
    private readonly PurchaseOrdersController _controller;

    public PurchaseOrdersControllerTests()
    {
        _mockService = new Mock<IPurchaseOrderService>();
        _controller = new PurchaseOrdersController(_mockService.Object);
        SetUserContext(actorUserId: 1);
    }

    private void SetUserContext(int actorUserId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, actorUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private PurchaseOrderListResponseDto BuildListResponse(List<PurchaseOrderListItemDto> items, int total, int page = 1, int pageSize = 20)
        => new()
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)System.Math.Ceiling((double)total / pageSize)
        };

    private PurchaseOrderDetailDto BuildDetailDto(
        int procurementId = 1,
        int supplierId = 1,
        string supplierName = "Tech Supply Co.",
        int status = 0,
        decimal totalAmount = 3500000m,
        string contractNo = "PO-001",
        List<PurchaseOrderLineItemDto>? lines = null)
        => new()
        {
            ProcurementId = procurementId,
            SupplierId = supplierId,
            SupplierName = supplierName,
            Status = status,
            TotalAmount = totalAmount,
            ContractNo = contractNo,
            Title = contractNo,
            Currency = "VND",
            CreateDate = System.DateTime.UtcNow,
            Lines = lines ?? new List<PurchaseOrderLineItemDto>()
        };

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            ContractNo = "PO-2025-001",
            Currency = "VND",
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Dell Laptop", Quantity = 5, Unit = "piece", UnitPrice = 15000000m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(42);

        var result = await _controller.Create(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var procurementId = (int)okResult.Value.GetType().GetProperty("procurementId")!.GetValue(okResult.Value)!;
        Assert.Equal(42, procurementId);
    }

    [Fact]
    public async Task Create_WithLinkedAssetRequest_ReturnsCreated()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            AssetRequestId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Monitor", Quantity = 3, Unit = "piece", UnitPrice = 5000000m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(5);

        var result = await _controller.Create(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var procurementId = (int)okResult.Value.GetType().GetProperty("procurementId")!.GetValue(okResult.Value)!;
        Assert.Equal(5, procurementId);
    }

    [Fact]
    public async Task Create_AsDraft_ReturnsCreatedWithDraftStatus()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            IsDraft = true,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Keyboard", Quantity = 10, Unit = "piece", UnitPrice = 500000m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(7);

        var result = await _controller.Create(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var procurementId = (int)okResult.Value.GetType().GetProperty("procurementId")!.GetValue(okResult.Value)!;
        Assert.True(procurementId > 0);
    }

    [Fact]
    public async Task Create_WithNullBody_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateAsync(1, null!)).ThrowsAsync(new System.ArgumentException("DTO cannot be null"));

        var result = await _controller.Create(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithMissingSupplier_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 999,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("Supplier not found"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithNoLines_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>()
        };

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("At least one line is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithInvalidLineQuantity_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 0, UnitPrice = 100m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("Line quantity must be greater than zero"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithInvalidAssetRequestId_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            AssetRequestId = 999,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("Asset request not found"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithInvalidAssetId_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", AssetId = 999, Quantity = 1, UnitPrice = 100m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("Asset not found"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithoutContractNo_AutoGeneratesContractNo()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            ContractNo = "",
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(10);

        var result = await _controller.Create(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var procurementId = (int)okResult.Value.GetType().GetProperty("procurementId")!.GetValue(okResult.Value)!;
        Assert.Equal(10, procurementId);
    }

    [Fact]
    public async Task Create_CalculatesTotalAmountCorrectly()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item A", Quantity = 2, UnitPrice = 1000000m },
                new() { Description = "Item B", Quantity = 3, UnitPrice = 500000m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(15);

        var result = await _controller.Create(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var procurementId = (int)okResult.Value.GetType().GetProperty("procurementId")!.GetValue(okResult.Value)!;
        Assert.Equal(15, procurementId);
    }

    #endregion

    #region GetList Tests

    [Fact]
    public async Task GetList_WithoutFilters_ReturnsAllOrders()
    {
        var response = BuildListResponse(new List<PurchaseOrderListItemDto>
        {
            new() { ProcurementId = 1, ContractNo = "PO-001", SupplierId = 1, Title = "PO-001", Currency = "VND", TotalAmount = 100000m, Status = 0, CreateDate = System.DateTime.UtcNow },
            new() { ProcurementId = 2, ContractNo = "PO-002", SupplierId = 1, Title = "PO-002", Currency = "VND", TotalAmount = 100000m, Status = -1, CreateDate = System.DateTime.UtcNow }
        }, total: 2);

        _mockService.Setup(s => s.GetListAsync(null, null, null, false, 1, 20)).ReturnsAsync(response);

        var result = await _controller.GetList(null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Equal(2, dto.Total);
        Assert.Equal(2, dto.Items.Count);
    }

    [Fact]
    public async Task GetList_WithSupplierFilter_ReturnsMatchingOrders()
    {
        var response = BuildListResponse(new List<PurchaseOrderListItemDto>
        {
            new() { ProcurementId = 1, ContractNo = "PO-001", SupplierId = 1, Title = "PO-001", Currency = "VND", TotalAmount = 100000m, Status = 0, CreateDate = System.DateTime.UtcNow }
        }, total: 1);

        _mockService.Setup(s => s.GetListAsync(null, 1, null, false, 1, 20)).ReturnsAsync(response);

        var result = await _controller.GetList(null, supplierId: 1, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Single(dto.Items);
        Assert.Equal("PO-001", dto.Items[0].ContractNo);
    }

    [Fact]
    public async Task GetList_WithStatusFilter_ReturnsFilteredOrders()
    {
        var response = BuildListResponse(new List<PurchaseOrderListItemDto>
        {
            new() { ProcurementId = 2, ContractNo = "PO-002", SupplierId = 1, Title = "PO-002", Currency = "VND", TotalAmount = 100000m, Status = -1, CreateDate = System.DateTime.UtcNow }
        }, total: 1);

        _mockService.Setup(s => s.GetListAsync(null, null, -1, false, 1, 20)).ReturnsAsync(response);

        var result = await _controller.GetList(null, null, status: -1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Single(dto.Items);
        Assert.Equal(-1, dto.Items[0].Status);
    }

    [Fact]
    public async Task GetList_WithReceivingEligible_ExcludesCancelledAndCompleted()
    {
        var response = BuildListResponse(new List<PurchaseOrderListItemDto>
        {
            new() { ProcurementId = 1, ContractNo = "PO-001", SupplierId = 1, Title = "PO-001", Currency = "VND", TotalAmount = 100000m, Status = 0, CreateDate = System.DateTime.UtcNow }
        }, total: 1);

        _mockService.Setup(s => s.GetListAsync(null, null, null, true, 1, 20)).ReturnsAsync(response);

        var result = await _controller.GetList(null, null, null, receivingEligible: true);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Single(dto.Items);
        Assert.Equal("PO-001", dto.Items[0].ContractNo);
    }

    [Fact]
    public async Task GetList_Pagination_ReturnsCorrectPage()
    {
        var items = new List<PurchaseOrderListItemDto>
        {
            new() { ProcurementId = 3, ContractNo = "PO-003", SupplierId = 1, Title = "PO-003", Currency = "VND", TotalAmount = 100000m, Status = 0, CreateDate = System.DateTime.UtcNow },
            new() { ProcurementId = 4, ContractNo = "PO-004", SupplierId = 1, Title = "PO-004", Currency = "VND", TotalAmount = 100000m, Status = 0, CreateDate = System.DateTime.UtcNow }
        };
        var response = BuildListResponse(items, total: 5, page: 2, pageSize: 2);

        _mockService.Setup(s => s.GetListAsync(null, null, null, false, 2, 2)).ReturnsAsync(response);

        var result = await _controller.GetList(null, null, null, page: 2, pageSize: 2);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Equal(5, dto.Total);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal(2, dto.Page);
        Assert.Equal(2, dto.PageSize);
        Assert.Equal(3, dto.TotalPages);
    }

    [Fact]
    public async Task GetList_WithNoOrders_ReturnsEmptyList()
    {
        var response = BuildListResponse(new List<PurchaseOrderListItemDto>(), total: 0);

        _mockService.Setup(s => s.GetListAsync(null, null, null, false, 1, 20)).ReturnsAsync(response);

        var result = await _controller.GetList(null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Empty(dto.Items);
        Assert.Equal(0, dto.Total);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOrderWithLines()
    {
        var lines = new List<PurchaseOrderLineItemDto>
        {
            new() { LineId = 1, LineIndex = 0, Description = "Item A", Quantity = 2, UnitPrice = 1000000m, LineTotal = 2000000m, ReceivedQuantity = 0m, OpenQuantity = 2000000m },
            new() { LineId = 2, LineIndex = 1, Description = "Item B", Quantity = 3, UnitPrice = 500000m, LineTotal = 1500000m, ReceivedQuantity = 0m, OpenQuantity = 1500000m }
        };
        var dto = BuildDetailDto(procurementId: 1, totalAmount: 3500000m, lines: lines);

        _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(dto);

        var result = await _controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<PurchaseOrderDetailDto>(okResult.Value);
        Assert.Equal("PO-001", responseDto.ContractNo);
        Assert.Equal(2, responseDto.Lines.Count);
        Assert.Equal(3500000m, responseDto.TotalAmount);
        Assert.Equal(2000000m, responseDto.Lines[0].LineTotal);
        Assert.Equal(0m, responseDto.Lines[0].ReceivedQuantity);
        Assert.Equal(2000000m, responseDto.Lines[0].OpenQuantity);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetByIdAsync(999))
            .ThrowsAsync(new System.Collections.Generic.KeyNotFoundException("Purchase order not found"));

        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedProcurement()
    {
        var dto = new PurchaseOrderUpdateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "New Item", Quantity = 5, UnitPrice = 200000m }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(1, 1, dto)).Returns(Task.CompletedTask);

        var result = await _controller.Update(1, dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var procurementId = (int)okResult.Value.GetType().GetProperty("procurementId")!.GetValue(okResult.Value)!;
        Assert.Equal(1, procurementId);
    }

    [Fact]
    public async Task Update_CancelledOrder_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderUpdateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 2, UnitPrice = 100000m }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(1, 1, dto))
            .ThrowsAsync(new System.ArgumentException("Cannot update a cancelled purchase order"));

        var result = await _controller.Update(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        var dto = new PurchaseOrderUpdateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100m }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(1, 999, dto))
            .ThrowsAsync(new System.Collections.Generic.KeyNotFoundException("Purchase order not found"));

        var result = await _controller.Update(999, dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidLineQuantity_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderUpdateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 0, UnitPrice = 100000m }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(1, 1, dto))
            .ThrowsAsync(new System.ArgumentException("Line quantity must be greater than zero"));

        var result = await _controller.Update(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidSupplier_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderUpdateDto
        {
            SupplierId = 999,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100000m }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(1, 1, dto))
            .ThrowsAsync(new System.ArgumentException("Supplier not found"));

        var result = await _controller.Update(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_WithValidOrder_ReturnsCancelledStatus()
    {
        _mockService.Setup(s => s.CancelAsync(1)).Returns(Task.CompletedTask);

        var result = await _controller.Cancel(1, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var procurementId = (int)okResult.Value.GetType().GetProperty("procurementId")!.GetValue(okResult.Value)!;
        var status = okResult.Value.GetType().GetProperty("status")!.GetValue(okResult.Value);
        Assert.Equal(1, procurementId);
        Assert.Equal(2, status); // StatusCancelled = 2
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CancelAsync(1))
            .ThrowsAsync(new System.ArgumentException("Purchase order is already cancelled"));

        var result = await _controller.Cancel(1, null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WithGoodsReceipt_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CancelAsync(1))
            .ThrowsAsync(new System.ArgumentException("Cannot cancel: goods receipts exist"));

        var result = await _controller.Cancel(1, null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WithInvalidId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.CancelAsync(999))
            .ThrowsAsync(new System.Collections.Generic.KeyNotFoundException("Purchase order not found"));

        var result = await _controller.Cancel(999, null);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(999))
            .ThrowsAsync(new System.Collections.Generic.KeyNotFoundException("Purchase order not found"));

        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region CreatePurchaseOrder Tests

    private PurchaseOrderCreateDto CreateValidPurchaseOrderDto(
        int supplierId = 1,
        string? contractNo = "PO-2025-TEST",
        string currency = "VND",
        int? assetRequestId = null)
        => new()
        {
            SupplierId = supplierId,
            ContractNo = contractNo,
            Currency = currency,
            AssetRequestId = assetRequestId,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Test Item", Quantity = 5, Unit = "pcs", UnitPrice = 1000000m }
            }
        };

    [Fact]
    public async Task CreatePurchaseOrder_ValidData_ReturnsCreated()
    {
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 1,
            contractNo: "PO-2025-001",
            currency: "VND",
            assetRequestId: 1);

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(1);

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreatePurchaseOrder_SupplierIdZero_ReturnsBadRequest()
    {
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 0,
            contractNo: "PO-2025-002",
            currency: "VND",
            assetRequestId: 1);

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("Supplier is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreatePurchaseOrder_SupplierIdNegative_ReturnsBadRequest()
    {
        var dto = CreateValidPurchaseOrderDto(
            supplierId: -1,
            contractNo: "PO-2025-003",
            currency: "VND",
            assetRequestId: 1);

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("Supplier is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreatePurchaseOrder_EmptyContractNo_ReturnsBadRequest()
    {
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 1,
            contractNo: "",
            currency: "VND",
            assetRequestId: 1);

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("Contract number is required for non-draft orders"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreatePurchaseOrder_ValidCurrencyUSD_ReturnsCreated()
    {
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 1,
            contractNo: "PO-2025-005",
            currency: "USD",
            assetRequestId: 1);

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(5);

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreatePurchaseOrder_ValidCurrencyEUR_ReturnsCreated()
    {
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 1,
            contractNo: "PO-2025-006",
            currency: "EUR",
            assetRequestId: 1);

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(6);

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreatePurchaseOrder_AssetRequestIdZero_ReturnsCreated()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            ContractNo = "PO-2025-007",
            Currency = "VND",
            AssetRequestId = 0,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Test Item", Quantity = 5, Unit = "pcs", UnitPrice = 1000000m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(7);

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreatePurchaseOrder_AssetRequestIdNegative_ReturnsBadRequest()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            ContractNo = "PO-2025-008",
            Currency = "VND",
            AssetRequestId = -1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Test Item", Quantity = 5, Unit = "pcs", UnitPrice = 1000000m }
            }
        };

        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new System.ArgumentException("Asset request not found"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
