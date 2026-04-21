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
/// Unit tests for DepartmentsController.UpdateDepartment
/// (PUT /api/Departments/{id})
/// </summary>
public class DepartmentsControllerUpdateDepartmentTests
{
    private readonly EaldsDbContext _context;
    private readonly DepartmentsController _controller;

    public DepartmentsControllerUpdateDepartmentTests()
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

    private async Task<Department> SeedDepartmentAsync(string code = "IT-001", string name = "Information Technology", int status = 1)
    {
        var department = new Department
        {
            Code = code,
            Name = name,
            Status = status,
            CreateDate = DateTime.UtcNow.AddDays(-1),
            CreatedBy = 1
        };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        return department;
    }

    /// <summary>
    /// Test case 1 (Normal):
    /// Code = Valid, Name = Valid, Status = 0.
    /// Expected output: 204 No Content
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_NormalCase_ValidCodeNameStatusZero_ReturnsNoContent()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "Updated IT Department",
            Status = 0
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal("IT-UPDATED", updated.Code);
        Assert.Equal("Updated IT Department", updated.Name);
        Assert.Equal(0, updated.Status);
        Assert.True(updated.UpdateDate.HasValue);
        Assert.Equal(1, updated.UpdatedBy);
    }

    /// <summary>
    /// Test case 2 (Abnormal):
    /// Code = Empty, Name = Valid, Status = 0.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_AbnormalCase_EmptyCode_ReturnsBadRequest()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "",
            Name = "Updated IT Department",
            Status = 0
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal):
    /// Code = Valid, Name = Empty, Status = 0.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_AbnormalCase_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "",
            Status = 0
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Normal):
    /// Code = Valid, Name = Valid, Status = 1.
    /// Expected output: 204 No Content
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_NormalCase_ValidCodeNameStatusOne_ReturnsNoContent()
    {
        // Arrange
        var department = await SeedDepartmentAsync(status: 0);
        var dto = new UpdateDepartmentDTO
        {
            Code = "HR-UPDATED",
            Name = "Updated HR Department",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal(1, updated.Status);
    }

    /// <summary>
    /// Test case 5 (Abnormal):
    /// Code = Valid, Name = Valid, Status = 2.
    /// Expected output: 204 No Content (no validation on Status)
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_AbnormalCase_StatusTwo_ReturnsNoContent()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "FIN-UPDATED",
            Name = "Updated Finance Department",
            Status = 2
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Status);
    }

    /// <summary>
    /// Test case 6 (Abnormal):
    /// Code = Valid, Name = Valid, Status = -1.
    /// Expected output: 204 No Content (no validation on Status)
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_AbnormalCase_StatusNegative_ReturnsNoContent()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "MKT-UPDATED",
            Name = "Updated Marketing Department",
            Status = -1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal(-1, updated.Status);
    }

    // ─── Additional validation tests ─────────────────────────────────────────

    /// <summary>
    /// Input:  Department id = 999 (does not exist)
    /// Expected return: 404 Not Found
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_NotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "Updated IT Department",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Input:  Code = null
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_NullCode_ReturnsBadRequest()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = null!,
            Name = "Updated IT Department",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Name = null
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_NullName_ReturnsBadRequest()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = null!,
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Code = whitespace only
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_WhitespaceCode_ReturnsBadRequest()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "   ",
            Name = "Updated IT Department",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Name = whitespace only
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "   ",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Code = same as another department (case-insensitive)
    /// Expected return: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_DuplicateCodeCaseInsensitive_ReturnsBadRequest()
    {
        // Arrange
        var department1 = await SeedDepartmentAsync("IT-FIRST", "IT First Department");
        var department2 = await SeedDepartmentAsync("IT-SECOND", "IT Second Department");

        var dto = new UpdateDepartmentDTO
        {
            Code = "it-first", // Same code, different case (but this is the same department)
            Name = "Updated IT Department",
            Status = 1
        };

        // Act - This should succeed since we're updating the same department
        var result = await _controller.UpdateDepartment(department2.DepartmentId, dto);

        // Assert - Should fail because "it-first" conflicts with department1's code
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Update to unique code that doesn't conflict
    /// Expected return: 204 No Content
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_UniqueCode_ReturnsNoContent()
    {
        // Arrange
        var department1 = await SeedDepartmentAsync("IT-FIRST", "IT First Department");
        var department2 = await SeedDepartmentAsync("IT-SECOND", "IT Second Department");

        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UNIQUE", // New unique code
            Name = "Updated Second Department",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department2.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department2.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal("IT-UNIQUE", updated.Code);
    }

    /// <summary>
    /// Input:  No user authenticated
    /// Expected return: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_NoUser_ReturnsUnauthorized()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "Updated IT Department",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Input:  Code with leading/trailing whitespace
    /// Expected return: 204 No Content with trimmed code
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_CodeWithWhitespace_TrimsCode()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "  IT-TRIMMED  ",
            Name = "Updated IT Department",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal("IT-TRIMMED", updated.Code);
    }

    /// <summary>
    /// Input:  Name with leading/trailing whitespace
    /// Expected return: 204 No Content with trimmed name
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_NameWithWhitespace_TrimsName()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "  Updated IT Department  ",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal("Updated IT Department", updated.Name);
    }

    /// <summary>
    /// Input:  Keep the same code (no change)
    /// Expected return: 204 No Content
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_SameCode_ReturnsNoContent()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = department.Code, // Same code
            Name = "Updated Department Name",
            Status = 1
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Input:  Update department with UpdateDate and UpdatedBy
    /// Expected return: Department has UpdateDate and UpdatedBy set
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_SetsUpdateDateAndUpdatedBy()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var beforeUpdate = DateTime.UtcNow;

        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "Updated IT Department",
            Status = 1
        };

        // Act
        await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.True(updated.UpdateDate.HasValue);
        Assert.True(updated.UpdateDate >= beforeUpdate);
        Assert.Equal(1, updated.UpdatedBy);
    }

    /// <summary>
    /// Input:  Valid update with Status = 0 (inactive)
    /// Expected return: Department status = 0
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_StatusZero_UpdatesToInactive()
    {
        // Arrange
        var department = await SeedDepartmentAsync(status: 1);
        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "Updated IT Department",
            Status = 0 // Inactive
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal(0, updated.Status);
    }

    /// <summary>
    /// Input:  Large status value
    /// Expected return: 204 No Content
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_LargeStatusValue_ReturnsNoContent()
    {
        // Arrange
        var department = await SeedDepartmentAsync();
        var dto = new UpdateDepartmentDTO
        {
            Code = "IT-UPDATED",
            Name = "Updated IT Department",
            Status = 99999
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal(99999, updated.Status);
    }

    /// <summary>
    /// Input:  Update multiple fields at once
    /// Expected return: All fields updated correctly
    /// </summary>
    [Fact]
    public async Task UpdateDepartment_MultipleFields_AllUpdated()
    {
        // Arrange
        var department = await SeedDepartmentAsync("OLD-CODE", "Old Name", status: 1);
        var dto = new UpdateDepartmentDTO
        {
            Code = "NEW-CODE",
            Name = "New Name",
            Status = 0
        };

        // Act
        var result = await _controller.UpdateDepartment(department.DepartmentId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updated = await _context.Departments.FindAsync(department.DepartmentId);
        Assert.NotNull(updated);
        Assert.Equal("NEW-CODE", updated.Code);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(0, updated.Status);
        // CreateDate should remain unchanged
        Assert.True(updated.CreateDate <= DateTime.UtcNow);
    }
}
