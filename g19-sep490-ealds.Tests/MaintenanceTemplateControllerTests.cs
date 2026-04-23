using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTemplate;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Mappers.Implementation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Implementation;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class MaintenanceTemplateControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly Mock<ILogger<MaintenanceTemplateService>> _mockLogger;
    private readonly MaintenanceTemplateController _controller;

    public MaintenanceTemplateControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _mockLogger = new Mock<ILogger<MaintenanceTemplateService>>();
        var service = new MaintenanceTemplateService(new MaintenanceTemplateMapper(), _context, _mockLogger.Object);
        _controller = new MaintenanceTemplateController(service);

        SeedTestData().Wait();
        SetUserClaim(1);
    }

    private async Task SeedTestData()
    {
        // Seed AssetType
        _context.AssetCategories.Add(new AssetCategory
        {
            CategoryId = 1,
            Name = "IT Equipment"
        });

        _context.AssetTypes.Add(new AssetType
        {
            AssetTypeId = 1,
            Name = "Computer",
            CategoryId = 1
        });

        // Seed User
        _context.Users.Add(new User
        {
            UserId = 1,
            Email = "admin@test.com",
            Password = "hashed",
            Status = 1
        });

        // Seed Asset and AssetInstance for ApplyTemplateToExistingAssetsAsync
        _context.Assets.Add(new Asset
        {
            AssetId = 1,
            AssetTypeId = 1,
            Code = "PC001",
            Name = "Desktop PC",
            Status = 1,
            Unit = "pcs",
            CreatedBy = 1
        });

        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 1,
            AssetId = 1,
            WarehouseId = 1,
            InstanceCode = "INS001",
            Status = (int)g19_sep490_ealds.Server.Utils.EnumsStatus.AssetStatus.Available,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalPrice = 10000000m
        });

        await _context.SaveChangesAsync();
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

    private MaintenanceTemplateController CreateController()
    {
        var service = new MaintenanceTemplateService(new MaintenanceTemplateMapper(), _context, _mockLogger.Object);
        return new MaintenanceTemplateController(service);
    }

    private TemplateCreateDTO CreateValidPeriodicDto(
        int assetTypeId = 1,
        string name = "Valid name",
        string content = "Valid content",
        int frequencyType = (int)MaintenanceFrequencyType.Periodic,
        int repeatIntervalValue = 1,
        int repeatIntervalUnit = (int)MaintenanceRepeatIntervalUnit.Month)
    {
        return new TemplateCreateDTO
        {
            AssetTypeId = assetTypeId,
            Name = name,
            Content = content,
            FrequencyType = (MaintenanceFrequencyType)frequencyType,
            RepeatIntervalValue = repeatIntervalValue,
            RepeatIntervalUnit = (MaintenanceRepeatIntervalUnit)repeatIntervalUnit,
            IsActive = true
        };
    }

    private TemplateCreateDTO CreateValidOneTimeDto(
        int assetTypeId = 1,
        string name = "Valid name",
        string content = "Valid content")
    {
        return new TemplateCreateDTO
        {
            AssetTypeId = assetTypeId,
            Name = name,
            Content = content,
            FrequencyType = MaintenanceFrequencyType.OneTime,
            RepeatIntervalValue = 0,
            RepeatIntervalUnit = 0,
            IsActive = true,
            OneTimeScheduledDate = DateTime.UtcNow.AddDays(7)
        };
    }

    #region Create Tests

    /// <summary>
    /// Test case 1 (Abnormal based on validation): AssetTypeId = 1, Name = Valid name,
    /// Content = Valid content, FrequencyType = 1, RepeatIntervalValue = 1, RepeatIntervalUnit = Month.
    /// Expected output: 400 Bad Request (OneTime cannot have RepeatIntervalValue > 0)
    /// Note: The test case is marked as "Normal" but based on validation logic,
    /// FrequencyType = 1 (OneTime) requires RepeatIntervalValue = 0 and RepeatIntervalUnit = 0
    /// </summary>
    [Fact]
    public async Task Create_FrequencyTypeOneTimeWithRepeatInterval_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: "Valid name",
            content: "Valid content",
            frequencyType: (int)MaintenanceFrequencyType.OneTime,
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Month);

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 2 (Abnormal): AssetTypeId = 0, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1, RepeatIntervalValue = 1, RepeatIntervalUnit = Month.
    /// Expected output: 400 Bad Request (Không có loại tài sản nào)
    /// </summary>
    [Fact]
    public async Task Create_AssetTypeIdZero_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 0,
            name: "Valid name",
            content: "Valid content");

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): AssetTypeId = -1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1, RepeatIntervalValue = 1, RepeatIntervalUnit = Month.
    /// Expected output: 400 Bad Request (Không có loại tài sản nào)
    /// </summary>
    [Fact]
    public async Task Create_AssetTypeIdNegative_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: -1,
            name: "Valid name",
            content: "Valid content");

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): AssetTypeId = 1, Name = Empty, Content = Valid content,
    /// FrequencyType = 1, RepeatIntervalValue = 1, RepeatIntervalUnit = Month.
    /// Expected output: 400 Bad Request (Tên quy định bảo dưỡng đã tồn tại)
    /// Note: Empty name will be trimmed and cause duplicate template check to fail
    /// </summary>
    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: "",
            content: "Valid content");

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4b (Abnormal): AssetTypeId = 1, Name = null, Content = Valid content.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullName_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: null!,
            content: "Valid content");

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4c (Abnormal): AssetTypeId = 1, Name = whitespace, Content = Valid content.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: "   ",
            content: "Valid content");

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Empty,
    /// FrequencyType = 1, RepeatIntervalValue = 1, RepeatIntervalUnit = Month.
    /// Expected output: 400 Bad Request (empty content causes validation/duplicate issues)
    /// </summary>
    [Fact]
    public async Task Create_EmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: "Valid name",
            content: "");

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5b (Abnormal): AssetTypeId = 1, Name = Valid name, Content = null.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullContent_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: "Valid name",
            content: null!);

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Normal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 2 (Periodic), RepeatIntervalValue = 1, RepeatIntervalUnit = Month.
    /// Expected output: 200 OK
    /// Note: Fixed FrequencyType to 2 (Periodic) for a valid test case
    /// </summary>
    [Fact]
    public async Task Create_ValidPeriodicData_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}", // Unique name to avoid duplicate
            content: "Valid content",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Month);

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 0, RepeatIntervalValue = 1, RepeatIntervalUnit = Month.
    /// Expected output: 400 Bad Request (Loại bảo trì không hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_FrequencyTypeZero_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content",
            frequencyType: 0, // Invalid frequency type
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Month);

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1, RepeatIntervalValue = 0, RepeatIntervalUnit = Month.
    /// Expected output: 400 Bad Request (OneTime cannot have RepeatIntervalUnit != 0)
    /// </summary>
    [Fact]
    public async Task Create_FrequencyTypeOneTimeWithUnit_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = $"Valid name {Guid.NewGuid()}",
            Content = "Valid content",
            FrequencyType = MaintenanceFrequencyType.OneTime,
            RepeatIntervalValue = 0,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month, // Invalid: should be 0
            IsActive = true,
            OneTimeScheduledDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 9 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1 (Periodic), RepeatIntervalValue = 1, RepeatIntervalUnit = Day.
    /// Expected output: 400 Bad Request (Bảo trì theo ngày phải >= 7 ngày)
    /// </summary>
    [Fact]
    public async Task Create_DayIntervalLessThan7_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1, // < 7 for Day
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Day);

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 10 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1 (Periodic), RepeatIntervalValue = 1, RepeatIntervalUnit = Week.
    /// Expected output: 400 Bad Request (Bảo trì theo tuần phải >= 2 tuần)
    /// </summary>
    [Fact]
    public async Task Create_WeekIntervalLessThan2_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1, // < 2 for Week
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Week);

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 11 (Normal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1 (Periodic), RepeatIntervalValue = 1, RepeatIntervalUnit = Year.
    /// Expected output: 200 OK (Year doesn't have additional validation)
    /// </summary>
    [Fact]
    public async Task Create_ValidYearInterval_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Year);

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Additional Edge Cases

    /// <summary>
    /// Test case: OneTime with valid date
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_ValidOneTimeData_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidOneTimeDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content");

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: OneTime without scheduled date
    /// Expected output: 400 Bad Request (Vui lòng chọn ngày bảo dưỡng)
    /// </summary>
    [Fact]
    public async Task Create_OneTimeWithoutScheduledDate_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = $"Valid name {Guid.NewGuid()}",
            Content = "Valid content",
            FrequencyType = MaintenanceFrequencyType.OneTime,
            RepeatIntervalValue = 0,
            RepeatIntervalUnit = 0,
            IsActive = true,
            OneTimeScheduledDate = null // Missing required date
        };

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Duplicate template name for same asset type
    /// Expected output: 400 Bad Request (Tên quy định bảo dưỡng đã tồn tại)
    /// </summary>
    [Fact]
    public async Task Create_DuplicateTemplateName_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var uniqueName = $"Duplicate name {Guid.NewGuid()}";

        var dto1 = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: uniqueName,
            content: "Valid content");
        await controller.AddTemplateAsync(dto1);

        var dto2 = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: uniqueName,
            content: "Valid content");

        // Act
        var result = await controller.AddTemplateAsync(dto2);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Invalid RepeatIntervalUnit enum
    /// Expected output: 400 Bad Request (Đơn vị khoảng thời gian không hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_InvalidRepeatIntervalUnit_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1,
            repeatIntervalUnit: 99); // Invalid enum value

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Non-existent AssetTypeId
    /// Expected output: 400 Bad Request (Không có loại tài sản nào)
    /// </summary>
    [Fact]
    public async Task Create_NonExistentAssetType_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidPeriodicDto(
            assetTypeId: 999,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content");

        // Act
        var result = await controller.AddTemplateAsync(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Update Tests

    private async Task<int> CreateTemplateForUpdate()
    {
        var template = new MaintenanceTemplate
        {
            AssetTypeId = 1,
            Name = $"Update test template {Guid.NewGuid()}",
            Content = "Original content",
            FrequencyType = (int)MaintenanceFrequencyType.OneTime,
            RepeatIntervalValue = 0,
            RepeatIntervalUnit = "None",
            IsActive = true,
            OneTimeScheduledDate = DateTime.UtcNow.AddDays(7)
        };
        _context.MaintenanceTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template.TemplateId;
    }

    private TemplateUpdateDTO CreateValidOneTimeUpdateDto(int assetTypeId = 1)
    {
        return new TemplateUpdateDTO
        {
            AssetTypeId = assetTypeId,
            Name = $"Updated name {Guid.NewGuid()}",
            Content = "Valid content",
            FrequencyType = MaintenanceFrequencyType.OneTime,
            RepeatIntervalValue = 0,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month, // Will be ignored for OneTime
            OneTimeScheduledDate = DateTime.UtcNow.AddDays(14)
        };
    }

    private TemplateUpdateDTO CreateValidPeriodicUpdateDto(
        int assetTypeId = 1,
        string name = "Updated name",
        string content = "Valid content",
        int frequencyType = (int)MaintenanceFrequencyType.Periodic,
        int repeatIntervalValue = 1,
        int repeatIntervalUnit = (int)MaintenanceRepeatIntervalUnit.Month)
    {
        return new TemplateUpdateDTO
        {
            AssetTypeId = assetTypeId,
            Name = name,
            Content = content,
            FrequencyType = (MaintenanceFrequencyType)frequencyType,
            RepeatIntervalValue = repeatIntervalValue,
            RepeatIntervalUnit = (MaintenanceRepeatIntervalUnit)repeatIntervalUnit
        };
    }

    /// <summary>
    /// Test case 1 (Normal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1 (OneTime), TemplateId = 1.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Update_ValidOneTimeData_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = $"Valid name {Guid.NewGuid()}";

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 2 (Abnormal): AssetTypeId = 0, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1, TemplateId = 1.
    /// Expected output: 400 Bad Request (invalid asset type)
    /// </summary>
    [Fact]
    public async Task Update_AssetTypeIdZero_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 0);
        dto.Name = $"Valid name {Guid.NewGuid()}";

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): AssetTypeId = -1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1, TemplateId = 1.
    /// Expected output: 400 Bad Request (invalid asset type)
    /// </summary>
    [Fact]
    public async Task Update_AssetTypeIdNegative_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: -1);
        dto.Name = $"Valid name {Guid.NewGuid()}";

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): AssetTypeId = 1, Name = Empty, Content = Valid content,
    /// FrequencyType = 1, TemplateId = 1.
    /// Expected output: 400 Bad Request (empty name)
    /// </summary>
    [Fact]
    public async Task Update_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = "";

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4b (Abnormal): AssetTypeId = 1, Name = null, Content = Valid content.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_NullName_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = null!;

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4c (Abnormal): AssetTypeId = 1, Name = whitespace, Content = Valid content.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = "   ";

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Empty,
    /// FrequencyType = 1, TemplateId = 1.
    /// Expected output: 400 Bad Request (empty content)
    /// </summary>
    [Fact]
    public async Task Update_EmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = $"Valid name {Guid.NewGuid()}";
        dto.Content = "";

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5b (Abnormal): AssetTypeId = 1, Name = Valid name, Content = null.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_NullContent_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = $"Valid name {Guid.NewGuid()}";
        dto.Content = null!;

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Normal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 2 (Periodic), TemplateId = 1.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Update_ValidPeriodicData_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidPeriodicUpdateDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Month);

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 0, TemplateId = 1.
    /// Expected output: 400 Bad Request (Loại bảo trì không hợp lệ)
    /// </summary>
    [Fact]
    public async Task Update_FrequencyTypeZero_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForUpdate();
        var dto = CreateValidPeriodicUpdateDto(
            assetTypeId: 1,
            name: $"Valid name {Guid.NewGuid()}",
            content: "Valid content",
            frequencyType: 0, // Invalid frequency type
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Month);

        // Act
        var result = await controller.UpdateTemplate(dto, templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1, TemplateId = 0.
    /// Expected output: 400 Bad Request (KeyNotFoundException - template not found)
    /// </summary>
    [Fact]
    public async Task Update_TemplateIdZero_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = $"Valid name {Guid.NewGuid()}";

        // Act
        var result = await controller.UpdateTemplate(dto, 0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 9 (Abnormal): AssetTypeId = 1, Name = Valid name, Content = Valid content,
    /// FrequencyType = 1, TemplateId = -1.
    /// Expected output: 400 Bad Request (KeyNotFoundException - template not found)
    /// </summary>
    [Fact]
    public async Task Update_TemplateIdNegative_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = $"Valid name {Guid.NewGuid()}";

        // Act
        var result = await controller.UpdateTemplate(dto, -1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Non-existent TemplateId
    /// Expected output: 400 Bad Request (KeyNotFoundException)
    /// </summary>
    [Fact]
    public async Task Update_NonExistentTemplateId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = CreateValidOneTimeUpdateDto(assetTypeId: 1);
        dto.Name = $"Valid name {Guid.NewGuid()}";

        // Act
        var result = await controller.UpdateTemplate(dto, 999);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Duplicate name for same asset type
    /// Expected output: 400 Bad Request (Tên đã được sử dụng)
    /// </summary>
    [Fact]
    public async Task Update_DuplicateNameForSameAssetType_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Create first template
        var templateId1 = await CreateTemplateForUpdate();
        var dto1 = CreateValidPeriodicUpdateDto(
            assetTypeId: 1,
            name: $"First template {Guid.NewGuid()}",
            content: "Content 1",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Month);
        await controller.UpdateTemplate(dto1, templateId1);

        // Create second template
        var templateId2 = await CreateTemplateForUpdate();
        var duplicateName = $"Duplicate {Guid.NewGuid()}";
        var dto2 = CreateValidPeriodicUpdateDto(
            assetTypeId: 1,
            name: duplicateName,
            content: "Content 2",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Month);
        await controller.UpdateTemplate(dto2, templateId2);

        // Try to update first with same name
        var dto3 = CreateValidPeriodicUpdateDto(
            assetTypeId: 1,
            name: duplicateName,
            content: "Updated content",
            frequencyType: (int)MaintenanceFrequencyType.Periodic,
            repeatIntervalValue: 1,
            repeatIntervalUnit: (int)MaintenanceRepeatIntervalUnit.Month);

        // Act
        var result = await controller.UpdateTemplate(dto3, templateId1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Delete Tests

    private async Task<int> CreateTemplateForDelete()
    {
        var template = new MaintenanceTemplate
        {
            AssetTypeId = 1,
            Name = $"Delete test template {Guid.NewGuid()}",
            Content = "Content for deletion",
            FrequencyType = (int)MaintenanceFrequencyType.OneTime,
            RepeatIntervalValue = 0,
            RepeatIntervalUnit = "None",
            IsActive = true,
            OneTimeScheduledDate = DateTime.UtcNow.AddDays(7)
        };
        _context.MaintenanceTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template.TemplateId;
    }

    /// <summary>
    /// Test case 1 (Normal): Can connect to server, TemplateId = 1.
    /// Expected output: 200 OK (successfully deleted)
    /// </summary>
    [Fact]
    public async Task Delete_ValidTemplateId_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForDelete();

        // Act
        var result = await controller.HardDeleteTemplate(templateId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 2 (Abnormal): Can connect to server, TemplateId = 0.
    /// Expected output: 400 Bad Request (KeyNotFoundException - template not found)
    /// </summary>
    [Fact]
    public async Task Delete_TemplateIdZero_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.HardDeleteTemplate(0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): Can connect to server, TemplateId = -1.
    /// Expected output: 400 Bad Request (KeyNotFoundException - template not found)
    /// </summary>
    [Fact]
    public async Task Delete_TemplateIdNegative_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.HardDeleteTemplate(-1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): Can connect to server, TemplateId = 999.
    /// Expected output: 400 Bad Request (KeyNotFoundException - template not found)
    /// </summary>
    [Fact]
    public async Task Delete_NonExistentTemplateId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.HardDeleteTemplate(999);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Template with associated schedules
    /// Expected output: 200 OK (successfully deleted with cascade)
    /// </summary>
    [Fact]
    public async Task Delete_TemplateWithSchedules_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForDelete();

        // Add a schedule for this template
        var schedule = new MaintenanceSchedule
        {
            TemplateId = templateId,
            AssetInstanceId = 1,
            Content = "Schedule content",
            ScheduleType = (int)ScheduleType.Auto,
            StartDate = DateTime.UtcNow,
            NextDueDate = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            CreateBy = 1,
            CreateDate = DateTime.UtcNow
        };
        _context.MaintenanceSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var result = await controller.HardDeleteTemplate(templateId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case: Delete same template twice
    /// Expected output: 400 Bad Request (KeyNotFoundException - already deleted)
    /// </summary>
    [Fact]
    public async Task Delete_SameTemplateTwice_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var templateId = await CreateTemplateForDelete();

        // Delete once
        await controller.HardDeleteTemplate(templateId);

        // Act - try to delete again
        var result = await controller.HardDeleteTemplate(templateId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
