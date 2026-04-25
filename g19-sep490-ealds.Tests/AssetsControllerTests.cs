using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AssetsControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly AssetsController _controller;
    private const int AdminUserId = 1;

    public AssetsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        var inMemorySettings = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "App:DepartmentHeadRoleId", "4" }
            }!)
            .Build();

        var maintenanceTemplateService = new Mock<g19_sep490_ealds.Server.Services.Interface.IMaintenanceTemplateService>().Object;

        _controller = new AssetsController(_context, inMemorySettings, maintenanceTemplateService);

        SeedTestData().Wait();
        SetUserContext(AdminUserId);
    }

    private async Task SeedTestData()
    {
        _context.Roles.Add(new Role { RoleId = 1, Code = "ADMIN", Name = "Administrator" });

        _context.Users.Add(new User
        {
            UserId = AdminUserId,
            Email = "admin@test.com",
            Password = "hashed",
            Status = 1
        });

        _context.UserRoles.Add(new UserRole { UserId = AdminUserId, RoleId = 1 });

        _context.Departments.Add(new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            Code = "IT",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = AdminUserId
        });

        _context.Employees.Add(new Employee
        {
            EmployeeId = 1,
            UserId = AdminUserId,
            DepartmentId = 1,
            Name = "Admin User",
            Code = "ADMIN001",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = AdminUserId
        });

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        _context.AssetTypes.Add(assetType);

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        await _context.SaveChangesAsync();

        var request = new CreateAssetDTO
        {
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            Unit = "piece",
            Quantity = 1,
            CreatedBy = AdminUserId
        };

        var result = await _controller.Create(request);
    }

    private void SetUserContext(int userId)
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

    #region CreateAsset Tests

    /// <summary>
    /// Test case: Create asset with valid data
    /// Expected output: 201 Created with asset data
    /// </summary>
    [Fact]
    public async Task CreateAsset_WithValidData_ReturnsCreated()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Monitor"
        };

        var request = new CreateAssetDTO
        {
            Code = "MON001",
            Name = "Dell Monitor 24",
            AssetTypeId = 2,
            Unit = "piece",
            Quantity = 5,
            CreatedBy = AdminUserId
        };

        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.CreatedAtActionResult>(result.Result);
        var createdResult = (Microsoft.AspNetCore.Mvc.CreatedAtActionResult)result.Result!;
        Assert.Equal(201, createdResult.StatusCode);

        var asset = Assert.IsType<AssetDetailResponseDTO>(createdResult.Value);
        Assert.Equal("Dell Monitor 24", asset.Name);
    }

    /// <summary>
    /// Test case: Create asset with duplicate code
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateAsset_WithDuplicateCode_ReturnsBadRequest()
    {
        // Arrange
        var existingAsset = new Asset
        {
            Code = "AST001",
            Name = "Existing Laptop",
            AssetTypeId = 1,
            Status = 1,
            Unit = "piece",
            CreatedBy = AdminUserId
        };
        _context.Assets.Add(existingAsset);
        await _context.SaveChangesAsync();

        var request = new CreateAssetDTO
        {
            Code = "AST001",
            Name = "New Laptop",
            AssetTypeId = 1,
            Unit = "piece",
            Quantity = 1,
            CreatedBy = AdminUserId
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create asset with specification
    /// Expected output: 201 Created with specification set
    /// </summary>
    [Fact]
    public async Task CreateAsset_WithSpecification_ReturnsCreated()
    {
        // Arrange
        var request = new CreateAssetDTO
        {
            Code = "HP001",
            Name = "HP Laptop",
            AssetTypeId = 1,
            Unit = "piece",
            Quantity = 2,
            CreatedBy = AdminUserId,
            Specification = "Intel i7, 16GB RAM, 512GB SSD"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.CreatedAtActionResult>(result.Result);
        var createdResult = (Microsoft.AspNetCore.Mvc.CreatedAtActionResult)result.Result!;
        var asset = Assert.IsType<AssetDetailResponseDTO>(createdResult.Value);
        Assert.Equal("HP Laptop", asset.Name);
    }

    #endregion

    #region GetAll Tests

    /// <summary>
    /// Test case: GetAll with no assets in database
    /// Expected output: 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetAll_WithNoAssets_ReturnsEmptyList()
    {
        // Arrange - clear all assets
        _context.Assets.RemoveRange(_context.Assets);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, null);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var okResult = (Microsoft.AspNetCore.Mvc.OkObjectResult)result.Result!;
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.Empty(assets);
    }

    /// <summary>
    /// Test case: GetAll without any filters
    /// Expected output: 200 OK with all assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithoutFilters_ReturnsAllAssets()
    {
        // Act
        var result = await _controller.GetAll(null, null, null);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var okResult = (Microsoft.AspNetCore.Mvc.OkObjectResult)result.Result!;
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.True(assets.Count() > 0);
    }

    /// <summary>
    /// Test case: GetAll with keyword search on name
    /// Expected output: 200 OK with matching assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithKeywordNameSearch_ReturnsMatchingAssets()
    {
        // Act
        var result = await _controller.GetAll("Dell", null, null);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var okResult = (Microsoft.AspNetCore.Mvc.OkObjectResult)result.Result!;
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.All(assets, a => Assert.Contains("Dell", a.Name));
    }

    /// <summary>
    /// Test case: GetAll with keyword search on code
    /// Expected output: 200 OK with matching assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithKeywordSearch_ReturnsMatchingAssets()
    {
        // Act
        var result = await _controller.GetAll("AST001", null, null);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var okResult = (Microsoft.AspNetCore.Mvc.OkObjectResult)result.Result!;
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.True(assets.Count() > 0);
    }

    /// <summary>
    /// Test case: GetAll with asset type filter
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithAssetTypeFilter_ReturnsFilteredAssets()
    {
        // Act
        var result = await _controller.GetAll(null, null, 1);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var okResult = (Microsoft.AspNetCore.Mvc.OkObjectResult)result.Result!;
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.All(assets, a => Assert.Equal(1, a.AssetTypeId));
    }

    /// <summary>
    /// Test case: GetAll with status filter
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredAssets()
    {
        // Act
        var result = await _controller.GetAll(null, AssetStatus.Available, null);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var okResult = (Microsoft.AspNetCore.Mvc.OkObjectResult)result.Result!;
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.All(assets, a => Assert.Equal(AssetStatus.Available, a.Status));
    }

    #endregion
}
