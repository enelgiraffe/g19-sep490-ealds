using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Profile;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class ProfileControllerTests
{
    private readonly Mock<IProfileService> _mockService;
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        _mockService = new Mock<IProfileService>();
        _controller = new ProfileController(_mockService.Object);
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

    private void SetNoUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private static UserProfileDto MakeProfile(string name = "John Doe", int id = 1)
        => new UserProfileDto
        {
            Id = id,
            Email = "test@example.com",
            Name = name,
            EmployeeCode = "EMP001",
            Phone = "1234567890",
            Address = "123 Main St",
            Dob = new DateOnly(1990, 1, 1),
            Gender = 1,
            ImageUrl = "https://example.com/image.jpg",
            DepartmentName = "IT Department",
            DepartmentId = 1,
            Role = "User",
            IsDepartmentHead = false
        };

    #region GetProfile Tests

    [Fact]
    public async Task GetProfile_WithValidUser_ReturnsOk()
    {
        _mockService.Setup(s => s.GetProfileAsync(1)).ReturnsAsync(MakeProfile());

        SetUserClaim(1);
        var result = await _controller.GetProfile();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.Equal(1, profile.Id);
        Assert.Equal("test@example.com", profile.Email);
        Assert.Equal("John Doe", profile.Name);
        Assert.Equal("EMP001", profile.EmployeeCode);
        Assert.Equal("1234567890", profile.Phone);
        Assert.Equal("123 Main St", profile.Address);
        Assert.Equal("IT Department", profile.DepartmentName);
    }

    [Fact]
    public async Task GetProfile_WithNoEmployeeRecord_ReturnsOkWithEmailAsName()
    {
        _mockService.Setup(s => s.GetProfileAsync(1)).ReturnsAsync(new UserProfileDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "test@example.com",
            Role = "User"
        });

        SetUserClaim(1);
        var result = await _controller.GetProfile();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.Equal("test@example.com", profile.Name);
        Assert.Null(profile.EmployeeCode);
        Assert.Null(profile.Phone);
        Assert.Null(profile.Address);
    }

    [Fact]
    public async Task GetProfile_WithNonExistentUser_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetProfileAsync(999))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        SetUserClaim(999);
        var result = await _controller.GetProfile();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetProfile_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetNoUser();
        var result = await _controller.GetProfile();

        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        _mockService.Setup(s => s.UpdateProfileAsync(1, It.IsAny<UpdateProfileRequestDto>()))
            .ReturnsAsync(new UserProfileDto
            {
                Id = 1,
                Email = "test@example.com",
                Name = "New Name",
                Phone = "9876543210",
                Address = "New Address",
                Dob = new DateOnly(1995, 5, 15),
                Gender = 1,
                ImageUrl = "https://example.com/image.jpg",
                Role = "User"
            });

        SetUserClaim(1);
        var request = new UpdateProfileRequestDto
        {
            Name = "New Name",
            Phone = "9876543210",
            Address = "New Address",
            Dob = new DateOnly(1995, 5, 15),
            Gender = 1,
            ImageUrl = "https://example.com/image.jpg"
        };

        var result = await _controller.UpdateProfile(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.Equal("New Name", profile.Name);
        Assert.Equal("9876543210", profile.Phone);
        Assert.Equal("New Address", profile.Address);
        Assert.Equal(new DateOnly(1995, 5, 15), profile.Dob);
        Assert.Equal(1, profile.Gender);
        Assert.Equal("https://example.com/image.jpg", profile.ImageUrl);
    }

    [Fact]
    public async Task UpdateProfile_WithNoEmployeeRecord_ReturnsNotFound()
    {
        _mockService.Setup(s => s.UpdateProfileAsync(1, It.IsAny<UpdateProfileRequestDto>()))
            .ThrowsAsync(new KeyNotFoundException("Employee record not found for user"));

        SetUserClaim(1);
        var request = new UpdateProfileRequestDto { Name = "New Name", Phone = "9876543210" };

        var result = await _controller.UpdateProfile(request);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetNoUser();
        var request = new UpdateProfileRequestDto { Name = "New Name" };

        var result = await _controller.UpdateProfile(request);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidModel_ReturnsBadRequest()
    {
        SetUserClaim(1);
        _controller.ModelState.AddModelError("Name", "Name is required.");

        var request = new UpdateProfileRequestDto { Name = "" };

        var result = await _controller.UpdateProfile(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_WithPartialData_ReturnsOk()
    {
        _mockService.Setup(s => s.UpdateProfileAsync(1, It.IsAny<UpdateProfileRequestDto>()))
            .ReturnsAsync(new UserProfileDto
            {
                Id = 1,
                Email = "test@example.com",
                Name = "Updated Name",
                Role = "User"
            });

        SetUserClaim(1);
        var request = new UpdateProfileRequestDto { Name = "Updated Name" };

        var result = await _controller.UpdateProfile(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(okResult.Value);
        Assert.Equal("Updated Name", profile.Name);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        _mockService.Setup(s => s.ChangePasswordAsync(1, It.IsAny<ChangePasswordRequestDto>()))
            .Returns(Task.CompletedTask);

        SetUserClaim(1);
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "oldpass",
            NewPassword = "newpass123"
        };

        var result = await _controller.ChangePassword(request);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.ChangePasswordAsync(1, It.IsAny<ChangePasswordRequestDto>()))
            .ThrowsAsync(new Exception("Current password is incorrect"));

        SetUserClaim(1);
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "wrongpass",
            NewPassword = "newpass123"
        };

        var result = await _controller.ChangePassword(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetNoUser();
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "oldpass",
            NewPassword = "newpass123"
        };

        var result = await _controller.ChangePassword(request);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WithWeakPassword_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.ChangePasswordAsync(1, It.IsAny<ChangePasswordRequestDto>()))
            .ThrowsAsync(new ArgumentException("Password must be at least 6 characters"));

        SetUserClaim(1);
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "oldpass",
            NewPassword = "123"
        };

        var result = await _controller.ChangePassword(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
