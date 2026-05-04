using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Allocation;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AllocationRequestsControllerTests
{
    private readonly Mock<IAllocationRequestService> _mockService = null!;
    private readonly AllocationRequestsController _controller;

    public AllocationRequestsControllerTests()
    {
        _mockService = new Mock<IAllocationRequestService>();
        _controller = new AllocationRequestsController(_mockService.Object);
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

    private CreateDepartmentAllocationRequestDto CreateValidDto()
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
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ReturnsAsync(42);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.Equal(200, okResult.StatusCode);
    }

    /// <summary>
    /// Test case 2 (Abnormal): Title = Empty, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Nhập tiêu đề yêu cầu)
    /// </summary>
    [Fact]
    public async Task Create_EmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = "";
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Nhập tiêu đề yêu cầu"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequest = (BadRequestObjectResult)result;
        Assert.Equal(400, badRequest.StatusCode);
    }

    /// <summary>
    /// Test case 2b (Abnormal): Title = null, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullTitle_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = null!;
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Nhập tiêu đề yêu cầu"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 2c (Abnormal): Title = whitespace, AssetType = 1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WhitespaceTitle_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Title = "   ";
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Nhập tiêu đề yêu cầu"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): Title = Valid title, AssetType = 0, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetTypeIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetTypeId = 0;
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Dòng 1: chọn loại, tài sản và số lượng hợp lệ"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): Title = Valid title, AssetType = -1, Asset = 1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetTypeIdNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetTypeId = -1;
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Dòng 1: chọn loại, tài sản và số lượng hợp lệ"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal): Title = Valid title, AssetType = 1, Asset = 0, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetIdZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetId = 0;
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Dòng 1: chọn loại, tài sản và số lượng hợp lệ"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Abnormal): Title = Valid title, AssetType = 1, Asset = -1, Quantity = 1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_AssetIdNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].AssetId = -1;
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Dòng 1: chọn loại, tài sản và số lượng hợp lệ"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): Title = Valid title, AssetType = 1, Asset = 1, Quantity = 0.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_QuantityZero_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].Quantity = 0;
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Dòng 1: chọn loại, tài sản và số lượng hợp lệ"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): Title = Valid title, AssetType = 1, Asset = 1, Quantity = -1.
    /// Expected output: 400 Bad Request (Dòng 1: chọn loại, tài sản và số lượng hợp lệ)
    /// </summary>
    [Fact]
    public async Task Create_QuantityNegative_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = CreateValidDto();
        dto.Lines[0].Quantity = -1;
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Dòng 1: chọn loại, tài sản và số lượng hợp lệ"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
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
        // Arrange
        SetUserClaim(1);
        var dto = new CreateDepartmentAllocationRequestDto
        {
            Title = "Valid Title",
            Lines = new List<AllocationLineInputDto>()
        };
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Cần ít nhất một dòng tài sản"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Lines is null
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullLines_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        var dto = new CreateDepartmentAllocationRequestDto
        {
            Title = "Valid Title",
            Lines = null!
        };
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Cần ít nhất một dòng tài sản"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: DTO is null
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_NullDto_ReturnsBadRequest()
    {
        // Arrange
        SetUserClaim(1);
        _mockService
            .Setup(x => x.CreateAsync(1, null!))
            .ThrowsAsync(new Exception("Nhập tiêu đề yêu cầu"));

        // Act
        var result = await _controller.Create(null!);

        // Assert
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
        _mockService
            .Setup(x => x.CreateAsync(2, dto))
            .ThrowsAsync(new UnauthorizedAccessException());

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
        _mockService
            .Setup(x => x.CreateAsync(1, dto))
            .ThrowsAsync(new Exception("Tài khoản chưa gắn phòng ban"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
