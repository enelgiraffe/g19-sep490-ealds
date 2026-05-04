using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Auth;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AuthControllerLoginTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerLoginTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        _controller = new AuthController(_mockAuthService.Object, mockEnv.Object);
    }

    /// <summary>
    /// Test case: Login with non-existent email
    /// Expected output: 401 Unauthorized
    /// Response body: { "message": "Email hoặc mật khẩu không đúng." }
    /// </summary>
    [Fact]
    public async Task Login_WithWrongEmail_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Email hoặc mật khẩu không đúng."));

        var request = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    /// <summary>
    /// Test case: Login with wrong password
    /// Expected output: 401 Unauthorized
    /// Response body: { "message": "Email hoặc mật khẩu không đúng." }
    /// </summary>
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.LoginAsync(It.Is<LoginRequestDto>(r => r.Email == "test@example.com")))
            .ThrowsAsync(new UnauthorizedAccessException("Email hoặc mật khẩu không đúng."));

        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }
}
