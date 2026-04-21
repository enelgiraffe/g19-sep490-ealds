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

public class PurchaseOrdersControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly PurchaseOrdersController _controller;

    public PurchaseOrdersControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new PurchaseOrdersController(_context);

        SetUserContext(actorUserId: 1);
        SeedTestDataAsync().Wait();
    }

    private async Task SeedTestDataAsync()
    {
        // Seed Supplier
        if (!await _context.Suppliers.AnyAsync())
        {
            _context.Suppliers.Add(new Supplier
            {
                SupplierId = 1,
                Code = "SUP001",
                Name = "Tech Supply Co.",
                Status = 1,
                CreateDate = DateTime.UtcNow
            });
        }

        // Seed AssetRequest for tests
        if (!await _context.AssetRequests.AnyAsync())
        {
            // Seed Department first
            if (!await _context.Departments.AnyAsync())
            {
                _context.Departments.Add(new Department
                {
                    DepartmentId = 1,
                    Name = "IT Department",
                    Code = "IT",
                    Status = 1,
                    CreateDate = DateTime.UtcNow,
                    CreatedBy = 1
                });
            }

            _context.AssetRequests.Add(new AssetRequest
            {
                AssetRequestId = 1,
                UserId = 2,
                RequestTypeId = 1,
                Title = "Purchase Laptop",
                Status = 2,
                CreatedBy = 2,
                CreateDate = DateTime.UtcNow,
                StepId = 1
            });
        }

        await _context.SaveChangesAsync();
    }

    #region Helper Methods

    /// <summary>
    /// Simulates the authenticated user via ClaimsPrincipal so that GetActorUserId() works.
    /// </summary>
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

    /// <summary>
    /// Seeds minimal required data: a Supplier and optional AssetRequest.
    /// </summary>
    private async Task<(Supplier supplier, AssetRequest? request)> SeedBaseDataAsync(bool withRequest = false)
    {
        var supplier = new Supplier
        {
            SupplierId = 1,
            Code = "SUP001",
            Name = "Tech Supply Co.",
            Status = 1,
            CreateDate = DateTime.UtcNow
        };

        AssetRequest? request = null;
        if (withRequest)
        {
            var dept = new Department
            {
                DepartmentId = 1,
                Name = "IT Department",
                Code = "IT",
                Status = 1,
                CreateDate = DateTime.UtcNow,
                CreatedBy = 1
            };
            _context.Departments.Add(dept);

            request = new AssetRequest
            {
                AssetRequestId = 1,
                UserId = 2,
                RequestTypeId = 1,
                Title = "Purchase Laptop",
                Status = 2,
                CreatedBy = 2,
                CreateDate = DateTime.UtcNow,
                StepId = 1
            };
            _context.AssetRequests.Add(request);
        }

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return (supplier, request);
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Test case: Create with valid data and at least one line returns 201 Created
    /// Expected output: Procurement record created with StatusCreated (0)
    /// </summary>
    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            ContractNo = "PO-2025-001",
            Currency = "VND",
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Dell Laptop", Quantity = 5, Unit = "piece", UnitPrice = 15000000m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.Value);

        var procurementId = (int)createdResult.Value.GetType().GetProperty("procurementId")!.GetValue(createdResult.Value)!;
        Assert.True(procurementId > 0);

        var saved = await _context.Procurements.FindAsync(procurementId);
        Assert.NotNull(saved);
        Assert.Equal("Dell Laptop", saved.Title);
        Assert.Equal(75000000m, saved.TotalAmount);
        Assert.Equal(PurchaseOrdersController.StatusCreated, saved.Status);
        Assert.Equal("VND", saved.Currency);
        Assert.Equal("PO-2025-001", saved.ContractNo);
    }

    /// <summary>
    /// Test case: Create with linked AssetRequestId creates procurement linked to request
    /// Expected output: Procurement.AssetRequestId is set
    /// </summary>
    [Fact]
    public async Task Create_WithLinkedAssetRequest_ReturnsCreated()
    {
        // Arrange
        var (supplier, request) = await SeedBaseDataAsync(withRequest: true);

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            AssetRequestId = request!.AssetRequestId,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Monitor", Quantity = 3, Unit = "piece", UnitPrice = 5000000m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var procurementId = (int)createdResult.Value!.GetType().GetProperty("procurementId")!.GetValue(createdResult.Value)!;
        var saved = await _context.Procurements.FindAsync(procurementId);
        Assert.NotNull(saved);
        Assert.Equal(request.AssetRequestId, saved.AssetRequestId);
    }

    /// <summary>
    /// Test case: Create as draft (IsDraft=true) sets status to StatusDraft (-1)
    /// Expected output: Procurement.Status == StatusDraft
    /// </summary>
    [Fact]
    public async Task Create_AsDraft_ReturnsCreatedWithDraftStatus()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            IsDraft = true,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Keyboard", Quantity = 10, Unit = "piece", UnitPrice = 500000m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var procurementId = (int)createdResult.Value!.GetType().GetProperty("procurementId")!.GetValue(createdResult.Value)!;
        var saved = await _context.Procurements.FindAsync(procurementId);
        Assert.NotNull(saved);
        Assert.Equal(PurchaseOrdersController.StatusDraft, saved.Status);
    }

    /// <summary>
    /// Test case: Create with null body returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithNullBody_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Create(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create without supplier returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithMissingSupplier_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 999,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create without line items returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithNoLines_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            Lines = new List<PurchaseOrderLineWriteDto>()
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create with line quantity <= 0 returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidLineQuantity_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 0, UnitPrice = 100m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create with non-existent AssetRequestId returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidAssetRequestId_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            AssetRequestId = 999,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create with line referencing non-existent AssetId returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidAssetId_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", AssetId = 999, Quantity = 1, UnitPrice = 100m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Auto-generates ContractNo as "PO-{id}" when ContractNo is empty
    /// Expected output: ContractNo follows pattern PO-{id}
    /// </summary>
    [Fact]
    public async Task Create_WithoutContractNo_AutoGeneratesContractNo()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            ContractNo = "",
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var procurementId = (int)createdResult.Value!.GetType().GetProperty("procurementId")!.GetValue(createdResult.Value)!;
        var saved = await _context.Procurements.FindAsync(procurementId);
        Assert.NotNull(saved);
        Assert.StartsWith("PO-", saved.ContractNo);
        Assert.Contains(procurementId.ToString(), saved.ContractNo);
    }

    /// <summary>
    /// Test case: Create calculates TotalAmount and RemainingAmount correctly
    /// Expected output: TotalAmount == sum(line.Quantity * line.UnitPrice), RemainingAmount == TotalAmount
    /// </summary>
    [Fact]
    public async Task Create_CalculatesTotalAmountCorrectly()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();

        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = supplier.SupplierId,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item A", Quantity = 2, UnitPrice = 1000000m },
                new() { Description = "Item B", Quantity = 3, UnitPrice = 500000m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var procurementId = (int)createdResult.Value!.GetType().GetProperty("procurementId")!.GetValue(createdResult.Value)!;
        var saved = await _context.Procurements.FindAsync(procurementId);
        Assert.NotNull(saved);
        Assert.Equal(3500000m, saved.TotalAmount);
        Assert.Equal(3500000m, saved.RemainingAmount);
        Assert.Equal(0m, saved.AdvanceAmount);
    }

    #endregion

    #region GetList Tests

    /// <summary>
    /// Test case: GetList returns all purchase orders
    /// Expected output: 200 OK with paginated list
    /// </summary>
    [Fact]
    public async Task GetList_WithoutFilters_ReturnsAllOrders()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        await CreateProcurementDirectly(supplier.SupplierId, status: -1, "PO-002");

        // Act
        var result = await _controller.GetList(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Equal(2, response.Total);
        Assert.Equal(2, response.Items.Count);
    }

    /// <summary>
    /// Test case: GetList filters by supplierId
    /// Expected output: 200 OK with only matching supplier orders
    /// </summary>
    [Fact]
    public async Task GetList_WithSupplierFilter_ReturnsMatchingOrders()
    {
        // Arrange
        await SeedBaseDataAsync();
        await CreateProcurementDirectly(supplierId: 1, status: 0, "PO-001");
        var supplier2 = new Supplier { SupplierId = 2, Code = "SUP002", Name = "Other Supplier", Status = 1, CreateDate = DateTime.UtcNow };
        _context.Suppliers.Add(supplier2);
        await _context.SaveChangesAsync();
        await CreateProcurementDirectly(supplierId: 2, status: 0, "PO-002");

        // Act
        var result = await _controller.GetList(null, supplierId: 1, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal("PO-001", response.Items[0].ContractNo);
    }

    /// <summary>
    /// Test case: GetList filters by status
    /// Expected output: 200 OK with filtered orders
    /// </summary>
    [Fact]
    public async Task GetList_WithStatusFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        await CreateProcurementDirectly(supplier.SupplierId, status: -1, "PO-002");
        await CreateProcurementDirectly(supplier.SupplierId, status: 2, "PO-003");

        // Act
        var result = await _controller.GetList(null, null, status: -1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal(-1, response.Items[0].Status);
    }

    /// <summary>
    /// Test case: GetList with receivingEligible=true excludes cancelled and completed
    /// Expected output: 200 OK with eligible orders only
    /// </summary>
    [Fact]
    public async Task GetList_WithReceivingEligible_ExcludesCancelledAndCompleted()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        await CreateProcurementDirectly(supplier.SupplierId, status: 2, "PO-002"); // cancelled
        await CreateProcurementDirectly(supplier.SupplierId, status: 3, "PO-003"); // completed

        // Act
        var result = await _controller.GetList(null, null, null, receivingEligible: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal("PO-001", response.Items[0].ContractNo);
    }

    /// <summary>
    /// Test case: GetList is paginated
    /// Expected output: 200 OK with correct page metadata
    /// </summary>
    [Fact]
    public async Task GetList_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        for (int i = 1; i <= 5; i++)
            await CreateProcurementDirectly(supplier.SupplierId, status: 0, $"PO-{i:D3}");

        // Act
        var result = await _controller.GetList(null, null, null, page: 2, pageSize: 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Equal(5, response.Total);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(3, response.TotalPages);
    }

    /// <summary>
    /// Test case: GetList returns empty list when no orders exist
    /// Expected output: 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetList_WithNoOrders_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetList(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PurchaseOrderListResponseDto>(okResult.Value);
        Assert.Empty(response.Items);
        Assert.Equal(0, response.Total);
    }

    #endregion

    #region GetById Tests

    /// <summary>
    /// Test case: GetById with valid id returns the purchase order with lines
    /// Expected output: 200 OK with PurchaseOrderDetailDto
    /// </summary>
    [Fact]
    public async Task GetById_WithValidId_ReturnsOrderWithLines()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        var procurement = await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        AddProcurementLine(procurement.ProcurementId, "Item A", 2, 1000000m);
        AddProcurementLine(procurement.ProcurementId, "Item B", 3, 500000m);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(procurement.ProcurementId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseOrderDetailDto>(okResult.Value);
        Assert.Equal("PO-001", dto.ContractNo);
        Assert.Equal(2, dto.Lines.Count);
        Assert.Equal(3500000m, dto.TotalAmount);
        Assert.Equal(2000000m, dto.Lines[0].LineTotal);
        Assert.Equal(0m, dto.Lines[0].ReceivedQuantity);
        Assert.Equal(2000000m, dto.Lines[0].OpenQuantity);
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
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Test case: Update with valid data updates lines and recalculates totals
    /// Expected output: 200 OK with updated procurement
    /// </summary>
    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedProcurement()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        var procurement = await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        AddProcurementLine(procurement.ProcurementId, "Old Item", 1, 100000m);
        await _context.SaveChangesAsync();

        var updateDto = new PurchaseOrderUpdateDto
        {
            SupplierId = supplier.SupplierId,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "New Item", Quantity = 5, UnitPrice = 200000m }
            }
        };

        // Act
        var result = await _controller.Update(procurement.ProcurementId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = await _context.Procurements
            .Include(p => p.Lines)
            .FirstAsync(p => p.ProcurementId == procurement.ProcurementId);

        Assert.Equal(1000000m, updated.TotalAmount);
        Assert.Equal(1000000m, updated.RemainingAmount);
        Assert.Single(updated.Lines);
        Assert.Equal("New Item", updated.Lines.First().Description);
    }

    /// <summary>
    /// Test case: Update a cancelled purchase order returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_CancelledOrder_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        var procurement = await CreateProcurementDirectly(supplier.SupplierId, status: 2, "PO-001"); // cancelled
        AddProcurementLine(procurement.ProcurementId, "Item", 1, 100000m);
        await _context.SaveChangesAsync();

        var updateDto = new PurchaseOrderUpdateDto
        {
            SupplierId = supplier.SupplierId,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 2, UnitPrice = 100000m }
            }
        };

        // Act
        var result = await _controller.Update(procurement.ProcurementId, updateDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Update with non-existent procurement id returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new PurchaseOrderUpdateDto
        {
            SupplierId = 1,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100m }
            }
        };

        // Act
        var result = await _controller.Update(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: Update with zero line quantity returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidLineQuantity_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        var procurement = await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        AddProcurementLine(procurement.ProcurementId, "Item", 1, 100000m);
        await _context.SaveChangesAsync();

        var updateDto = new PurchaseOrderUpdateDto
        {
            SupplierId = supplier.SupplierId,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 0, UnitPrice = 100000m }
            }
        };

        // Act
        var result = await _controller.Update(procurement.ProcurementId, updateDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Update with non-existent supplier returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidSupplier_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        var procurement = await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        AddProcurementLine(procurement.ProcurementId, "Item", 1, 100000m);
        await _context.SaveChangesAsync();

        var updateDto = new PurchaseOrderUpdateDto
        {
            SupplierId = 999,
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 100000m }
            }
        };

        // Act
        var result = await _controller.Update(procurement.ProcurementId, updateDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Cancel Tests

    /// <summary>
    /// Test case: Cancel a non-cancelled purchase order returns Ok with updated status
    /// Expected output: 200 OK with status=StatusCancelled
    /// </summary>
    [Fact]
    public async Task Cancel_WithValidOrder_ReturnsCancelledStatus()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        var procurement = await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        AddProcurementLine(procurement.ProcurementId, "Item", 1, 100000m);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Cancel(procurement.ProcurementId, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var saved = await _context.Procurements.FindAsync(procurement.ProcurementId);
        Assert.NotNull(saved);
        Assert.Equal(PurchaseOrdersController.StatusCancelled, saved.Status);
    }

    /// <summary>
    /// Test case: Cancel an already cancelled purchase order returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        var procurement = await CreateProcurementDirectly(supplier.SupplierId, status: 2, "PO-001");
        AddProcurementLine(procurement.ProcurementId, "Item", 1, 100000m);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Cancel(procurement.ProcurementId, null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Cancel a purchase order with goods receipts returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Cancel_WithGoodsReceipt_ReturnsBadRequest()
    {
        // Arrange
        var (supplier, _) = await SeedBaseDataAsync();
        var procurement = await CreateProcurementDirectly(supplier.SupplierId, status: 0, "PO-001");
        var line = AddProcurementLine(procurement.ProcurementId, "Item", 5, 100000m);
        line.ReceivedQuantity = 2;
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Cancel(procurement.ProcurementId, null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Cancel a non-existent purchase order returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Cancel_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.Cancel(999, null);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region Create Purchase Order Tests

    private PurchaseOrderCreateDto CreateValidPurchaseOrderDto(
        int supplierId = 1,
        string? contractNo = "PO-2025-TEST",
        string currency = "VND",
        int? assetRequestId = null)
    {
        return new PurchaseOrderCreateDto
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
    }

    /// <summary>
    /// Test case 1 (Normal): SupplierId = 1, ContractNo = Valid ContractNo, Currency = VND, AssetRequestId = 1.
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task CreatePurchaseOrder_ValidData_ReturnsCreated()
    {
        // Arrange
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 1,
            contractNo: "PO-2025-001",
            currency: "VND",
            assetRequestId: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>
    /// Test case 2 (Abnormal): SupplierId = 0, ContractNo = Valid ContractNo, Currency = VND, AssetRequestId = 1.
    /// Expected output: 400 Bad Request (Supplier is required)
    /// </summary>
    [Fact]
    public async Task CreatePurchaseOrder_SupplierIdZero_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 0,
            contractNo: "PO-2025-002",
            currency: "VND",
            assetRequestId: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): SupplierId = -1, ContractNo = Valid ContractNo, Currency = VND, AssetRequestId = 1.
    /// Expected output: 400 Bad Request (Supplier is required)
    /// </summary>
    [Fact]
    public async Task CreatePurchaseOrder_SupplierIdNegative_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidPurchaseOrderDto(
            supplierId: -1,
            contractNo: "PO-2025-003",
            currency: "VND",
            assetRequestId: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): SupplierId = 1, ContractNo = Empty, Currency = VND, AssetRequestId = 1.
    /// Expected output: 400 Bad Request (Số chứng từ là bắt buộc khi tạo đơn)
    /// Note: Empty ContractNo is allowed for draft orders (IsDraft = true)
    /// </summary>
    [Fact]
    public async Task CreatePurchaseOrder_EmptyContractNo_ReturnsBadRequest()
    {
        // Arrange
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 1,
            contractNo: "",
            currency: "VND",
            assetRequestId: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case 5 (Normal): SupplierId = 1, ContractNo = Valid ContractNo, Currency = USD, AssetRequestId = 1.
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task CreatePurchaseOrder_ValidCurrencyUSD_ReturnsCreated()
    {
        // Arrange
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 1,
            contractNo: "PO-2025-005",
            currency: "USD",
            assetRequestId: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>
    /// Test case 6 (Normal): SupplierId = 1, ContractNo = Valid ContractNo, Currency = EUR, AssetRequestId = 1.
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task CreatePurchaseOrder_ValidCurrencyEUR_ReturnsCreated()
    {
        // Arrange
        var dto = CreateValidPurchaseOrderDto(
            supplierId: 1,
            contractNo: "PO-2025-006",
            currency: "EUR",
            assetRequestId: 1);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): SupplierId = 1, ContractNo = Valid ContractNo, Currency = VND, AssetRequestId = 0.
    /// Expected output: 201 Created (AssetRequestId = 0 is treated as null/not provided)
    /// Note: When AssetRequestId = 0, it's considered as no linked request, which is valid
    /// </summary>
    [Fact]
    public async Task CreatePurchaseOrder_AssetRequestIdZero_ReturnsCreated()
    {
        // Arrange
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            ContractNo = "PO-2025-007",
            Currency = "VND",
            AssetRequestId = 0, // Treated as null
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Test Item", Quantity = 5, Unit = "pcs", UnitPrice = 1000000m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): SupplierId = 1, ContractNo = Valid ContractNo, Currency = VND, AssetRequestId = -1.
    /// Expected output: 400 Bad Request (Linked requisition not found)
    /// Note: AssetRequestId = -1 is treated as -1, which doesn't exist
    /// </summary>
    [Fact]
    public async Task CreatePurchaseOrder_AssetRequestIdNegative_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = 1,
            ContractNo = "PO-2025-008",
            Currency = "VND",
            AssetRequestId = -1, // Invalid - doesn't exist
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { Description = "Test Item", Quantity = 5, Unit = "pcs", UnitPrice = 1000000m }
            }
        };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Private Helper Methods for Test Setup

    private async Task<Procurement> CreateProcurementDirectly(
        int supplierId,
        int status,
        string contractNo,
        int? assetRequestId = null)
    {
        var procurement = new Procurement
        {
            ProcurementId = (_context.Procurements.Any() ? _context.Procurements.Max(p => p.ProcurementId) : 0) + 1,
            SupplierId = supplierId,
            AssetRequestId = assetRequestId,
            ContractNo = contractNo,
            ContractDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Title = contractNo,
            Currency = "VND",
            TotalAmount = 100000m,
            AdvanceAmount = 0,
            RemainingAmount = 100000m,
            Status = status,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow
        };
        _context.Procurements.Add(procurement);
        await _context.SaveChangesAsync();
        return procurement;
    }

    private ProcurementLine AddProcurementLine(int procurementId, string description, decimal quantity, decimal unitPrice)
    {
        var line = new ProcurementLine
        {
            ProcurementId = procurementId,
            LineIndex = _context.ProcurementLines.Count(l => l.ProcurementId == procurementId),
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            ReceivedQuantity = 0
        };
        _context.ProcurementLines.Add(line);
        return line;
    }

    #endregion
}
