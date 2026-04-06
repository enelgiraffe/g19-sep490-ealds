using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class SuppliersControllerTests : IDisposable
{
    private readonly EaldsDbContext _context;
    private readonly SuppliersController _controller;

    public SuppliersControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new SuppliersController(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetSuppliers Tests

    /// <summary>
    /// Test: GET /api/Suppliers returns all suppliers
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; containing all suppliers
    /// </summary>
    [Fact]
    public async Task GetSuppliers_ReturnsAllSuppliers()
    {
        // Arrange
        _context.Suppliers.AddRange(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", Status = 1, CreateDate = DateTime.UtcNow },
            new Supplier { SupplierId = 2, Code = "SUP002", Name = "Office Supplies Co", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Equal(2, supplierList.Count);
        Assert.Contains(supplierList, s => s.Code == "SUP001" && s.Name == "Tech Corp");
        Assert.Contains(supplierList, s => s.Code == "SUP002" && s.Name == "Office Supplies Co");
    }

    /// <summary>
    /// Test: GET /api/Suppliers returns empty list when no suppliers exist
    /// Expected: 200 OK with empty IEnumerable&lt;SupplierDTO&gt;
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithNoSuppliers_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetSuppliers(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        Assert.Empty(suppliers);
    }

    /// <summary>
    /// Test: GET /api/Suppliers?keyword=tech searches by name (case insensitive)
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; containing only matching suppliers
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithKeywordSearchByName_ReturnsMatchingSuppliers()
    {
        // Arrange
        _context.Suppliers.AddRange(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", Status = 1, CreateDate = DateTime.UtcNow },
            new Supplier { SupplierId = 2, Code = "SUP002", Name = "Office Supplies Co", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers("tech");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Single(supplierList);
        Assert.Equal("Tech Corp", supplierList[0].Name);
    }

    /// <summary>
    /// Test: GET /api/Suppliers?keyword=TECH searches by name (all uppercase)
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; containing matching suppliers (case insensitive)
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithUppercaseKeyword_ReturnsMatchingSuppliers()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers("TECH");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Single(supplierList);
        Assert.Equal("Tech Corp", supplierList[0].Name);
    }

    /// <summary>
    /// Test: GET /api/Suppliers?keyword=SU001 searches by code
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; containing supplier with matching code
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithKeywordSearchByCode_ReturnsMatchingSuppliers()
    {
        // Arrange
        _context.Suppliers.AddRange(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", Status = 1, CreateDate = DateTime.UtcNow },
            new Supplier { SupplierId = 2, Code = "SUP002", Name = "Office Supplies Co", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers("SU001");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Single(supplierList);
        Assert.Equal("SUP001", supplierList[0].Code);
    }

    /// <summary>
    /// Test: GET /api/Suppliers?keyword=1234567890 searches by tax code
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; containing supplier with matching tax code
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithKeywordSearchByTaxCode_ReturnsMatchingSuppliers()
    {
        // Arrange
        _context.Suppliers.AddRange(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", TaxCode = "1234567890", Status = 1, CreateDate = DateTime.UtcNow },
            new Supplier { SupplierId = 2, Code = "SUP002", Name = "Office Supplies Co", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers("1234567890");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Single(supplierList);
        Assert.Equal("1234567890", supplierList[0].TaxCode);
    }

    /// <summary>
    /// Test: GET /api/Suppliers?keyword=0912345678 searches by phone
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; containing supplier with matching phone
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithKeywordSearchByPhone_ReturnsMatchingSuppliers()
    {
        // Arrange
        _context.Suppliers.AddRange(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", Phone = "0912345678", Status = 1, CreateDate = DateTime.UtcNow },
            new Supplier { SupplierId = 2, Code = "SUP002", Name = "Office Supplies Co", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers("0912345678");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Single(supplierList);
        Assert.Equal("0912345678", supplierList[0].Phone);
    }

    /// <summary>
    /// Test: GET /api/Suppliers?keyword=contact searches by email
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; containing supplier with matching email
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithKeywordSearchByEmail_ReturnsMatchingSuppliers()
    {
        // Arrange
        _context.Suppliers.AddRange(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", Email = "contact@techcorp.com", Status = 1, CreateDate = DateTime.UtcNow },
            new Supplier { SupplierId = 2, Code = "SUP002", Name = "Office Supplies Co", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers("contact");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Single(supplierList);
        Assert.Equal("contact@techcorp.com", supplierList[0].Email);
    }

    /// <summary>
    /// Test: GET /api/Suppliers?keyword=NonExistent returns empty list
    /// Expected: 200 OK with empty IEnumerable&lt;SupplierDTO&gt;
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithNonExistentKeyword_ReturnsEmptyList()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers("NonExistentKeyword");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        Assert.Empty(suppliers);
    }

    /// <summary>
    /// Test: GET /api/Suppliers?keyword=  with leading/trailing spaces trims and searches
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; matching suppliers after trim
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithKeywordAndSpaces_TrimsAndSearches()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Tech Corp", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers("  Tech  ");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Single(supplierList);
        Assert.Equal("Tech Corp", supplierList[0].Name);
    }

    /// <summary>
    /// Test: GET /api/Suppliers returns suppliers with all nullable fields
    /// Expected: 200 OK with IEnumerable&lt;SupplierDTO&gt; where nullable fields are preserved
    /// </summary>
    [Fact]
    public async Task GetSuppliers_WithNullableFields_ReturnsNullableFields()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier
            {
                SupplierId = 1,
                Code = "SUP001",
                Name = "Minimal Supplier",
                TaxCode = null,
                Address = null,
                Phone = null,
                Email = null,
                Status = 1,
                CreateDate = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSuppliers(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suppliers = Assert.IsAssignableFrom<IEnumerable<SupplierDTO>>(okResult.Value);
        var supplierList = suppliers.ToList();

        Assert.Single(supplierList);
        Assert.Equal("Minimal Supplier", supplierList[0].Name);
        Assert.Null(supplierList[0].TaxCode);
        Assert.Null(supplierList[0].Address);
        Assert.Null(supplierList[0].Phone);
        Assert.Null(supplierList[0].Email);
    }

    #endregion

    #region GetSupplier Tests

    /// <summary>
    /// Test: GET /api/Suppliers/{id} with valid ID returns supplier details
    /// Expected: 200 OK with SupplierDTO containing all fields
    /// </summary>
    [Fact]
    public async Task GetSupplier_WithValidId_ReturnsSupplierDetails()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier
            {
                SupplierId = 1,
                Code = "SUP001",
                Name = "Tech Corp",
                TaxCode = "1234567890",
                Address = "123 Main St",
                Phone = "0912345678",
                Email = "contact@techcorp.com",
                Status = 1,
                CreateDate = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSupplier(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var supplier = Assert.IsType<SupplierDTO>(okResult.Value);
        Assert.Equal(1, supplier.SupplierId);
        Assert.Equal("SUP001", supplier.Code);
        Assert.Equal("Tech Corp", supplier.Name);
        Assert.Equal("1234567890", supplier.TaxCode);
        Assert.Equal("123 Main St", supplier.Address);
        Assert.Equal("0912345678", supplier.Phone);
        Assert.Equal("contact@techcorp.com", supplier.Email);
        Assert.Equal(1, supplier.Status);
    }

    /// <summary>
    /// Test: GET /api/Suppliers/{id} with invalid ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetSupplier_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetSupplier(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Test: GET /api/Suppliers/{id} with negative ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetSupplier_WithNegativeId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetSupplier(-1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Test: GET /api/Suppliers/{id} with zero ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetSupplier_WithZeroId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetSupplier(0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region CreateSupplier Tests

    /// <summary>
    /// Test: POST /api/Suppliers with valid data creates supplier
    /// Expected: 201 Created with SupplierDTO containing all fields
    /// </summary>
    [Fact]
    public async Task CreateSupplier_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "Tech Corp",
            TaxCode = "1234567890",
            Address = "123 Main St",
            Phone = "0912345678",
            Email = "contact@techcorp.com",
            Status = 1
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("GetSupplier", createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.Equal(1, createdResult.RouteValues["id"]);

        var supplier = Assert.IsType<SupplierDTO>(createdResult.Value);
        Assert.True(supplier.SupplierId > 0);
        Assert.Equal("SUP001", supplier.Code);
        Assert.Equal("Tech Corp", supplier.Name);
        Assert.Equal("1234567890", supplier.TaxCode);
        Assert.Equal("123 Main St", supplier.Address);
        Assert.Equal("0912345678", supplier.Phone);
        Assert.Equal("contact@techcorp.com", supplier.Email);
        Assert.Equal(1, supplier.Status);

        // Verify database
        var savedSupplier = await _context.Suppliers.FindAsync(supplier.SupplierId);
        Assert.NotNull(savedSupplier);
        Assert.Equal("Tech Corp", savedSupplier.Name);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with duplicate code returns 400
    /// Expected: 400 Bad Request with error message about duplicate code
    /// </summary>
    [Fact]
    public async Task CreateSupplier_WithDuplicateCode_ReturnsBadRequest()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Existing Supplier", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "New Supplier",
            Status = 1
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with Status=0 creates inactive supplier
    /// Expected: 201 Created with SupplierDTO where Status=0
    /// </summary>
    [Fact]
    public async Task CreateSupplier_WithInactiveStatus_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "Inactive Supplier",
            Status = 0
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var supplier = Assert.IsType<SupplierDTO>(createdResult.Value);
        Assert.Equal(0, supplier.Status);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with 10-digit TaxCode (valid)
    /// Expected: 201 Created
    /// </summary>
    [Fact]
    public async Task CreateSupplier_With10DigitTaxCode_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "Supplier With 10-Digit Tax",
            TaxCode = "1234567890",
            Status = 1
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var supplier = Assert.IsType<SupplierDTO>(createdResult.Value);
        Assert.Equal("1234567890", supplier.TaxCode);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with 13-digit TaxCode (valid)
    /// Expected: 201 Created
    /// </summary>
    [Fact]
    public async Task CreateSupplier_With13DigitTaxCode_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "Supplier With 13-Digit Tax",
            TaxCode = "1234567890123",
            Status = 1
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var supplier = Assert.IsType<SupplierDTO>(createdResult.Value);
        Assert.Equal("1234567890123", supplier.TaxCode);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with valid Vietnamese phone number (0-prefixed)
    /// Expected: 201 Created
    /// </summary>
    [Fact]
    public async Task CreateSupplier_WithValidPhone0Prefix_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "Supplier With Valid Phone",
            Phone = "0912345678",
            Status = 1
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with valid Vietnamese phone number (+84 prefix)
    /// Expected: 201 Created
    /// </summary>
    [Fact]
    public async Task CreateSupplier_WithValidPhone84Prefix_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "Supplier With +84 Phone",
            Phone = "+84912345678",
            Status = 1
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with valid email format
    /// Expected: 201 Created
    /// </summary>
    [Fact]
    public async Task CreateSupplier_WithValidEmail_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "Supplier With Valid Email",
            Email = "contact@supplier.com",
            Status = 1
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var supplier = Assert.IsType<SupplierDTO>(createdResult.Value);
        Assert.Equal("contact@supplier.com", supplier.Email);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with no optional fields creates supplier with nulls
    /// Expected: 201 Created with SupplierDTO where optional fields are null
    /// </summary>
    [Fact]
    public async Task CreateSupplier_WithNoOptionalFields_ReturnsCreatedWithNulls()
    {
        // Arrange
        var dto = new CreateSupplierDTO
        {
            Code = "SUP001",
            Name = "Minimal Supplier",
            Status = 1
        };

        // Act
        var result = await _controller.CreateSupplier(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var supplier = Assert.IsType<SupplierDTO>(createdResult.Value);
        Assert.Null(supplier.TaxCode);
        Assert.Null(supplier.Address);
        Assert.Null(supplier.Phone);
        Assert.Null(supplier.Email);
    }

    /// <summary>
    /// Test: POST /api/Suppliers with multiple valid suppliers creates all
    /// Expected: 201 Created for each supplier with unique IDs
    /// </summary>
    [Fact]
    public async Task CreateSupplier_MultipleSuppliers_ReturnsCreatedWithUniqueIds()
    {
        // Arrange
        var dto1 = new CreateSupplierDTO { Code = "SUP001", Name = "Supplier One", Status = 1 };
        var dto2 = new CreateSupplierDTO { Code = "SUP002", Name = "Supplier Two", Status = 1 };
        var dto3 = new CreateSupplierDTO { Code = "SUP003", Name = "Supplier Three", Status = 1 };

        // Act
        var result1 = await _controller.CreateSupplier(dto1);
        var result2 = await _controller.CreateSupplier(dto2);
        var result3 = await _controller.CreateSupplier(dto3);

        // Assert
        var createdResult1 = Assert.IsType<CreatedAtActionResult>(result1);
        var createdResult2 = Assert.IsType<CreatedAtActionResult>(result2);
        var createdResult3 = Assert.IsType<CreatedAtActionResult>(result3);

        var supplier1 = Assert.IsType<SupplierDTO>(createdResult1.Value);
        var supplier2 = Assert.IsType<SupplierDTO>(createdResult2.Value);
        var supplier3 = Assert.IsType<SupplierDTO>(createdResult3.Value);

        Assert.NotEqual(supplier1.SupplierId, supplier2.SupplierId);
        Assert.NotEqual(supplier2.SupplierId, supplier3.SupplierId);

        // Verify database
        var allSuppliers = await _context.Suppliers.ToListAsync();
        Assert.Equal(3, allSuppliers.Count);
    }

    #endregion

    #region UpdateSupplier Tests

    /// <summary>
    /// Test: PUT /api/Suppliers/{id} with valid data updates supplier
    /// Expected: 204 No Content
    /// </summary>
    [Fact]
    public async Task UpdateSupplier_WithValidData_ReturnsNoContent()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier
            {
                SupplierId = 1,
                Code = "SUP001",
                Name = "Old Name",
                Status = 1,
                CreateDate = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var dto = new UpdateSupplierDTO
        {
            Code = "SUP001",
            Name = "Updated Supplier",
            TaxCode = "9876543210",
            Address = "456 New St",
            Phone = "0998765432",
            Email = "updated@supplier.com",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateSupplier(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify database
        var updatedSupplier = await _context.Suppliers.FindAsync(1);
        Assert.NotNull(updatedSupplier);
        Assert.Equal("Updated Supplier", updatedSupplier.Name);
        Assert.Equal("9876543210", updatedSupplier.TaxCode);
        Assert.Equal("456 New St", updatedSupplier.Address);
        Assert.Equal("0998765432", updatedSupplier.Phone);
        Assert.Equal("updated@supplier.com", updatedSupplier.Email);
    }

    /// <summary>
    /// Test: PUT /api/Suppliers/{id} with invalid ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task UpdateSupplier_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateSupplierDTO
        {
            Code = "SUP001",
            Name = "Updated Supplier",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateSupplier(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test: PUT /api/Suppliers/{id} with duplicate code (different supplier) returns 400
    /// Expected: 400 Bad Request with error message about duplicate code
    /// </summary>
    [Fact]
    public async Task UpdateSupplier_WithDuplicateCode_ReturnsBadRequest()
    {
        // Arrange
        _context.Suppliers.AddRange(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Supplier One", Status = 1, CreateDate = DateTime.UtcNow },
            new Supplier { SupplierId = 2, Code = "SUP002", Name = "Supplier Two", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var dto = new UpdateSupplierDTO
        {
            Code = "SUP001", // Trying to use Supplier Two's code as SUP001
            Name = "Supplier Two",
            Status = 1
        };

        // Act - Try to update Supplier Two with code SUP001 (which belongs to Supplier One)
        var result = await _controller.UpdateSupplier(2, dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test: PUT /api/Suppliers/{id} keeping the same code succeeds
    /// Expected: 204 No Content
    /// </summary>
    [Fact]
    public async Task UpdateSupplier_WithSameCode_ReturnsNoContent()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Supplier", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var dto = new UpdateSupplierDTO
        {
            Code = "SUP001", // Same code
            Name = "Updated Name",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateSupplier(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Test: PUT /api/Suppliers/{id} updates status from active to inactive
    /// Expected: 204 No Content and Status=0 in database
    /// </summary>
    [Fact]
    public async Task UpdateSupplier_ChangesStatusToInactive_ReturnsNoContent()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Active Supplier", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var dto = new UpdateSupplierDTO
        {
            Code = "SUP001",
            Name = "Active Supplier",
            Status = 0
        };

        // Act
        var result = await _controller.UpdateSupplier(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updatedSupplier = await _context.Suppliers.FindAsync(1);
        Assert.NotNull(updatedSupplier);
        Assert.Equal(0, updatedSupplier.Status);
    }

    /// <summary>
    /// Test: PUT /api/Suppliers/{id} updates tax code
    /// Expected: 204 No Content and TaxCode updated in database
    /// </summary>
    [Fact]
    public async Task UpdateSupplier_UpdatesTaxCode_ReturnsNoContent()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "Supplier", TaxCode = "1234567890", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var dto = new UpdateSupplierDTO
        {
            Code = "SUP001",
            Name = "Supplier",
            TaxCode = "9876543210987",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateSupplier(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updatedSupplier = await _context.Suppliers.FindAsync(1);
        Assert.NotNull(updatedSupplier);
        Assert.Equal("9876543210987", updatedSupplier.TaxCode);
    }

    /// <summary>
    /// Test: PUT /api/Suppliers/{id} with negative ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task UpdateSupplier_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateSupplierDTO
        {
            Code = "SUP001",
            Name = "Supplier",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateSupplier(-1, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region DeleteSupplier Tests

    /// <summary>
    /// Test: DELETE /api/Suppliers/{id} with valid ID (no references) permanently deletes
    /// Expected: 204 No Content and supplier removed from database
    /// </summary>
    [Fact]
    public async Task DeleteSupplier_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _context.Suppliers.Add(
            new Supplier { SupplierId = 1, Code = "SUP001", Name = "To Delete", Status = 1, CreateDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteSupplier(1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedSupplier = await _context.Suppliers.FindAsync(1);
        Assert.Null(deletedSupplier);
    }

    /// <summary>
    /// Test: DELETE /api/Suppliers/{id} with invalid ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task DeleteSupplier_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteSupplier(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test: DELETE /api/Suppliers/{id} with negative ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task DeleteSupplier_WithNegativeId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteSupplier(-1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test: DELETE /api/Suppliers/{id} with referenced Procurements soft-deletes (status=0)
    /// Expected: 200 OK with message about soft delete, Status=0 in database
    /// </summary>
    [Fact]
    public async Task DeleteSupplier_WithProcurementReferences_SoftDeletes()
    {
        // Arrange
        var supplier = new Supplier { SupplierId = 1, Code = "SUP001", Name = "Referenced Supplier", Status = 1, CreateDate = DateTime.UtcNow };
        var procurement = new Procurement { ProcurementId = 1, SupplierId = 1, Status = 1 };

        _context.Suppliers.Add(supplier);
        _context.Procurements.Add(procurement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteSupplier(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Supplier should still exist with Status=0
        var softDeletedSupplier = await _context.Suppliers.FindAsync(1);
        Assert.NotNull(softDeletedSupplier);
        Assert.Equal(0, softDeletedSupplier.Status);
    }

    /// <summary>
    /// Test: DELETE /api/Suppliers/{id} with referenced RepairRecords soft-deletes (status=0)
    /// Expected: 200 OK with message about soft delete, Status=0 in database
    /// </summary>
    [Fact]
    public async Task DeleteSupplier_WithRepairRecordReferences_SoftDeletes()
    {
        // Arrange
        var supplier = new Supplier { SupplierId = 1, Code = "SUP001", Name = "Referenced Supplier", Status = 1, CreateDate = DateTime.UtcNow };
        var repairRecord = new RepairRecord { RepairRecordId = 1, SupplierId = 1, Status = 1 };

        _context.Suppliers.Add(supplier);
        _context.RepairRecords.Add(repairRecord);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteSupplier(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Supplier should still exist with Status=0
        var softDeletedSupplier = await _context.Suppliers.FindAsync(1);
        Assert.NotNull(softDeletedSupplier);
        Assert.Equal(0, softDeletedSupplier.Status);
    }

    /// <summary>
    /// Test: DELETE /api/Suppliers/{id} with both Procurement and RepairRecord references soft-deletes
    /// Expected: 200 OK with Status=0
    /// </summary>
    [Fact]
    public async Task DeleteSupplier_WithBothReferences_SoftDeletes()
    {
        // Arrange
        var supplier = new Supplier { SupplierId = 1, Code = "SUP001", Name = "Referenced Supplier", Status = 1, CreateDate = DateTime.UtcNow };
        var procurement = new Procurement { ProcurementId = 1, SupplierId = 1, Status = 1 };
        var repairRecord = new RepairRecord { RepairRecordId = 1, SupplierId = 1, Status = 1 };

        _context.Suppliers.Add(supplier);
        _context.Procurements.Add(procurement);
        _context.RepairRecords.Add(repairRecord);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteSupplier(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var softDeletedSupplier = await _context.Suppliers.FindAsync(1);
        Assert.NotNull(softDeletedSupplier);
        Assert.Equal(0, softDeletedSupplier.Status);
    }

    #endregion
}
