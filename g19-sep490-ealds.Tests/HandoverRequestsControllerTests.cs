using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Allocation;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class HandoverRequestsControllerTests
{
    private readonly Mock<IHandoverRequestService> _mockService = null!;
    private readonly HandoverRequestsController _controller;

    public HandoverRequestsControllerTests()
    {
        _mockService = new Mock<IHandoverRequestService>();
        _controller = new HandoverRequestsController(_mockService.Object);
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

    private void SetUserWithoutClaim()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private static CreateDepartmentAllocationRequestDto CreateValidDto()
    {
        return new CreateDepartmentAllocationRequestDto
        {
            Title = "Valid Title",
            Lines = new List<AllocationLineInputDto>
            {
                new AllocationLineInputDto
                {
                    AssetTypeId = 1,
                    AssetId = 1,
                    Quantity = 1
                }
            }
        };
    }

    #region Create Tests

    /// <summary>
    /// Test case 1 (Normal): Title = Valid title, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_ValidData_ReturnsOk()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(100);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 2 (Abnormal): Title = Empty, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Nhập tiêu đề yêu cầu)
    /// </summary>
    [Fact]
    public async Task Create_EmptyTitle_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = "";
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("Title is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 2b (Abnormal): Title = null, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullTitle_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = null!;
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("Title is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 2c (Abnormal): Title = whitespace, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WhitespaceTitle_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = "   ";
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("Title is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): Title = Valid title, AssetType = 0, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetTypeIdZero_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetTypeId = 0;
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("AssetTypeId must be greater than 0"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): Title = Valid title, AssetType = -1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetTypeIdNegative_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetTypeId = -1;
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("AssetTypeId must be greater than 0"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal): Title = Valid title, AssetType = 1, Asset = 0, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetIdZero_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetId = 0;
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("AssetId must be greater than 0"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Abnormal): Title = Valid title, AssetType = 1, Asset = -1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetIdNegative_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetId = -1;
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("AssetId must be greater than 0"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): Title = Valid title, AssetType = 1, Asset = 1, Quantity = 0.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_QuantityZero_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].Quantity = 0;
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("Quantity must be greater than 0"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): Title = Valid title, AssetType = 1, Asset = 1, Quantity = -1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_QuantityNegative_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].Quantity = -1;
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("Quantity must be greater than 0"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Additional Edge Cases

    /// <summary>
    /// Test case: Lines is empty
    /// Expected output: 400 Bad Request (Cần ít nhất một dòng tài sản)
    /// </summary>
    [Fact]
    public async Task Create_EmptyLines_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = new CreateDepartmentAllocationRequestDto
        {
            Title = "Valid Title",
            Lines = new List<AllocationLineInputDto>()
        };
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("At least one allocation line is required"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Lines is null
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullLines_ReturnsBadRequest()
    {
        SetUserClaim(1);
        var dto = new CreateDepartmentAllocationRequestDto
        {
            Title = "Valid Title",
            Lines = null!
        };
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("Lines cannot be null"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: DTO is null
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullDto_ReturnsBadRequest()
    {
        SetUserClaim(1);
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentAllocationRequestDto>()))
            .ThrowsAsync(new ArgumentException("Request body is required"));

        var result = await _controller.Create(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Without user claim (unauthorized)
    /// Expected output: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task Create_WithoutUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        SetUserWithoutClaim();
        var dto = CreateValidDto();

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Test case: User without department head role
    /// Expected output: 403 Forbidden
    /// </summary>
    [Fact]
    public async Task Create_UserNotDepartmentHead_ReturnsForbidden()
    {
        // Arrange
        SetUserClaim(2);
        var dto = CreateValidDto();
        _mockService.Setup(s => s.CreateAsync(2, dto))
            .ThrowsAsync(new UnauthorizedAccessException("Không có quyền trưởng phòng"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    /// <summary>
    /// Test case: User without employee/department association
    /// Expected output: 400 Bad Request (Tài khoản chưa gắn phòng ban)
    /// </summary>
    [Fact]
    public async Task Create_UserWithoutDepartment_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        _mockService.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Tài khoản chưa gắn phòng ban"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
