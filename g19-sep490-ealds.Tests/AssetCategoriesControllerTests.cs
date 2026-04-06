using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.AssetCategory;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AssetCategoriesControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly AssetCategoriesController _controller;

    public AssetCategoriesControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new AssetCategoriesController(_context);
    }


    #region GetById Tests

    /// <summary>
    /// Test case: Get category by valid ID
    /// Expected output: 200 OK with AssetCategoryDetailDto containing CategoryId, Name, AssetTypeCount, and AssetTypes list
    /// </summary>
    [Fact]
    public async Task GetById_WithValidId_ReturnsCategoryDetails()
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

        var warehouse = new WarehouseAsset
        {
            WarehouseId = 1,
            Name = "Main Warehouse"
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
        _context.AssetTypes.Add(assetType);
        _context.WarehouseAssets.Add(warehouse);
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        
        var categoryDetail = Assert.IsType<AssetCategoryDetailDto>(okResult.Value);
        Assert.Equal(1, categoryDetail.CategoryId);
        Assert.Equal("Computer Equipment", categoryDetail.Name);
        Assert.Equal(1, categoryDetail.AssetTypeCount);
        Assert.Single(categoryDetail.AssetTypes);
        Assert.Equal("Laptop", categoryDetail.AssetTypes.First().Name);
        Assert.Equal(1, categoryDetail.AssetTypes.First().AssetCount);
    }

    /// <summary>
    /// Test case: Get category by invalid ID
    /// Expected output: 404 Not Found with error message: "Asset category with id {id} not found."
    /// </summary>
    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange - No category with ID 999

        // Act
        var result = await _controller.GetById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Test case: Get category by ID with multiple asset types
    /// Expected output: 200 OK with AssetCategoryDetailDto where AssetTypes are sorted alphabetically by Name
    /// </summary>
    [Fact]
    public async Task GetById_WithMultipleAssetTypes_ReturnsSortedAssetTypes()
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
            Name = "Zebra Device"
        };

        var assetType2 = new AssetType
        {
            AssetTypeId = 2,
            CategoryId = 1,
            Name = "Apple Device"
        };

        var assetType3 = new AssetType
        {
            AssetTypeId = 3,
            CategoryId = 1,
            Name = "Microsoft Device"
        };

        _context.AssetCategories.Add(category);
        _context.AssetTypes.AddRange(assetType1, assetType2, assetType3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var categoryDetail = Assert.IsType<AssetCategoryDetailDto>(okResult.Value);

        Assert.Equal(3, categoryDetail.AssetTypes.Count());
        var assetTypeList = categoryDetail.AssetTypes.ToList();
        Assert.Equal("Apple Device", assetTypeList[0].Name);
        Assert.Equal("Microsoft Device", assetTypeList[1].Name);
        Assert.Equal("Zebra Device", assetTypeList[2].Name);
    }

    /// <summary>
    /// Test case: Get category by ID with no asset types
    /// Expected output: 200 OK with AssetCategoryDetailDto where AssetTypes is empty and AssetTypeCount is 0
    /// </summary>
    [Fact]
    public async Task GetById_WithNoAssetTypes_ReturnsEmptyAssetTypesList()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var categoryDetail = Assert.IsType<AssetCategoryDetailDto>(okResult.Value);

        Assert.Equal(1, categoryDetail.CategoryId);
        Assert.Equal(0, categoryDetail.AssetTypeCount);
        Assert.Empty(categoryDetail.AssetTypes);
    }

    /// <summary>
    /// Test case: Get category by ID with negative ID
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetById_WithNegativeId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(-1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Test case: Create category with valid data
    /// Expected output: 201 Created with AssetCategoryResponseDto containing CategoryId, Name, AssetTypeCount=0
    /// </summary>
    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreateAssetCategoryDto
        {
            Name = "Computer Equipment"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("GetById", createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.Equal(2, createdResult.RouteValues["id"]);
        
        var category = Assert.IsType<AssetCategoryResponseDto>(createdResult.Value);
        Assert.True(category.CategoryId > 0);
        Assert.Equal("Computer Equipment", category.Name);
        Assert.Equal(0, category.AssetTypeCount);

        // Verify database
        var savedCategory = await _context.AssetCategories.FindAsync(category.CategoryId);
        Assert.NotNull(savedCategory);
        Assert.Equal("Computer Equipment", savedCategory.Name);
    }

    /// <summary>
    /// Test case: Create category with duplicate name (case insensitive)
    /// Expected output: 409 Conflict with error message: "Category name '{name}' already exists."
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        var existingCategory = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        _context.AssetCategories.Add(existingCategory);
        await _context.SaveChangesAsync();

        var request = new CreateAssetCategoryDto
        {
            Name = "computer equipment" // Same name, different case
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    /// <summary>
    /// Test case: Create category with same name (exact match)
    /// Expected output: 409 Conflict with error message: "Category name '{name}' already exists."
    /// </summary>
    [Fact]
    public async Task Create_WithExactDuplicateName_ReturnsConflict()
    {
        // Arrange
        var existingCategory = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        _context.AssetCategories.Add(existingCategory);
        await _context.SaveChangesAsync();

        var request = new CreateAssetCategoryDto
        {
            Name = "Computer Equipment"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    /// <summary>
    /// Test case: Create category with name containing spaces (should be trimmed)
    /// Expected output: 201 Created with AssetCategoryResponseDto where Name is trimmed
    /// </summary>
    [Fact]
    public async Task Create_WithSpacesInName_TrimsAndCreates()
    {
        // Arrange
        var request = new CreateAssetCategoryDto
        {
            Name = "  Computer Equipment  "
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var category = Assert.IsType<AssetCategoryResponseDto>(createdResult.Value);

        Assert.Equal("Computer Equipment", category.Name);
        Assert.DoesNotContain("  ", category.Name);
    }

    /// <summary>
    /// Test case: Create multiple categories successfully
    /// Expected output: 201 Created for each category with unique CategoryId
    /// </summary>
    [Fact]
    public async Task Create_MultipleCategories_ReturnsCreated()
    {
        // Arrange
        var request1 = new CreateAssetCategoryDto { Name = "Computer Equipment" };
        var request2 = new CreateAssetCategoryDto { Name = "Office Furniture" };
        var request3 = new CreateAssetCategoryDto { Name = "Vehicles" };

        // Act
        var result1 = await _controller.Create(request1);
        var result2 = await _controller.Create(request2);
        var result3 = await _controller.Create(request3);

        // Assert
        var createdResult1 = Assert.IsType<CreatedAtActionResult>(result1.Result);
        var createdResult2 = Assert.IsType<CreatedAtActionResult>(result2.Result);
        var createdResult3 = Assert.IsType<CreatedAtActionResult>(result3.Result);

        var category1 = Assert.IsType<AssetCategoryResponseDto>(createdResult1.Value);
        var category2 = Assert.IsType<AssetCategoryResponseDto>(createdResult2.Value);
        var category3 = Assert.IsType<AssetCategoryResponseDto>(createdResult3.Value);

        Assert.Equal("Computer Equipment", category1.Name);
        Assert.Equal("Office Furniture", category2.Name);
        Assert.Equal("Vehicles", category3.Name);
        Assert.NotEqual(category1.CategoryId, category2.CategoryId);
        Assert.NotEqual(category2.CategoryId, category3.CategoryId);

        // Verify database
        var allCategories = await _context.AssetCategories.ToListAsync();
        Assert.Equal(3, allCategories.Count);
    }

    /// <summary>
    /// Test case: Create category with single character name
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public async Task Create_WithSingleCharacterName_ReturnsCreated()
    {
        // Arrange
        var request = new CreateAssetCategoryDto
        {
            Name = "A"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var category = Assert.IsType<AssetCategoryResponseDto>(createdResult.Value);

        Assert.Equal("A", category.Name);
    }

    /// <summary>
    /// Test case: Create category with Unicode characters
    /// Expected output: 201 Created with Unicode name
    /// </summary>
    [Fact]
    public async Task Create_WithUnicodeName_ReturnsCreated()
    {
        // Arrange
        var request = new CreateAssetCategoryDto
        {
            Name = "Thiết bị văn phòng"
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var category = Assert.IsType<AssetCategoryResponseDto>(createdResult.Value);

        Assert.Equal("Thiết bị văn phòng", category.Name);
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Test case: Update category with valid data
    /// Expected output: 200 OK with AssetCategoryResponseDto containing updated CategoryId, Name, AssetTypeCount
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

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetCategoryDto
        {
            Name = "IT Equipment"
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        
        var updatedCategory = Assert.IsType<AssetCategoryResponseDto>(okResult.Value);
        Assert.Equal(1, updatedCategory.CategoryId);
        Assert.Equal("IT Equipment", updatedCategory.Name);

        // Verify database
        var savedCategory = await _context.AssetCategories.FindAsync(1);
        Assert.NotNull(savedCategory);
        Assert.Equal("IT Equipment", savedCategory.Name);
    }

    /// <summary>
    /// Test case: Update category with invalid ID
    /// Expected output: 404 Not Found with error message: "Asset category with id {id} not found."
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateAssetCategoryDto
        {
            Name = "IT Equipment"
        };

        // Act
        var result = await _controller.Update(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Test case: Update category name to an existing name (case insensitive)
    /// Expected output: 409 Conflict with error message: "Category name '{name}' already exists."
    /// </summary>
    [Fact]
    public async Task Update_ToExistingName_ReturnsConflict()
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

        _context.AssetCategories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetCategoryDto
        {
            Name = "computer equipment" // Same name, different case
        };

        // Act
        var result = await _controller.Update(2, request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    /// <summary>
    /// Test case: Update category with same name (should succeed, it's the same category)
    /// Expected output: 200 OK with AssetCategoryResponseDto
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

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetCategoryDto
        {
            Name = "Computer Equipment" // Same name
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedCategory = Assert.IsType<AssetCategoryResponseDto>(okResult.Value);

        Assert.Equal("Computer Equipment", updatedCategory.Name);
    }

    /// <summary>
    /// Test case: Update category with spaces in name (should be trimmed)
    /// Expected output: 200 OK with AssetCategoryResponseDto where Name is trimmed
    /// </summary>
    [Fact]
    public async Task Update_WithSpacesInName_TrimsAndUpdates()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetCategoryDto
        {
            Name = "  IT Equipment  "
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedCategory = Assert.IsType<AssetCategoryResponseDto>(okResult.Value);

        Assert.Equal("IT Equipment", updatedCategory.Name);
        Assert.DoesNotContain("  ", updatedCategory.Name);
    }

    /// <summary>
    /// Test case: Update category returns correct asset type count
    /// Expected output: 200 OK with AssetCategoryResponseDto where AssetTypeCount is correctly calculated
    /// </summary>
    [Fact]
    public async Task Update_ReturnsCorrectAssetTypeCount()
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

        var request = new UpdateAssetCategoryDto
        {
            Name = "IT Equipment"
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedCategory = Assert.IsType<AssetCategoryResponseDto>(okResult.Value);

        Assert.Equal(2, updatedCategory.AssetTypeCount);
    }

    /// <summary>
    /// Test case: Update category name to another existing category name
    /// Expected output: 409 Conflict
    /// </summary>
    [Fact]
    public async Task Update_ToAnotherCategoryName_ReturnsConflict()
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

        _context.AssetCategories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        var request = new UpdateAssetCategoryDto
        {
            Name = "Office Furniture"
        };

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    /// <summary>
    /// Test case: Update non-existent category
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Update_NonExistentCategory_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateAssetCategoryDto
        {
            Name = "New Category Name"
        };

        // Act
        var result = await _controller.Update(999, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Test case: Delete category with valid ID and no asset types
    /// Expected output: 204 No Content, category is removed from database
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

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);

        var deletedCategory = await _context.AssetCategories.FindAsync(1);
        Assert.Null(deletedCategory);
    }

    /// <summary>
    /// Test case: Delete category with invalid ID
    /// Expected output: 404 Not Found with error message: "Asset category with id {id} not found."
    /// </summary>
    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange - No category with ID 999

        // Act
        var result = await _controller.Delete(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    /// <summary>
    /// Test case: Delete category with linked asset types
    /// Expected output: 409 Conflict with error message: "Cannot delete category '{name}' because it has {count} asset type(s) linked to it. Remove or reassign them first."
    /// </summary>
    [Fact]
    public async Task Delete_WithLinkedAssetTypes_ReturnsConflict()
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
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
        
        // Verify category is still in database
        var categoryStillExists = await _context.AssetCategories.FindAsync(1);
        Assert.NotNull(categoryStillExists);
        
        // Verify asset type is still in database
        var assetTypeStillExists = await _context.AssetTypes.FindAsync(1);
        Assert.NotNull(assetTypeStillExists);
    }

    /// <summary>
    /// Test case: Delete category with multiple linked asset types
    /// Expected output: 409 Conflict with error message showing correct count of linked asset types
    /// </summary>
    [Fact]
    public async Task Delete_WithMultipleLinkedAssetTypes_ReturnsConflictWithCorrectMessage()
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
        var result = await _controller.Delete(1);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    /// <summary>
    /// Test case: Delete category with negative ID
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Delete_WithNegativeId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.Delete(-1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: Delete already deleted category
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Delete_AlreadyDeletedCategory_ReturnsNotFound()
    {
        // Arrange
        var category = new AssetCategory
        {
            CategoryId = 1,
            Name = "Computer Equipment"
        };

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();

        // Delete once
        await _controller.Delete(1);

        // Act - Try to delete again
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
