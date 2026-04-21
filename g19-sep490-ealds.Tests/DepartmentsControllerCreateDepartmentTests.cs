using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests for DepartmentsController.CreateDepartment
/// (POST /api/Departments)
/// </summary>
public class DepartmentsControllerCreateDepartmentTests
{
    private readonly EaldsDbContext _context;
    private readonly DepartmentsController _controller;

    public DepartmentsControllerCreateDepartmentTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);
        _controller = new DepartmentsController(_context);
        SetUser(actorUserId: 1);
    }

    private void SetUser(int actorUserId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, actorUserId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>
    /// Test case 1 (Normal):
    /// Code = Valid, Name = Valid, Status = 0.
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public void CreateDepartment_NormalCase_ValidCodeNameStatusZero_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-001",
            Name = "Information Technology Department",
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.Equal(201, createdResult.StatusCode);

        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal("IT-001", response.Code);
        Assert.Equal("Information Technology Department", response.Name);
        Assert.Equal(0, response.Status);
        Assert.True(response.DepartmentId > 0);
    }

    /// <summary>
    /// Test case 2 (Abnormal):
    /// Code = Empty, Name = Valid, Status = 0.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateDepartment_AbnormalCase_EmptyCode_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "",
            Name = "Information Technology Department",
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case 3 (Abnormal):
    /// Code = Valid, Name = Empty, Status = 0.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateDepartment_AbnormalCase_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-001",
            Name = "",
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test case 4 (Normal):
    /// Code = Valid, Name = Valid, Status = 1.
    /// Expected output: 201 Created
    /// </summary>
    [Fact]
    public void CreateDepartment_NormalCase_ValidCodeNameStatusOne_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "HR-001",
            Name = "Human Resources Department",
            Status = 1
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.Equal(201, createdResult.StatusCode);

        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal("HR-001", response.Code);
        Assert.Equal("Human Resources Department", response.Name);
        Assert.Equal(1, response.Status);
    }

    /// <summary>
    /// Test case 5 (Abnormal):
    /// Code = Valid, Name = Valid, Status = 2.
    /// Expected output: 201 Created (no validation on Status)
    /// </summary>
    [Fact]
    public void CreateDepartment_AbnormalCase_StatusTwo_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "FIN-001",
            Name = "Finance Department",
            Status = 2
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.Equal(201, createdResult.StatusCode);

        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal(2, response.Status);
    }

    /// <summary>
    /// Test case 6 (Abnormal):
    /// Code = Valid, Name = Valid, Status = -1.
    /// Expected output: 201 Created (no validation on Status)
    /// </summary>
    [Fact]
    public void CreateDepartment_AbnormalCase_StatusNegative_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "MKT-001",
            Name = "Marketing Department",
            Status = -1
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.Equal(201, createdResult.StatusCode);

        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal(-1, response.Status);
    }

    // ─── Additional validation tests ─────────────────────────────────────────

    /// <summary>
    /// Input:  Code = null
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateDepartment_NullCode_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = null!,
            Name = "Information Technology Department",
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Input:  Name = null
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateDepartment_NullName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-001",
            Name = null!,
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Input:  Code = whitespace only
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateDepartment_WhitespaceCode_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "   ",
            Name = "Information Technology Department",
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Input:  Name = whitespace only
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateDepartment_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-001",
            Name = "   ",
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Input:  Code = "IT001" (duplicate, case-insensitive)
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public void CreateDepartment_DuplicateCodeCaseInsensitive_ReturnsBadRequest()
    {
        // Arrange - Create first department
        _context.Departments.Add(new Department
        {
            Code = "IT001",
            Name = "Information Technology",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });
        _context.SaveChanges();

        var dto = new CreateDepartmentDTO
        {
            Code = "it001", // Same code, different case
            Name = "IT Department",
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Input:  Valid department with Status = 1 (default)
    /// Expected return: 201 Created with Status = 1
    /// </summary>
    [Fact]
    public void CreateDepartment_DefaultStatus_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-002",
            Name = "IT Department"
            // Status not set - should default to 1
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        var response = Assert.IsType<DepartmentDTO>(((CreatedAtActionResult)result.Result!).Value);
        Assert.Equal(1, response.Status); // Default value from DTO
    }

    /// <summary>
    /// Input:  No user authenticated
    /// Expected return: 401 Unauthorized
    /// </summary>
    [Fact]
    public void CreateDepartment_NoUser_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var dto = new CreateDepartmentDTO
        {
            Code = "IT-001",
            Name = "Information Technology Department",
            Status = 0
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    /// <summary>
    /// Input:  Valid data, department is persisted to database
    /// Expected return: Department found in database
    /// </summary>
    [Fact]
    public void CreateDepartment_ValidData_PersistsToDatabase()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-003",
            Name = "Information Technology Department",
            Status = 1
        };

        // Act
        _controller.CreateDepartment(dto);

        // Assert
        var department = _context.Departments.FirstOrDefault(d => d.Code == "IT-003");
        Assert.NotNull(department);
        Assert.Equal("Information Technology Department", department.Name);
        Assert.Equal(1, department.Status);
        Assert.Equal(1, department.CreatedBy);
        Assert.True(department.CreateDate <= DateTime.UtcNow);
    }

    /// <summary>
    /// Input:  Code with leading/trailing whitespace
    /// Expected return: 201 Created with trimmed code
    /// </summary>
    [Fact]
    public void CreateDepartment_CodeWithWhitespace_TrimsCode()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "  IT-004  ",
            Name = "IT Department",
            Status = 1
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        var response = Assert.IsType<DepartmentDTO>(((CreatedAtActionResult)result.Result!).Value);
        Assert.Equal("IT-004", response.Code);
    }

    /// <summary>
    /// Input:  Name with leading/trailing whitespace
    /// Expected return: 201 Created with trimmed name
    /// </summary>
    [Fact]
    public void CreateDepartment_NameWithWhitespace_TrimsName()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-005",
            Name = "  IT Department  ",
            Status = 1
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);

        var response = Assert.IsType<DepartmentDTO>(((CreatedAtActionResult)result.Result!).Value);
        Assert.Equal("IT Department", response.Name);
    }

    /// <summary>
    /// Input:  Unique code with special characters
    /// Expected return: 201 Created
    /// </summary>
    [Fact]
    public void CreateDepartment_CodeWithSpecialChars_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-DEPT_001",
            Name = "IT Department",
            Status = 1
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>
    /// Input:  Very long Name (within MaxLength)
    /// Expected return: 201 Created
    /// </summary>
    [Fact]
    public void CreateDepartment_LongName_ReturnsCreated()
    {
        // Arrange
        var longName = new string('A', 255); // MaxLength is 255
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-006",
            Name = longName,
            Status = 1
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        Assert.IsType<CreatedAtResponse>(result.Result);
    }

    /// <summary>
    /// Input:  Response contains correct CreatedAtAction route values
    /// Expected return: RouteValues contain id and action name
    /// </summary>
    [Fact]
    public void CreateDepartment_ResponseHasCorrectRouteValues()
    {
        // Arrange
        var dto = new CreateDepartmentDTO
        {
            Code = "IT-007",
            Name = "IT Department",
            Status = 1
        };

        // Act
        var result = _controller.CreateDepartment(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(DepartmentsController.GetDepartment), createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.True(createdResult.RouteValues.ContainsKey("id"));
    }

    /// <summary>
    /// Input:  Create multiple departments with unique codes
    /// Expected return: All departments created successfully
    /// </summary>
    [Fact]
    public void CreateDepartment_MultipleUniqueCodes_AllCreated()
    {
        // Arrange & Act
        var dto1 = new CreateDepartmentDTO { Code = "IT-008", Name = "IT 1", Status = 1 };
        var dto2 = new CreateDepartmentDTO { Code = "IT-009", Name = "IT 2", Status = 1 };
        var dto3 = new CreateDepartmentDTO { Code = "IT-010", Name = "IT 3", Status = 1 };

        var result1 = _controller.CreateDepartment(dto1);
        var result2 = _controller.CreateDepartment(dto2);
        var result3 = _controller.CreateDepartment(dto3);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result1.Result);
        Assert.IsType<CreatedAtActionResult>(result2.Result);
        Assert.IsType<CreatedAtActionResult>(result3.Result);

        Assert.Equal(3, _context.Departments.Count());
    }
}
