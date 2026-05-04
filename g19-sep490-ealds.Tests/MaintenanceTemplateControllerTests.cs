using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Maintenance;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class MaintenanceTemplateControllerTests
{
    private readonly Mock<IMaintenanceTemplateService> _mockService;
    private readonly MaintenanceTemplateController _controller;

    public MaintenanceTemplateControllerTests()
    {
        _mockService = new Mock<IMaintenanceTemplateService>();
        _controller = new MaintenanceTemplateController(_mockService.Object);
        SetUserClaim(1);
    }

    private void SetUserClaim(int userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    private MaintenanceTemplateResponseDTO MakeResponse(string name = "Test Template", int assetTypeId = 1)
        => new MaintenanceTemplateResponseDTO
        {
            TemplateId = 1,
            AssetTypeId = assetTypeId,
            Name = name,
            Content = "Test content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

    #region Create Tests

    [Fact]
    public async Task Create_ValidPeriodicData_ReturnsOk()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ReturnsAsync(MakeResponse());

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = "Valid Periodic Template",
            Content = "Valid content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_FrequencyTypeOneTimeWithRepeatInterval_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("OneTime cannot have repeat interval"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = "OneTime with interval",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.OneTime,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_AssetTypeIdZero_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Asset type is required"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 0,
            Name = "Template",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_AssetTypeIdNegative_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Asset type is required"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = -1,
            Name = "Template",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Template name is required"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = "",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_NullName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Template name is required"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = null!,
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhitespaceName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Template name is required"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = "   ",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_EmptyContent_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Content is required"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = "Template",
            Content = "",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_OneTimeWithoutScheduledDate_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Scheduled date is required for OneTime template"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = "OneTime No Date",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.OneTime,
            RepeatIntervalValue = 0,
            RepeatIntervalUnit = 0,
            IsActive = true,
            OneTimeScheduledDate = null
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_DuplicateTemplateName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateTemplateAsync(It.IsAny<TemplateCreateDTO>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Template name already exists"));

        var dto = new TemplateCreateDTO
        {
            AssetTypeId = 1,
            Name = "Duplicate",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month,
            IsActive = true
        };

        var result = await _controller.AddTemplateAsync(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidPeriodicData_ReturnsOk()
    {
        _mockService.Setup(s => s.UpdatTemplateAsync(It.IsAny<int>(), It.IsAny<TemplateUpdateDTO>()))
            .ReturnsAsync(MakeResponse());

        var dto = new TemplateUpdateDTO
        {
            AssetTypeId = 1,
            Name = "Updated Template",
            Content = "Updated content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month
        };

        var result = await _controller.UpdateTemplate(dto, 1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_EmptyContent_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdatTemplateAsync(It.IsAny<int>(), It.IsAny<TemplateUpdateDTO>()))
            .ThrowsAsync(new ArgumentException("Content is required"));

        var dto = new TemplateUpdateDTO
        {
            AssetTypeId = 1,
            Name = "Updated",
            Content = "",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month
        };

        var result = await _controller.UpdateTemplate(dto, 1);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_TemplateIdNegative_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdatTemplateAsync(It.IsAny<int>(), It.IsAny<TemplateUpdateDTO>()))
            .ThrowsAsync(new ArgumentException("Invalid template ID"));

        var dto = new TemplateUpdateDTO
        {
            AssetTypeId = 1,
            Name = "Updated",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month
        };

        var result = await _controller.UpdateTemplate(dto, -1);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_NullName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdatTemplateAsync(It.IsAny<int>(), It.IsAny<TemplateUpdateDTO>()))
            .ThrowsAsync(new ArgumentException("Template name is required"));

        var dto = new TemplateUpdateDTO
        {
            AssetTypeId = 1,
            Name = null!,
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month
        };

        var result = await _controller.UpdateTemplate(dto, 1);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ValidPeriodicData_ThrowsKeyNotFound_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdatTemplateAsync(It.IsAny<int>(), It.IsAny<TemplateUpdateDTO>()))
            .ThrowsAsync(new KeyNotFoundException("Template not found"));

        var dto = new TemplateUpdateDTO
        {
            AssetTypeId = 1,
            Name = "Updated",
            Content = "Content",
            FrequencyType = MaintenanceFrequencyType.Periodic,
            RepeatIntervalValue = 1,
            RepeatIntervalUnit = MaintenanceRepeatIntervalUnit.Month
        };

        var result = await _controller.UpdateTemplate(dto, 999);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Toggle / Change Status Tests

    [Fact]
    public async Task ToggleStatus_ValidId_ReturnsOk()
    {
        _mockService.Setup(s => s.ToggleTemplateStatusAsync(It.IsAny<int>()))
            .ReturnsAsync(MakeResponse());

        var result = await _controller.ToggleStatusAsync(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ToggleStatus_InvalidId_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.ToggleTemplateStatusAsync(It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Invalid template ID"));

        var result = await _controller.ToggleStatusAsync(-1);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsOk()
    {
        _mockService.Setup(s => s.HardDeleteTemplateAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        var result = await _controller.HardDeleteTemplate(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_SameTemplateTwice_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.HardDeleteTemplateAsync(It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Template not found"));

        var result = await _controller.HardDeleteTemplate(1);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_TemplateIdZero_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.HardDeleteTemplateAsync(It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Invalid template ID"));

        var result = await _controller.HardDeleteTemplate(0);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_TemplateIdNegative_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.HardDeleteTemplateAsync(It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Invalid template ID"));

        var result = await _controller.HardDeleteTemplate(-1);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
