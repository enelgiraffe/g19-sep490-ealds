using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Auth;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AuthControllerResetPasswordTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerResetPasswordTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        _controller = new AuthController(_mockAuthService.Object, mockEnv.Object);
    }

    /// <summary>
    /// Test case: Reset password with valid token
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsOk()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequestDto>()))
            .Returns(Task.CompletedTask);

        var request = new ResetPasswordRequestDto
        {
            Token = "validToken123",
            NewPassword = "newPassword456",
            ConfirmNewPassword = "newPassword456"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: Reset password with invalid/expired token
    /// Expected output: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequestDto>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        var request = new ResetPasswordRequestDto
        {
            Token = "expiredToken",
            NewPassword = "newPassword456",
            ConfirmNewPassword = "newPassword456"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    /// <summary>
    /// Test case: Reset password with token that doesn't exist in database
    /// Expected output: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithNonExistentToken_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequestDto>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        var request = new ResetPasswordRequestDto
        {
            Token = "nonExistentToken",
            NewPassword = "newPassword456",
            ConfirmNewPassword = "newPassword456"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    /// <summary>
    /// Test case: Reset password with invalid email format
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequestDto
        {
            Token = "validToken123",
            NewPassword = "newPassword456",
            ConfirmNewPassword = "differentPassword"
        };
        _controller.ModelState.AddModelError("ConfirmNewPassword", "Xác nhận mật khẩu không khớp.");

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Reset password with password too short
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequestDto>()))
            .ThrowsAsync(new ArgumentException("Password must be at least 6 characters"));

        var request = new ResetPasswordRequestDto
        {
            Token = "validToken123",
            NewPassword = "123",
            ConfirmNewPassword = "123"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Reset password clears any existing refresh token
    /// Expected: Service method is called (invalidation happens at service level)
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithValidToken_CallsService()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequestDto>()))
            .Returns(Task.CompletedTask);

        var request = new ResetPasswordRequestDto
        {
            Token = "validToken123",
            NewPassword = "newPassword456",
            ConfirmNewPassword = "newPassword456"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockAuthService.Verify(
            s => s.ResetPasswordAsync(It.Is<ResetPasswordRequestDto>(r => r.Token == "validToken123")),
            Times.Once);
    }
}
