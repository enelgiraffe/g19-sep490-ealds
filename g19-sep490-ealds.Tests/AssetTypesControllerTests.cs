using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.AssetType;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AssetTypesControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly AssetTypesController _controller;

    public AssetTypesControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new AssetTypesController(_context);
    }

    #region GetAll Tests

    /// <summary>
    /// Test case: Get all asset types without filters
    /// Expected output: 200 OK with list of AssetTypeResponseDto containing all asset types
    /// </summary>
    [Fact]
    public async Task GetAll_WithoutFilters_ReturnsAllAssetTypes()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType1 = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Desktop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.AddRange(assetType1, assetType2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypes = Assert.IsAssignableFrom<IEnumerable<AssetTypeResponseDto>>(okResult.Value);
        var assetTypeList = assetTypes.ToList();

        Assert.Equal(2, assetTypeList.Count);
        Assert.Contains(assetTypeList, at => at.AssetTypeId == 1 && at.Name == "Laptop");
        Assert.Contains(assetTypeList, at => at.AssetTypeId == 2 && at.Name == "Desktop");
    }

    /// <summary>
    /// Test case: Get all asset types returns empty list when no asset types exist
    /// Expected output: 200 OK with empty IEnumerable<AssetTypeResponseDto>
    /// </summary>
    [Fact]
    public async Task GetAll_WithNoAssetTypes_ReturnsEmptyList()
    {
        // Arrange - No asset types in database

        // Act
        var result = await _controller.GetAll(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypes = Assert.IsAssignableFrom<IEnumerable<AssetTypeResponseDto>>(okResult.Value);
        
        Assert.Empty(assetTypes);
    }

    /// <summary>
    /// Test case: Filter asset types by category ID
    /// Expected output: 200 OK with IEnumerable<AssetTypeResponseDto> filtered by categoryId
    /// </summary>
    [Fact]
    public async Task GetAll_WithCategoryIdFilter_ReturnsFilteredAssetTypes()
    {
        // Arrange
        var category1 = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var category2 = new AssetCategory
        {
            CategoryId = 2,
            Name = "Office Furniture"
        };

        var assetType1 = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 2,
            Name = "Chair"
        };

        _context.AssetCategories.AddRange(category1, category2);
        _context.AssetTypes.AddRange(assetType1, assetType2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(1, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypes = Assert.IsAssignableFrom<IEnumerable<AssetTypeResponseDto>>(okResult.Value);
        var assetTypeList = assetTypes.ToList();

        Assert.Single(assetTypeList);
        Assert.Equal(1, assetTypeList[0].AssetTypeId);
        Assert.Equal("Laptop", assetTypeList[0].Name);
        Assert.Equal(1, assetTypeList[0].CategoryId);
        Assert.Equal("Computer Equipment", assetTypeList[0].CategoryName);
    }

    /// <summary>
    /// Test case: Search asset types by keyword
    /// Expected output: 200 OK with IEnumerable<AssetTypeResponseDto> containing matching asset types
    /// </summary>
    [Fact]
    public async Task GetAll_WithKeywordSearch_ReturnsMatchingAssetTypes()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType1 = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Desktop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.AddRange(assetType1, assetType2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, "Laptop");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypes = Assert.IsAssignableFrom<IEnumerable<AssetTypeResponseDto>>(okResult.Value);
        var assetTypeList = assetTypes.ToList();

        Assert.Single(assetTypeList);
        Assert.Equal("Laptop", assetTypeList[0].Name);
        Assert.Contains(assetTypeList, at => at.Name.Contains("Laptop"));
    }

    /// <summary>
    /// Test case: Search asset types by keyword (case insensitive)
    /// Expected output: 200 OK with IEnumerable<AssetTypeResponseDto> containing matching asset types (case insensitive)
    /// </summary>
    [Fact]
    public async Task GetAll_WithKeywordSearchCaseInsensitive_ReturnsMatchingAssetTypes()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, "LAPTOP");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypes = Assert.IsAssignableFrom<IEnumerable<AssetTypeResponseDto>>(okResult.Value);
        var assetTypeList = assetTypes.ToList();

        Assert.Single(assetTypeList);
        Assert.Equal("Laptop", assetTypeList[0].Name);
    }

    /// <summary>
    /// Test case: Get all asset types sorted by category name, then by asset type name
    /// Expected output: 200 OK with IEnumerable<AssetTypeResponseDto> sorted by CategoryName ASC, Name ASC
    /// </summary>
    [Fact]
    public async Task GetAll_ReturnsAssetTypesSortedByCategoryThenByName()
    {
        // Arrange
        var category1 = new AssetCategory
        {
            CategoryId = 1,
            Name = "Office Furniture"
        };

        var category2 = new AssetCategory
        {
            CategoryId = 2,
            Name = "Computer Equipment"
        };

        var assetType1 = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 2,
            Name = "Zebra Laptop"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Apple Chair"
        };

        var assetType3 = new AssetType
        {
            AssetTypeId = 3,
            CategoryId = 2,
            Name = "Apple Laptop"
        };

        _context.AssetCategories.AddRange(category1, category2);
        _context.AssetTypes.AddRange(assetType1, assetType2, assetType3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypes = Assert.IsAssignableFrom<IEnumerable<AssetTypeResponseDto>>(okResult.Value);
        var assetTypeList = assetTypes.ToList();

        Assert.Equal(3, assetTypeList.Count);
        Assert.Equal("Apple Chair", assetTypeList[0].Name);
        Assert.Equal("Apple Laptop", assetTypeList[1].Name);
        Assert.Equal("Zebra Laptop", assetTypeList[2].Name);
    }

    /// <summary>
    /// Test case: Get all asset types with correct asset count
    /// Expected output: 200 OK with IEnumerable<AssetTypeResponseDto> where AssetCount is correctly calculated
    /// </summary>
    [Fact]
    public async Task GetAll_ReturnsAssetTypesWithCorrectAssetCount()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
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
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 1100.00m,
            CurrentValue = 1100.00m,
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        _context.AssetCategories.Add(category);
        _context.WarehouseAssets.Add(warehouse);
        _context.AssetTypes.Add(assetType);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypes = Assert.IsAssignableFrom<IEnumerable<AssetTypeResponseDto>>(okResult.Value);
        var assetTypeList = assetTypes.ToList();

        Assert.Single(assetTypeList);
        Assert.Equal(2, assetTypeList[0].AssetCount);
    }

    /// <summary>
    /// Test case: Get all asset types with both category filter and keyword search
    /// Expected output: 200 OK with IEnumerable<AssetTypeResponseDto> filtered by both criteria
    /// </summary>
    [Fact]
    public async Task GetAll_WithCategoryIdAndKeywordFilter_ReturnsFilteredAssetTypes()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType1 = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Gaming Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.AddRange(assetType1, assetType2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(1, "Gaming");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypes = Assert.IsAssignableFrom<IEnumerable<AssetTypeResponseDto>>(okResult.Value);
        var assetTypeList = assetTypes.ToList();

        Assert.Single(assetTypeList);
        Assert.Equal("Gaming Laptop", assetTypeList[0].Name);
    }

    #endregion

    #region GetById Tests

    /// <summary>
    /// Test case: Get asset type by valid ID
    /// Expected output: 200 OK with AssetTypeDetailDto containing all details including InventorySessionCount and MaintenanceTemplateCount
    /// </summary>
    [Fact]
    public async Task GetById_WithValidId_ReturnsAssetTypeDetails()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypeDetail = Assert.IsType<AssetTypeDetailDto>(okResult.Value);

        Assert.Equal(1, assetTypeDetail.AssetTypeId);
        Assert.Equal("Laptop", assetTypeDetail.Name);
        Assert.Equal(1, assetTypeDetail.CategoryId);
        Assert.Equal("Computer Equipment", assetTypeDetail.CategoryName);
        Assert.Equal(0, assetTypeDetail.AssetCount);
        Assert.Equal(0, assetTypeDetail.InventorySessionCount);
        Assert.Equal(0, assetTypeDetail.MaintenanceTemplateCount);
    }

    /// <summary>
    /// Test case: Get asset type by valid ID with linked data
    /// Expected output: 200 OK with AssetTypeDetailDto where counts are correctly populated
    /// </summary>
    [Fact]
    public async Task GetById_WithValidIdAndLinkedData_ReturnsAssetTypeDetailsWithCounts()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var asset = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        var inventorySession = new InventorySession
        {
            SessionId = 1,
            AssetTypeId = 1,
            SessionDate = new DateOnly(2024, 6, 1),
            Status = 1
        };

        var maintenanceTemplate = new MaintenanceTemplate
        {
            TemplateId = 1,
            AssetTypeId = 1,
            Name = "Monthly Check"
        };

        _context.AssetCategories.Add(category);
        _context.WarehouseAssets.Add(warehouse);
        _context.AssetTypes.Add(assetType);
        _context.Assets.Add(asset);
        _context.InventorySessions.Add(inventorySession);
        _context.MaintenanceTemplates.Add(maintenanceTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assetTypeDetail = Assert.IsType<AssetTypeDetailDto>(okResult.Value);

        Assert.Equal(1, assetTypeDetail.AssetCount);
        Assert.Equal(1, assetTypeDetail.InventorySessionCount);
        Assert.Equal(1, assetTypeDetail.MaintenanceTemplateCount);
    }

    /// <summary>
    /// Test case: Get asset type by invalid ID
    /// Expected output: 404 Not Found with error message: "Asset type with id {id} not found."
    /// </summary>
    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange - No asset type with ID 999

        // Act
        var result = await _controller.GetById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(notFoundResult.Value);
        
        Assert.Contains("Asset type with id 999 not found.", errorResponse.ToString());
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Test case: Create asset type with valid data
    /// Expected output: 201 Created with AssetTypeResponseDto containing AssetTypeId, Name, CategoryId, CategoryName, AssetCount=0
    /// </summary>
    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        var request = new CreateAssetTypeDto
        {
            CategoryId = 1,
            Name = "Laptop"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetById", createdResult.ActionName);
        
        var assetType = Assert.IsType<AssetTypeResponseDto>(createdResult.Value);
        Assert.True(assetType.AssetTypeId > 0);
        Assert.Equal("Laptop", assetType.Name);
        Assert.Equal(1, assetType.CategoryId);
        Assert.Equal("Computer Equipment", assetType.CategoryName);
        Assert.Equal(0, assetType.AssetCount);
    }

    /// <summary>
    /// Test case: Create asset type with invalid category ID
    /// Expected output: 404 Not Found with error message: "Asset category with id {categoryId} not found."
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidCategoryId_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateAssetTypeDto
        {
            CategoryId = 999,
            Name = "Laptop"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(notFoundResult.Value);
        
        Assert.Contains("Asset category with id 999 not found.", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Create asset type with duplicate name in same category (case insensitive)
    /// Expected output: 409 Conflict with error message: "Asset type '{name}' already exists in this category."
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicateNameInSameCategory_ReturnsConflict()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var existingAssetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(existingAssetType);
        await _context.SaveChangesAsync();

        var request = new CreateAssetTypeDto
        {
            CategoryId = 1,
            Name = "laptop" // Same name, different case
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(conflictResult.Value);
        
        Assert.Contains("Asset type 'laptop' already exists in this category.", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Create asset type with exact duplicate name in same category
    /// Expected output: 409 Conflict with error message: "Asset type '{name}' already exists in this category."
    /// </summary>
    [Fact]
    public async Task Create_WithExactDuplicateNameInSameCategory_ReturnsConflict()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var existingAssetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(existingAssetType);
        await _context.SaveChangesAsync();

        var request = new CreateAssetTypeDto
        {
            CategoryId = 1,
            Name = "Laptop" // Exact same name
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(conflictResult.Value);
        
        Assert.Contains("Asset type 'Laptop' already exists in this category.", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Create asset type with same name in different category (should succeed)
    /// Expected output: 201 Created with AssetTypeResponseDto
    /// </summary>
    [Fact]
    public async Task Create_WithSameNameInDifferentCategory_ReturnsCreated()
    {
        // Arrange
        var category1 = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var category2 = new AssetCategory
        {
            CategoryId = 2,
            Name = "Office Furniture"
        };

        var existingAssetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.AddRange(category1, category2);
        _context.AssetTypes.Add(existingAssetType);
        await _context.SaveChangesAsync();

        var request = new CreateAssetTypeDto
        {
            CategoryId = 2,
            Name = "Laptop" // Same name, different category
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var assetType = Assert.IsType<AssetTypeResponseDto>(createdResult.Value);

        Assert.Equal("Laptop", assetType.Name);
        Assert.Equal(2, assetType.CategoryId);
        Assert.Equal("Office Furniture", assetType.CategoryName);
    }

    /// <summary>
    /// Test case: Create asset type with spaces in name (should be trimmed)
    /// Expected output: 201 Created with AssetTypeResponseDto where Name is trimmed
    /// </summary>
    [Fact]
    public async Task Create_WithSpacesInName_TrimsAndCreates()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        var request = new CreateAssetTypeDto
        {
            CategoryId = 1,
            Name = "  Laptop  "
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var assetType = Assert.IsType<AssetTypeResponseDto>(createdResult.Value);

        Assert.Equal("Laptop", assetType.Name);
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Test case: Update asset type name
    /// Expected output: 200 OK with AssetTypeResponseDto containing updated Name, CategoryId, CategoryName, AssetCount
    /// </summary>
    [Fact]
    public async Task Update_WithValidData_ReturnsUpdated()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetTypeDto
        {
            CategoryId = 1,
            Name = "Notebook"
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedAssetType = Assert.IsType<AssetTypeResponseDto>(okResult.Value);

        Assert.Equal(1, updatedAssetType.AssetTypeId);
        Assert.Equal("Notebook", updatedAssetType.Name);
        Assert.Equal(1, updatedAssetType.CategoryId);
        Assert.Equal("Computer Equipment", updatedAssetType.CategoryName);
    }

    /// <summary>
    /// Test case: Update asset type category
    /// Expected output: 200 OK with AssetTypeResponseDto containing updated CategoryId and CategoryName
    /// </summary>
    [Fact]
    public async Task Update_ChangeCategory_ReturnsUpdated()
    {
        // Arrange
        var category1 = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var category2 = new AssetCategory
        {
            CategoryId = 2,
            Name = "Office Electronics"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.AddRange(category1, category2);
        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetTypeDto
        {
            CategoryId = 2,
            Name = "Laptop"
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedAssetType = Assert.IsType<AssetTypeResponseDto>(okResult.Value);

        Assert.Equal(2, updatedAssetType.CategoryId);
        Assert.Equal("Office Electronics", updatedAssetType.CategoryName);
    }

    /// <summary>
    /// Test case: Update asset type with invalid ID
    /// Expected output: 404 Not Found with error message: "Asset type with id {id} not found."
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateAssetTypeDto
        {
            CategoryId = 1,
            Name = "Laptop"
        };

        // Act
        var result = await _controller.Update(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(notFoundResult.Value);
        
        Assert.Contains("Asset type with id 999 not found.", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Update asset type with invalid category ID
    /// Expected output: 404 Not Found with error message: "Asset category with id {categoryId} not found."
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidCategoryId_ReturnsNotFound()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetTypeDto
        {
            CategoryId = 999,
            Name = "Laptop"
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(notFoundResult.Value);
        
        Assert.Contains("Asset category with id 999 not found.", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Update asset type to duplicate name in same category
    /// Expected output: 409 Conflict with error message: "Asset type '{name}' already exists in this category."
    /// </summary>
    [Fact]
    public async Task Update_ToDuplicateNameInSameCategory_ReturnsConflict()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType1 = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Desktop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.AddRange(assetType1, assetType2);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetTypeDto
        {
            CategoryId = 1,
            Name = "laptop" // Same name as assetType1, different case
        };

        // Act
        var result = await _controller.Update(2, request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(conflictResult.Value);
        
        Assert.Contains("Asset type 'laptop' already exists in this category.", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Update asset type to same name (should succeed)
    /// Expected output: 200 OK with AssetTypeResponseDto
    /// </summary>
    [Fact]
    public async Task Update_ToSameName_ReturnsUpdated()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetTypeDto
        {
            CategoryId = 1,
            Name = "Laptop" // Same name
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedAssetType = Assert.IsType<AssetTypeResponseDto>(okResult.Value);

        Assert.Equal("Laptop", updatedAssetType.Name);
    }

    /// <summary>
    /// Test case: Update asset type returns correct asset count
    /// Expected output: 200 OK with AssetTypeResponseDto where AssetCount is correctly calculated
    /// </summary>
    [Fact]
    public async Task Update_ReturnsCorrectAssetCount()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
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
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        var asset2 = new Asset
        {
            AssetId = 2,
            Code = "AST002",
            Name = "HP Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 2, 1),
            OriginalPrice = 1100.00m,
            CurrentValue = 1100.00m,
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        _context.AssetCategories.Add(category);
        _context.WarehouseAssets.Add(warehouse);
        _context.AssetTypes.Add(assetType);
        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetTypeDto
        {
            CategoryId = 1,
            Name = "Notebook"
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedAssetType = Assert.IsType<AssetTypeResponseDto>(okResult.Value);

        Assert.Equal(2, updatedAssetType.AssetCount);
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Test case: Delete asset type with valid ID and no linked data
    /// Expected output: 204 No Content, asset is removed from database
    /// </summary>
    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedAssetType = await _context.AssetTypes.FindAsync(1);
        Assert.Null(deletedAssetType);
    }

    /// <summary>
    /// Test case: Delete asset type with invalid ID
    /// Expected output: 404 Not Found with error message: "Asset type with id {id} not found."
    /// </summary>
    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange - No asset type with ID 999

        // Act
        var result = await _controller.Delete(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(notFoundResult.Value);
        
        Assert.Contains("Asset type with id 999 not found.", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Delete asset type with linked assets
    /// Expected output: 409 Conflict with error message: "Cannot delete asset type '{name}' because it is linked to {count} asset(s). Remove or reassign them first."
    /// </summary>
    [Fact]
    public async Task Delete_WithLinkedAssets_ReturnsConflict()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var asset = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        _context.AssetCategories.Add(category);
        _context.WarehouseAssets.Add(warehouse);
        _context.AssetTypes.Add(assetType);
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(conflictResult.Value);
        
        Assert.Contains("Cannot delete asset type 'Laptop' because it is linked to 1 asset(s)", errorResponse.ToString());
        
        // Verify asset type is still in database
        var assetTypeStillExists = await _context.AssetTypes.FindAsync(1);
        Assert.NotNull(assetTypeStillExists);
    }

    /// <summary>
    /// Test case: Delete asset type with linked inventory sessions
    /// Expected output: 409 Conflict with error message mentioning inventory session(s)
    /// </summary>
    [Fact]
    public async Task Delete_WithLinkedInventorySessions_ReturnsConflict()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var inventorySession = new InventorySession
        {
            SessionId = 1,
            AssetTypeId = 1,
            SessionDate = new DateOnly(2024, 6, 1),
            Status = 1
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(assetType);
        _context.InventorySessions.Add(inventorySession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(conflictResult.Value);
        
        Assert.Contains("Cannot delete asset type 'Laptop' because it is linked to 1 inventory session(s)", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Delete asset type with linked maintenance templates
    /// Expected output: 409 Conflict with error message mentioning maintenance template(s)
    /// </summary>
    [Fact]
    public async Task Delete_WithLinkedMaintenanceTemplates_ReturnsConflict()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var maintenanceTemplate = new MaintenanceTemplate
        {
            TemplateId = 1,
            AssetTypeId = 1,
            Name = "Monthly Check"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.Add(assetType);
        _context.MaintenanceTemplates.Add(maintenanceTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(conflictResult.Value);
        
        Assert.Contains("Cannot delete asset type 'Laptop' because it is linked to 1 maintenance template(s)", errorResponse.ToString());
    }

    /// <summary>
    /// Test case: Delete asset type with multiple linked types
    /// Expected output: 409 Conflict with error message listing all linked types
    /// </summary>
    [Fact]
    public async Task Delete_WithMultipleLinkedTypes_ReturnsConflictWithAllIssues()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
        };

        var assetType = new AssetType
        {
            AssetTypeId = 1,
            CategoryId = 1,
            Name = "Laptop"
        };

        var asset = new Asset
        {
            AssetId = 1,
            Code = "AST001",
            Name = "Dell Laptop",
            AssetTypeId = 1,
            PurchaseDate = new DateOnly(2024, 1, 1),
            OriginalPrice = 1000.00m,
            CurrentValue = 1000.00m,
            Status = 1,
            Unit = "piece",
            Quantity = 1,
            WarehouseId = 1,
            CreatedBy = 1
        };

        var inventorySession = new InventorySession
        {
            SessionId = 1,
            AssetTypeId = 1,
            SessionDate = new DateOnly(2024, 6, 1),
            Status = 1
        };

        var maintenanceTemplate = new MaintenanceTemplate
        {
            TemplateId = 1,
            AssetTypeId = 1,
            Name = "Monthly Check"
        };

        _context.AssetCategories.Add(category);
        _context.WarehouseAssets.Add(warehouse);
        _context.AssetTypes.Add(assetType);
        _context.Assets.Add(asset);
        _context.InventorySessions.Add(inventorySession);
        _context.MaintenanceTemplates.Add(maintenanceTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<Microsoft.AspNetCore.Mvc.SerializableError>(conflictResult.Value);
        
        var errorString = errorResponse.ToString();
        Assert.Contains("1 asset(s)", errorString);
        Assert.Contains("1 inventory session(s)", errorString);
        Assert.Contains("1 maintenance template(s)", errorString);
    }

    #endregion
}
