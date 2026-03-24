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

public class AuthControllerLoginTests
{
    private readonly EaldsDbContext _context;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerLoginTests()
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

        _mockConfiguration.Setup(x => x["Jwt:RefreshTokenExpirationDays"]).Returns("7");

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
    /// Test case: Login with non-existent email
    /// Expected output: 401 Unauthorized
    /// Response body: { "message": "Email hoặc mật khẩu không đúng." }
    /// </summary>
    [Fact]
    public async Task Login_WithWrongEmail_ReturnsUnauthorized()
    {
        // Arrange
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
        
        // Output: 401 Unauthorized
        // Response: { "message": "Email hoặc mật khẩu không đúng." }
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
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "correctpassword",
            Status = 1,
            RefreshToken = null,
            RefreshTokenExpiryTime = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

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
        
        // Output: 401 Unauthorized
        // Response: { "message": "Email hoặc mật khẩu không đúng." }
    }

    /// <summary>
    /// Test case: Login with valid credentials
    /// Expected output: 200 OK
    /// Response body: 
    /// {
    ///   "accessToken": "fake-access-token",
    ///   "refreshToken": "fake-refresh-token",
    ///   "user": {
    ///     "userId": 1,
    ///     "email": "test@example.com",
    ///     "name": "Test User"
    ///   }
    /// }
    /// Note: This test is commented out due to EF Core InMemory 
    /// not supporting keyless entities (UserRole). 
    /// Use integration testing with real database for this case.
    /// </summary>
    // [Fact]
    // public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    // {
    //     // Arrange
    //     var user = new User
    //     {
    //         UserId = 1,
    //         Email = "test@example.com",
    //         Password = "password123",
    //         Status = 1,
    //         RefreshToken = null,
    //         RefreshTokenExpiryTime = null
    //     };
    //
    //     var employee = new Employee
    //     {
    //         EmployeeId = 1,
    //         UserId = 1,
    //         Name = "Test User"
    //     };
    //
    //     _context.Users.Add(user);
    //     _context.Employees.Add(employee);
    //     await _context.SaveChangesAsync();
    //
    //     var request = new LoginRequestDto
    //     {
    //         Email = "test@example.com",
    //         Password = "password123"
    //     };
    //
    //     _mockTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<List<string>>()))
    //         .Returns("fake-access-token");
    //     _mockTokenService.Setup(x => x.GenerateRefreshToken())
    //         .Returns("fake-refresh-token");
    //
    //     // Act
    //     var result = await _controller.Login(request);
    //
    //     // Assert
    //     var okResult = Assert.IsType<OkObjectResult>(result);
    //     var response = Assert.IsType<LoginResponseDto>(okResult.Value);
    //     
    //     Assert.NotNull(response.AccessToken);
    //     Assert.NotNull(response.RefreshToken);
    //     Assert.Equal("test@example.com", response.User.Email);
    //     
    //     // Output: 200 OK
    //     // Response: { "accessToken": "...", "refreshToken": "...", "user": {...} }
    // }
}
