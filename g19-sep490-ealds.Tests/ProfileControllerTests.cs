using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Profile;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class ProfileControllerTests
{
    private readonly EaldsDbContext _context;
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new ProfileController(_context);
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

    #region GetProfile Tests

    /// <summary>
    /// Test case: Get profile with valid user ID
    /// Expected output: 200 OK with user profile data
    /// </summary>
    [Fact]
    public async Task GetProfile_WithValidUser_ReturnsOk()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "password123",
            Status = 1
        };

        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            Code = "IT",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1,
            CreatedByNavigation = user
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "John Doe",
            Code = "EMP001",
            Phone = "1234567890",
            Address = "123 Main St",
            Dob = new DateOnly(1990, 1, 1),
            Gender = 1,
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1,
            CreatedByNavigation = user,
            Department = department
        };

        _context.Users.Add(user);
        _context.Departments.Add(department);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        SetUserClaim(1);

        // Act
        var result = await _controller.GetProfile();

        // Assert
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

    /// <summary>
    /// Test case: Get profile with user that has no employee record
    /// Expected output: 200 OK with email as name fallback
    /// </summary>
    [Fact]
    public async Task GetProfile_WithNoEmployeeRecord_ReturnsOkWithEmailAsName()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "password123",
            Status = 1
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        SetUserClaim(1);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(okResult.Value);
        
        Assert.Equal("test@example.com", profile.Name); // Falls back to email
        Assert.Null(profile.EmployeeCode);
        Assert.Null(profile.Phone);
        Assert.Null(profile.Address);
    }

    /// <summary>
    /// Test case: Get profile with non-existent user
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetProfile_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange - No user in database
        SetUserClaim(999);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: Get profile without user claim (unauthorized)
    /// Expected output: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetProfile_WithoutUserClaim_ReturnsUnauthorized()
    {
        // Arrange - No claims set
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.GetProfile();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region UpdateProfile Tests

    /// <summary>
    /// Test case: Update profile with valid data
    /// Expected output: 200 OK with updated profile
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "password123",
            Status = 1
        };

        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            Code = "IT",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1,
            CreatedByNavigation = user
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Old Name",
            Code = "EMP001",
            Phone = "0000000000",
            Address = "Old Address",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1,
            CreatedByNavigation = user,
            Department = department
        };

        _context.Users.Add(user);
        _context.Departments.Add(department);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

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

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(okResult.Value);
        
        Assert.Equal("New Name", profile.Name);
        Assert.Equal("9876543210", profile.Phone);
        Assert.Equal("New Address", profile.Address);
        Assert.Equal(new DateOnly(1995, 5, 15), profile.Dob);
        Assert.Equal(1, profile.Gender);
        Assert.Equal("https://example.com/image.jpg", profile.ImageUrl);

        // Verify database was updated
        var updatedEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == 1);
        Assert.NotNull(updatedEmployee);
        Assert.Equal("New Name", updatedEmployee.Name);
    }

    /// <summary>
    /// Test case: Update profile with user that has no employee record
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithNoEmployeeRecord_ReturnsNotFound()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "password123",
            Status = 1
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        SetUserClaim(1);

        var request = new UpdateProfileRequestDto
        {
            Name = "New Name",
            Phone = "9876543210"
        };

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case: Update profile without user claim (unauthorized)
    /// Expected output: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithoutUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new UpdateProfileRequestDto
        {
            Name = "New Name"
        };

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Test case: Update profile with invalid model (empty name)
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "password123",
            Status = 1
        };

        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            Code = "IT",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1,
            CreatedByNavigation = user
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Old Name",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1,
            CreatedByNavigation = user,
            Department = department
        };

        _context.Users.Add(user);
        _context.Departments.Add(department);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        SetUserClaim(1);

        // Add model error to simulate validation failure
        _controller.ModelState.AddModelError("Name", "Name is required.");

        var request = new UpdateProfileRequestDto
        {
            Name = "" // Invalid - empty name
        };

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case: Update profile with only some fields (partial update)
    /// Expected output: 200 OK with updated profile
    /// Note: Non-provided fields will be set to null (not preserved)
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithPartialData_ReturnsOk()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "test@example.com",
            Password = "password123",
            Status = 1
        };

        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            Code = "IT",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1,
            CreatedByNavigation = user
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Old Name",
            Code = "EMP001",
            Phone = "0000000000",
            Address = "Old Address",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1,
            CreatedByNavigation = user,
            Department = department
        };

        _context.Users.Add(user);
        _context.Departments.Add(department);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        SetUserClaim(1);

        var request = new UpdateProfileRequestDto
        {
            Name = "Updated Name"
            // Other fields are null - will be set to null
        };

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfileDto>(okResult.Value);
        
        Assert.Equal("Updated Name", profile.Name);
        // Non-provided fields are set to null
        Assert.Null(profile.Phone);
        Assert.Null(profile.Address);
    }

    #endregion
}
