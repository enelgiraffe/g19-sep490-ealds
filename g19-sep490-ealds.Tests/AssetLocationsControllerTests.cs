using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.AssetLocation;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AssetLocationsControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly AssetLocationsController _controller;

    public AssetLocationsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new AssetLocationsController(_context);
    }

    #region GetDepartments Tests

    /// <summary>
    /// Test case: GetDepartments returns all departments ordered by id
    /// Expected output: 200 OK with department list
    /// </summary>
    [Fact]
    public async Task GetDepartments_ReturnsAllDepartments()
    {
        // Arrange
        var dept1 = new Department { DepartmentId = 1, Name = "IT Department", Code = "IT", Status = 1, CreateDate = DateTime.UtcNow, CreatedBy = 1 };
        var dept2 = new Department { DepartmentId = 2, Name = "HR Department", Code = "HR", Status = 1, CreateDate = DateTime.UtcNow, CreatedBy = 1 };

        _context.Departments.AddRange(dept1, dept2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetDepartments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: GetDepartments returns empty list when no departments exist
    /// Expected output: 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetDepartments_WithNoDepartments_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetDepartments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetEmployeesForDepartment Tests

    /// <summary>
    /// Test case: GetEmployeesForDepartment returns employees for a valid department
    /// Expected output: 200 OK with employee list
    /// </summary>
    [Fact]
    public async Task GetEmployeesForDepartment_WithValidDepartment_ReturnsEmployees()
    {
        // Arrange
        var dept = new Department { DepartmentId = 1, Name = "IT Department", Code = "IT", Status = 1, CreateDate = DateTime.UtcNow, CreatedBy = 1 };
        var emp1 = new Employee { EmployeeId = 1, DepartmentId = 1, Name = "Nguyen Van A", Code = "EMP001", UserId = 10, Status = 1, CreateDate = DateTime.UtcNow, CreatedBy = 1 };
        var emp2 = new Employee { EmployeeId = 2, DepartmentId = 1, Name = "Tran Van B", Code = "EMP002", UserId = null, Status = 1, CreateDate = DateTime.UtcNow, CreatedBy = 1 };

        _context.Departments.Add(dept);
        _context.Employees.AddRange(emp1, emp2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEmployeesForDepartment(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: GetEmployeesForDepartment returns NotFound for non-existent department
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetEmployeesForDepartment_WithInvalidDepartment_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetEmployeesForDepartment(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetAll Tests

    /// <summary>
    /// Test case: GetAll without filters returns all location records
    /// Expected output: 200 OK with all records ordered by IsCurrent desc, StartDate desc
    /// </summary>
    [Fact]
    public async Task GetAll_WithoutFilters_ReturnsAllLocationRecords()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        // Act
        var result = await _controller.GetAll(null, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsAssignableFrom<IEnumerable<AssetLocationResponseDto>>(okResult.Value);
        Assert.Equal(2, locations.Count());
    }

    /// <summary>
    /// Test case: GetAll with assetInstanceId filter returns matching records
    /// Expected output: 200 OK with filtered records
    /// </summary>
    [Fact]
    public async Task GetAll_WithAssetInstanceIdFilter_ReturnsMatchingRecords()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        // Act
        var result = await _controller.GetAll(instance.AssetInstanceId, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsAssignableFrom<IEnumerable<AssetLocationResponseDto>>(okResult.Value);
        Assert.Equal(2, locations.Count());
    }

    /// <summary>
    /// Test case: GetAll with departmentId filter returns matching records
    /// Expected output: 200 OK with filtered records
    /// </summary>
    [Fact]
    public async Task GetAll_WithDepartmentIdFilter_ReturnsMatchingRecords()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        // Act
        var result = await _controller.GetAll(null, null, 1, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsAssignableFrom<IEnumerable<AssetLocationResponseDto>>(okResult.Value);
        Assert.Single(locations);
        Assert.Equal(1, locations.First().DepartmentId);
    }

    /// <summary>
    /// Test case: GetAll with isCurrent filter returns matching records
    /// Expected output: 200 OK with current location record
    /// </summary>
    [Fact]
    public async Task GetAll_WithIsCurrentFilter_ReturnsCurrentRecords()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        // Act
        var result = await _controller.GetAll(null, null, null, true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsAssignableFrom<IEnumerable<AssetLocationResponseDto>>(okResult.Value);
        Assert.Single(locations);
        Assert.True(locations.First().IsCurrent);
    }

    /// <summary>
    /// Test case: GetAll returns empty list when no records exist
    /// Expected output: 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetAll_WithNoRecords_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetAll(null, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var locations = Assert.IsAssignableFrom<IEnumerable<AssetLocationResponseDto>>(okResult.Value);
        Assert.Empty(locations);
    }

    #endregion

    #region GetById Tests

    /// <summary>
    /// Test case: GetById with valid id returns the location record
    /// Expected output: 200 OK with location data
    /// </summary>
    [Fact]
    public async Task GetById_WithValidId_ReturnsLocationRecord()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        // Act
        var result = await _controller.GetById(location1.LocationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var location = Assert.IsType<AssetLocationResponseDto>(okResult.Value);
        Assert.Equal(location1.LocationId, location.LocationId);
        Assert.Equal(instance.InstanceCode, location.InstanceCode);
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

    #region Create Tests

    /// <summary>
    /// Test case: Create with valid data creates a new location record and returns Created
    /// Expected output: 201 Created with response DTO
    /// </summary>
    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        var request = new CreateAssetLocationDto
        {
            AssetInstanceId = instance.AssetInstanceId,
            DepartmentId = 2,
            StartDate = new DateOnly(2025, 7, 1),
            IsCurrent = false,
            Note = "Moved to HR floor"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var location = Assert.IsType<AssetLocationResponseDto>(createdResult.Value);
        Assert.Equal(instance.AssetInstanceId, location.AssetInstanceId);
        Assert.Equal(2, location.DepartmentId);
        Assert.Equal("Moved to HR floor", location.Note);
        Assert.False(location.IsCurrent);
    }

    /// <summary>
    /// Test case: Create with non-existent asset instance returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidAssetInstance_ReturnsNotFound()
    {
        // Arrange
        var dept = new Department { DepartmentId = 1, Name = "IT Department", Code = "IT", Status = 1, CreateDate = DateTime.UtcNow, CreatedBy = 1 };
        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        var request = new CreateAssetLocationDto
        {
            AssetInstanceId = 999,
            DepartmentId = 1,
            StartDate = new DateOnly(2025, 1, 1),
            IsCurrent = false
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create with non-existent department returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidDepartment_ReturnsNotFound()
    {
        // Arrange
        var assetType = new AssetType { AssetTypeId = 1, CategoryId = 1, Name = "Computer" };
        var warehouse = new WarehouseAsset { WarehouseId = 1, Name = "Main Warehouse" };
        var asset = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            CreatedBy = 1
        };
        var instance = new AssetInstance
        {
            AssetInstanceId = 1,
            AssetId = 1,
            InstanceCode = "INST001",
            WarehouseId = 1,
            Status = 1
        };
        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.Add(asset);
        _context.AssetInstances.Add(instance);
        await _context.SaveChangesAsync();

        var request = new CreateAssetLocationDto
        {
            AssetInstanceId = 1,
            DepartmentId = 999,
            StartDate = new DateOnly(2025, 1, 1),
            IsCurrent = false
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create with EndDate earlier than StartDate returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidEndDate_ReturnsBadRequest()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        var request = new CreateAssetLocationDto
        {
            AssetInstanceId = instance.AssetInstanceId,
            DepartmentId = 1,
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 1, 1),
            IsCurrent = false
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create with IsCurrent=true auto-closes the previous current location
    /// Expected output: Previous current record is closed (IsCurrent=false, EndDate set)
    /// </summary>
    [Fact]
    public async Task Create_WithIsCurrentTrue_AutoClosesPreviousCurrentLocation()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        var request = new CreateAssetLocationDto
        {
            AssetInstanceId = instance.AssetInstanceId,
            DepartmentId = 2,
            StartDate = new DateOnly(2025, 7, 1),
            IsCurrent = true
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var newLocation = Assert.IsType<AssetLocationResponseDto>(createdResult.Value);
        Assert.True(newLocation.IsCurrent);

        var closedLocation = await _context.AssetLocations.FindAsync(location1.LocationId);
        Assert.NotNull(closedLocation);
        Assert.False(closedLocation.IsCurrent);
        Assert.Equal(new DateOnly(2025, 6, 30), closedLocation.EndDate);
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Test case: Update with valid data updates the location record
    /// Expected output: 200 OK with updated data
    /// </summary>
    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedLocation()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        var updateRequest = new UpdateAssetLocationDto
        {
            DepartmentId = 2,
            StartDate = new DateOnly(2025, 3, 1),
            IsCurrent = false,
            Note = "Updated note"
        };

        // Act
        var result = await _controller.Update(location1.LocationId, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AssetLocationResponseDto>(okResult.Value);
        Assert.Equal(2, updated.DepartmentId);
        Assert.Equal("Updated note", updated.Note);
    }

    /// <summary>
    /// Test case: Update with non-existent location id returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = new UpdateAssetLocationDto
        {
            DepartmentId = 1,
            StartDate = new DateOnly(2025, 1, 1),
            IsCurrent = false
        };

        // Act
        var result = await _controller.Update(999, updateRequest);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Update with invalid department id returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidDepartment_ReturnsNotFound()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        var updateRequest = new UpdateAssetLocationDto
        {
            DepartmentId = 999,
            StartDate = new DateOnly(2025, 1, 1),
            IsCurrent = false
        };

        // Act
        var result = await _controller.Update(location1.LocationId, updateRequest);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Update with EndDate earlier than StartDate returns BadRequest
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidEndDate_ReturnsBadRequest()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        var updateRequest = new UpdateAssetLocationDto
        {
            DepartmentId = 1,
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 1, 1),
            IsCurrent = false
        };

        // Act
        var result = await _controller.Update(location1.LocationId, updateRequest);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Update with IsCurrent changing from false to true auto-closes the previous current
    /// Expected output: Previous current record is closed
    /// </summary>
    [Fact]
    public async Task Update_IsCurrentTrue_AutoClosesPreviousCurrentLocation()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        var updateRequest = new UpdateAssetLocationDto
        {
            DepartmentId = 2,
            StartDate = new DateOnly(2025, 4, 1),
            IsCurrent = true
        };

        // Act
        var result = await _controller.Update(location2.LocationId, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AssetLocationResponseDto>(okResult.Value);
        Assert.True(updated.IsCurrent);

        var closedLocation = await _context.AssetLocations.FindAsync(location1.LocationId);
        Assert.NotNull(closedLocation);
        Assert.False(closedLocation.IsCurrent);
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Test case: Delete with valid id removes the location record
    /// Expected output: 204 No Content
    /// </summary>
    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();
        var idToDelete = location2.LocationId;

        // Act
        var result = await _controller.Delete(idToDelete);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var deleted = await _context.AssetLocations.FindAsync(idToDelete);
        Assert.Null(deleted);
    }

    /// <summary>
    /// Test case: Delete with non-existent id returns NotFound
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: Delete a location record referenced by InventoryRecord returns Conflict
    /// Expected output: 409 Conflict
    /// </summary>
    [Fact]
    public async Task Delete_WithInventoryRecordReference_ReturnsConflict()
    {
        // Arrange
        var (asset, instance, location1, location2) = await SeedLocationData();

        var inventoryRecord = new InventoryRecord
        {
            TaskId = 1,
            ActualLocationId = location1.LocationId,
            ActualCondition = "Good",
            CheckedBy = 1,
            CheckedDate = DateTime.UtcNow,
            DateCheckCompleted = DateTime.UtcNow
        };
        _context.InventoryRecords.Add(inventoryRecord);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(location1.LocationId);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
        var stillExists = await _context.AssetLocations.FindAsync(location1.LocationId);
        Assert.NotNull(stillExists);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Seeds a minimal test database with: Asset, AssetInstance, Department, Employee,
    /// and two AssetLocation records (one IsCurrent=true, one IsCurrent=false).
    /// Returns the seeded entities in order: asset, instance, location1 (current), location2.
    /// </summary>
    private async Task<(Asset asset, AssetInstance instance, AssetLocation location1, AssetLocation location2)> SeedLocationData()
    {
        var dept1 = new Department { DepartmentId = 1, Name = "IT Department", Code = "IT", Status = 1, CreateDate = DateTime.UtcNow, CreatedBy = 1 };
        var dept2 = new Department { DepartmentId = 2, Name = "HR Department", Code = "HR", Status = 1, CreateDate = DateTime.UtcNow, CreatedBy = 1 };

        var assetType = new AssetType { AssetTypeId = 1, CategoryId = 1, Name = "Computer" };
        var warehouse = new WarehouseAsset { WarehouseId = 1, Name = "Main Warehouse" };

        var asset = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            CreatedBy = 1
        };

        var instance = new AssetInstance
        {
            AssetInstanceId = 1,
            AssetId = 1,
            InstanceCode = "INST001",
            WarehouseId = 1,
            Status = 1
        };

        var location1 = new AssetLocation
        {
            LocationId = 1,
            AssetInstanceId = 1,
            DepartmentId = 1,
            StartDate = new DateOnly(2025, 1, 1),
            IsCurrent = true,
            Note = "Currently in IT"
        };

        var location2 = new AssetLocation
        {
            LocationId = 2,
            AssetInstanceId = 1,
            DepartmentId = 2,
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            IsCurrent = false,
            Note = "Previously in HR"
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            DepartmentId = 1,
            Name = "Nguyen Van A",
            Code = "EMP001",
            UserId = 1,
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        };

        _context.Departments.AddRange(dept1, dept2);
        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.Add(asset);
        _context.AssetInstances.Add(instance);
        _context.AssetLocations.AddRange(location1, location2);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return (asset, instance, location1, location2);
    }

    #endregion
}
