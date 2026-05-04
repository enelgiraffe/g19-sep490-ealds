using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Auth;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AuthControllerForgotPasswordTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerForgotPasswordTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        _controller = new AuthController(_mockAuthService.Object, mockEnv.Object);
    }

    /// <summary>
    /// Test case: Forgot password with valid email that exists
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsOk()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequestDto>()))
            .Returns(Task.CompletedTask);

        var request = new ForgotPasswordRequestDto { Email = "test@example.com" };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case: Forgot password with email that does not exist
    /// Expected output: 400 Bad Request (service throws InvalidOperationException)
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ReturnsBadRequest()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Email không tồn tại trong hệ thống."));

        var request = new ForgotPasswordRequestDto { Email = "nonexistent@example.com" };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Forgot password with email of disabled user
    /// Expected output: 400 Bad Request (service throws InvalidOperationException)
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithDisabledUser_ReturnsBadRequest()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Tài khoản đã bị vô hiệu hóa."));

        var request = new ForgotPasswordRequestDto { Email = "disabled@example.com" };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Forgot password with invalid email format
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto { Email = "not-a-valid-email" };
        _controller.ModelState.AddModelError("Email", "Email không hợp lệ.");

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Forgot password when email service fails
    /// Expected output: 500 Internal Server Error (in development mode)
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WhenEmailServiceFails_ReturnsInternalServerError()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequestDto>()))
            .ThrowsAsync(new Exception("SMTP error"));

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = new AuthController(_mockAuthService.Object, mockEnv.Object);

        var request = new ForgotPasswordRequestDto { Email = "test@example.com" };

        // Act
        var result = await controller.ForgotPassword(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    /// <summary>
    /// Test case: Forgot password calls service when user exists
    /// Verifies the service method is called with the correct email.
    /// </summary>
    [Fact]
    public async Task ForgotPassword_ValidEmail_CallsService()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequestDto>()))
            .Returns(Task.CompletedTask);

        var request = new ForgotPasswordRequestDto { Email = "existing@example.com" };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockAuthService.Verify(
            s => s.ForgotPasswordAsync(It.Is<ForgotPasswordRequestDto>(r => r.Email == "existing@example.com")),
            Times.Once);
    }

    /// <summary>
    /// Test case: Forgot password with non-existent email does not send email
    /// Expected: Service is still called (returns OK in controller to prevent enumeration)
    /// </summary>
    [Fact]
    public async Task ForgotPassword_NonExistentEmail_ServiceCalled()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequestDto>()))
            .Returns(Task.CompletedTask);

        var request = new ForgotPasswordRequestDto { Email = "notfound@example.com" };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockAuthService.Verify(
            s => s.ForgotPasswordAsync(It.Is<ForgotPasswordRequestDto>(r => r.Email == "notfound@example.com")),
            Times.Once);
    }
}
