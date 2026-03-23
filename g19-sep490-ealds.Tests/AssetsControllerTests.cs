using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AssetsControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly AssetsController _controller;

    public AssetsControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new AssetsController(_context);
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
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

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
            PurchaseDate = new DateOnly(2024, 1, 15),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.CreatedAtActionResult>(result.Result);
        var asset = Assert.IsType<AssetResponseDTO>(actionResult.Value);
        
        Assert.Equal("AST001", asset.Code);
        Assert.Equal("Dell Laptop", asset.Name);
        Assert.Equal(AssetStatus.Available, asset.Status);
        Assert.Equal(1, asset.AssetTypeId);
        Assert.Equal(1, asset.WarehouseId);
    }

    /// <summary>
    /// Test case: Create asset with duplicate code
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateAsset_WithDuplicateCode_ReturnsBadRequest()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var existingAsset = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Existing Asset",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.Add(existingAsset);
        await _context.SaveChangesAsync();

        var request = new CreateAssetDTO
        {
            Code = "AST001", // Duplicate code
            Name = "New Asset",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 15),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case: Create asset with depreciation policy
    /// Expected output: 201 Created with depreciation info
    /// </summary>
    [Fact]
    public async Task CreateAsset_WithDepreciationPolicy_ReturnsCreated()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var policy = new DepreciationPolicy
        {
            PolicyId = 1,
            Name = "Straight Line 5 Years",
            UsefullLifeMonths = 60,
            SalvageValue = 100.00m
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.DepreciationPolicies.Add(policy);
        await _context.SaveChangesAsync();

        var request = new CreateAssetDTO
        {
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 15),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            DepreciationPolicyId = 1
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.CreatedAtActionResult>(result.Result);
        var asset = Assert.IsType<AssetResponseDTO>(actionResult.Value);
        
        Assert.Equal("AST001", asset.Code);
        Assert.Equal(1, asset.DepreciationPolicyId);
    }

    #endregion

    #region GetAll (Search & Filter) Tests

    /// <summary>
    /// Test case: Get all assets without filters
    /// Expected output: 200 OK with all assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithoutFilters_ReturnsAllAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.InUse,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, null, null, null, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Equal(2, assets.Count());
    }

    /// <summary>
    /// Test case: Search assets by keyword (code)
    /// Expected output: 200 OK with matching assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithKeywordSearch_ReturnsMatchingAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll("AST001", null, null, null, null, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal("AST001", assets.First().Code);
    }

    /// <summary>
    /// Test case: Search assets by keyword (name)
    /// Expected output: 200 OK with matching assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithKeywordNameSearch_ReturnsMatchingAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll("Laptop", null, null, null, null, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal("Dell Laptop", assets.First().Name);
    }

    /// <summary>
    /// Test case: Filter assets by status
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.InUse,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, AssetStatus.InUse, null, null, null, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal(AssetStatus.InUse, assets.First().Status);
    }

    /// <summary>
    /// Test case: Filter assets by asset type
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithAssetTypeFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType1 = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Printer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType1,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 2,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType2,
            Warehouse = warehouse
        };

        _context.AssetTypes.AddRange(assetType1, assetType2);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, 1, null, null, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal(1, assets.First().AssetTypeId);
    }

    /// <summary>
    /// Test case: Filter assets by warehouse
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithWarehouseFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse1 = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var warehouse2 = new WarehouseAsset
        {
            WarehouseId = 2,
            Name = "Secondary Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse1
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 2,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse2
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.AddRange(warehouse1, warehouse2);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, null, 2, null, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal(2, assets.First().WarehouseId);
    }

    /// <summary>
    /// Test case: Filter assets by price range (min price)
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithMinPriceFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "USB Cable",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 10.00m,
            CurrentValue = 10.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, null, null, 100.00m, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal(1000.00m, assets.First().CurrentValue);
    }

    /// <summary>
    /// Test case: Filter assets by price range (max price)
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithMaxPriceFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "USB Cable",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 10.00m,
            CurrentValue = 10.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, null, null, null, 500.00m, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal(10.00m, assets.First().CurrentValue);
    }

    /// <summary>
    /// Test case: Filter assets by price range (min and max)
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithPriceRangeFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "USB Cable",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 10.00m,
            CurrentValue = 10.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset3 = new Asset
        {
            AssetId = 3,
            Code = "AST003",
            Name = "Mouse",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 3, 1),
            OriginalPrice = 50.00m,
            CurrentValue = 50.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2, asset3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, null, null, 20.00m, 500.00m, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal(50.00m, assets.First().CurrentValue);
    }

    /// <summary>
    /// Test case: Filter assets by purchase date range (from date)
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithFromDateFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 6, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, null, null, null, null, new DateOnly(2024, 3, 1), null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal(new DateOnly(2024, 6, 1), assets.First().PurchaseDate);
    }

    /// <summary>
    /// Test case: Filter assets by purchase date range (to date)
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithToDateFilter_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 6, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType,
            Warehouse = warehouse
        };

        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null, null, null, null, null, null, new DateOnly(2024, 3, 1));

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal(new DateOnly(2024, 1, 1), assets.First().PurchaseDate);
    }

    /// <summary>
    /// Test case: Combine multiple filters (keyword + status + asset type)
    /// Expected output: 200 OK with filtered assets
    /// </summary>
    [Fact]
    public async Task GetAll_WithMultipleFilters_ReturnsFilteredAssets()
    {
        // Arrange
        var assetType1 = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Computer"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Printer"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var asset1 = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = (int)AssetStatus.Available,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType1,
            Warehouse = warehouse
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Printer",
            AssetTypeId = 2,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 500.00m,
            CurrentValue = 500.00m,
            Status = (int)AssetStatus.InUse,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1,
            AssetType = assetType2,
            Warehouse = warehouse
        };

        _context.AssetTypes.AddRange(assetType1, assetType2);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll("Dell", AssetStatus.Available, 1, null, null, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Single(assets);
        Assert.Equal("Dell Laptop", assets.First().Name);
        Assert.Equal(AssetStatus.Available, assets.First().Status);
    }

    /// <summary>
    /// Test case: Get all assets returns empty list when no assets exist
    /// Expected output: 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetAll_WithNoAssets_ReturnsEmptyList()
    {
        // Arrange - No assets in database

        // Act
        var result = await _controller.GetAll(null, null, null, null, null, null, null, null);

        // Assert
        var actionResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var assets = Assert.IsAssignableFrom<IEnumerable<AssetResponseDTO>>(actionResult.Value);
        Assert.Empty(assets);
    }

    #endregion
}
