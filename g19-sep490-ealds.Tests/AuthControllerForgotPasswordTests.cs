using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Auth;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class AuthControllerForgotPasswordTests
{
    private readonly EaldsDbContext _context;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerForgotPasswordTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _mockTokenService = new Mock<ITokenService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEmailService = new Mock<IEmailService>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        _mockConfiguration.Setup(x => x["App:OtpExpirationMinutes"]).Returns("10");
        _mockEnv.Setup(x => x.EnvironmentName).Returns("Development");

        _controller = new AuthController(
            _context,
            _mockTokenService.Object,
            _mockConfiguration.Object,
            _mockEmailService.Object,
            _mockEnv.Object,
            _mockLogger.Object
        );
    }

    /// <summary>
    /// Test case: Forgot password with valid email that exists
    /// Expected output: 200 OK
    /// Response body: { "message": "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã OTP để đặt lại mật khẩu." }
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "password123",
            Status = 1,
            RefreshToken = null,
            RefreshTokenExpiryTime = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ForgotPasswordRequestDto
        {
            Email = "test@example.com"
        };

        _mockEmailService.Setup(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Output: 200 OK
        // Response: { "message": "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã OTP để đặt lại mật khẩu." }
    }

    /// <summary>
    /// Test case: Forgot password with email that does not exist
    /// Expected output: 200 OK (to avoid leaking whether email exists)
    /// Response body: { "message": "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã OTP để đặt lại mật khẩu." }
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ReturnsOk()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Output: 200 OK
        // Response: { "message": "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã OTP để đặt lại mật khẩu." }
    }

    /// <summary>
    /// Test case: Forgot password with email of disabled user
    /// Expected output: 200 OK (user is disabled, so no OTP sent)
    /// Response body: { "message": "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã OTP để đặt lại mật khẩu." }
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithDisabledUser_ReturnsOk()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "disabled@example.com",
            Password = "password123",
            Status = 0, // Disabled
            RefreshToken = null,
            RefreshTokenExpiryTime = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ForgotPasswordRequestDto
        {
            Email = "disabled@example.com"
        };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Output: 200 OK
        // Response: { "message": "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được mã OTP để đặt lại mật khẩu." }
    }

    /// <summary>
    /// Test case: Forgot password with invalid email format
    /// Expected output: 400 Bad Request
    /// Response body: ModelState validation errors
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto
        {
            Email = "not-a-valid-email"
        };

        _controller.ModelState.AddModelError("Email", "Email không hợp lệ.");

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        
        // Output: 400 Bad Request
    }

    /// <summary>
    /// Test case: Forgot password when email service fails
    /// Expected output: 500 Internal Server Error (in development mode)
    /// Response body: { "message": "Gửi email thất bại.", "error": "...", "detail": "..." }
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WhenEmailServiceFails_ReturnsInternalServerError()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "password123",
            Status = 1,
            RefreshToken = null,
            RefreshTokenExpiryTime = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ForgotPasswordRequestDto
        {
            Email = "test@example.com"
        };

        _mockEmailService.Setup(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP error"));

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        
        // Output: 500 Internal Server Error
        // Response: { "message": "Gửi email thất bại.", "error": "...", "detail": "..." }
    }

    /// <summary>
    /// Test case: Verify system checks database for email existence
    /// This test verifies that when user requests password reset,
    /// the system queries the database to check if the email exists
    /// Expected: Database is queried and user record is found
    /// </summary>
    [Fact]
    public async Task ForgotPassword_ValidEmail_QueriesDatabaseAndFindsUser()
    {
        // Arrange - Add user to in-memory database
        var user = new User
        {
            UserId = 1,
            Email = "existing@example.com",
            Password = "password123",
            Status = 1,
            RefreshToken = null,
            RefreshTokenExpiryTime = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Verify user exists in database before test
        var userInDbBefore = await _context.Users.FirstOrDefaultAsync(u => u.Email == "existing@example.com");
        Assert.NotNull(userInDbBefore);

        var request = new ForgotPasswordRequestDto
        {
            Email = "existing@example.com"
        };

        _mockEmailService.Setup(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert - Verify user was found and OTP was generated
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Verify OTP was saved to database
        var userInDbAfter = await _context.Users.FirstOrDefaultAsync(u => u.Email == "existing@example.com");
        Assert.NotNull(userInDbAfter);
        Assert.NotNull(userInDbAfter.ResetPasswordToken);
        Assert.NotNull(userInDbAfter.ResetPasswordTokenExpiryTime);
    }

    /// <summary>
    /// Test case: Verify system returns OK even when email doesn't exist in database
    /// This simulates the scenario where user types an email that doesn't exist in the system
    /// Expected: Returns 200 OK (to prevent email enumeration attack)
    /// </summary>
    [Fact]
    public async Task ForgotPassword_NonExistentEmail_DoesNotSendEmail()
    {
        // Arrange - No user in database
        var request = new ForgotPasswordRequestDto
        {
            Email = "notfound@example.com"
        };

        // Verify no user exists
        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "notfound@example.com");
        Assert.Null(userInDb);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert - Should still return OK
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Verify email service was NOT called
        _mockEmailService.Verify(
            x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
