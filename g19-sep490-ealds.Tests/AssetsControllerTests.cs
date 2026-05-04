using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Assets;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AssetsControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly Mock<IAssetService> _mockService;
    private readonly AssetsController _controller;
    private const int AdminUserId = 1;

    public AssetsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        _mockService = new Mock<IAssetService>();
        _controller = new AssetsController(_mockService.Object);

        SetUserContext(AdminUserId);

        SeedTestData();
    }

    private void SeedTestData()
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
            CreateDate = System.DateTime.UtcNow,
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
            CreateDate = System.DateTime.UtcNow,
            CreatedBy = AdminUserId
        });

        _context.AssetTypes.Add(new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        });

        _context.AssetTypes.Add(new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Monitor"
        });

        _context.WarehouseAssets.Add(new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        });

        _context.Assets.Add(new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            Unit = "piece",
            Status = (int)AssetStatus.Available,
            CreatedBy = AdminUserId
        });

        _context.SaveChanges();
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
        var createdDto = new AssetDetailResponseDTO
        {
            AssetId = 10,
            Code = "MON001",
            Name = "Dell Monitor 24",
            AssetTypeId = 2,
            Unit = "piece",
            Status = (int)AssetStatus.Available
        };
        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<int?>(), It.IsAny<CreateAssetDTO>()))
            .ReturnsAsync(createdDto);

        var request = new CreateAssetDTO
        {
            Code = "MON001",
            Name = "Dell Monitor 24",
            AssetTypeId = 2,
            Unit = "piece",
            Quantity = 5,
            CreatedBy = AdminUserId
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
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
        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<int?>(), It.IsAny<CreateAssetDTO>()))
            .ThrowsAsync(new ArgumentException("Asset code already exists"));

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
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create asset with specification
    /// Expected output: 201 Created with specification set
    /// </summary>
    [Fact]
    public async Task CreateAsset_WithSpecification_ReturnsCreated()
    {
        // Arrange
        var createdDto = new AssetDetailResponseDTO
        {
            AssetId = 11,
            Code = "HP001",
            Name = "HP Laptop",
            AssetTypeId = 1,
            Unit = "piece",
            Status = (int)AssetStatus.Available,
            Specification = "Intel i7, 16GB RAM, 512GB SSD"
        };
        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<int?>(), It.IsAny<CreateAssetDTO>()))
            .ReturnsAsync(createdDto);

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
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
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
        // Arrange
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string?>(), It.IsAny<AssetStatus?>(), It.IsAny<int?>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AssetResponseDTO>());

        // Act
        var result = await _controller.GetAll(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
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
        // Arrange
        var assets = new List<AssetResponseDTO>
        {
            new AssetResponseDTO { AssetId = 1, Code = "AST001", Name = "Dell Laptop", AssetTypeId = 1, Status = (int)AssetStatus.Available }
        };
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string?>(), It.IsAny<AssetStatus?>(), It.IsAny<int?>(), It.IsAny<bool>()))
            .ReturnsAsync(assets);

        // Act
        var result = await _controller.GetAll(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.Single(returnedAssets);
    }

    /// <summary>
    /// Test case: GetAll with keyword search on name
    /// Expected output: 200 OK with matching assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithKeywordNameSearch_ReturnsMatchingAssets()
    {
        // Arrange
        var assets = new List<AssetResponseDTO>
        {
            new AssetResponseDTO { AssetId = 1, Code = "AST001", Name = "Dell Laptop", AssetTypeId = 1, Status = (int)AssetStatus.Available }
        };
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<ClaimsPrincipal>(), "Dell", null, null, false))
            .ReturnsAsync(assets);

        // Act
        var result = await _controller.GetAll("Dell", null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.All(returnedAssets, a => Assert.Contains("Dell", a.Name));
    }

    /// <summary>
    /// Test case: GetAll with keyword search on code
    /// Expected output: 200 OK with matching assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithKeywordSearch_ReturnsMatchingAssets()
    {
        // Arrange
        var assets = new List<AssetResponseDTO>
        {
            new AssetResponseDTO { AssetId = 1, Code = "AST001", Name = "Dell Laptop", AssetTypeId = 1, Status = (int)AssetStatus.Available }
        };
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<ClaimsPrincipal>(), "AST001", null, null, false))
            .ReturnsAsync(assets);

        // Act
        var result = await _controller.GetAll("AST001", null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.True(returnedAssets.Count() > 0);
    }

    /// <summary>
    /// Test case: GetAll with asset type filter
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithAssetTypeFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assets = new List<AssetResponseDTO>
        {
            new AssetResponseDTO { AssetId = 1, Code = "AST001", Name = "Dell Laptop", AssetTypeId = 1, Status = (int)AssetStatus.Available }
        };
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string?>(), It.IsAny<AssetStatus?>(), 1, false))
            .ReturnsAsync(assets);

        // Act
        var result = await _controller.GetAll(null, null, 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.All(returnedAssets, a => Assert.Equal(1, a.AssetTypeId));
    }

    /// <summary>
    /// Test case: GetAll with status filter
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assets = new List<AssetResponseDTO>
        {
            new AssetResponseDTO { AssetId = 1, Code = "AST001", Name = "Dell Laptop", AssetTypeId = 1, Status = (int)AssetStatus.Available }
        };
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string?>(), AssetStatus.Available, It.IsAny<int?>(), false))
            .ReturnsAsync(assets);

        // Act
        var result = await _controller.GetAll(null, AssetStatus.Available, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(okResult.Value);
        Assert.All(returnedAssets, a => Assert.Equal(AssetStatus.Available, a.Status));
    }

    #endregion
}
