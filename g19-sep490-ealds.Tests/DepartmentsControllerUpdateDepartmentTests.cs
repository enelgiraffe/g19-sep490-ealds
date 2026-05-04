using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Departments;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests for DepartmentsController.UpdateDepartment
/// (PUT /api/Departments/{id})
/// </summary>
public class DepartmentsControllerUpdateDepartmentTests
{
    private readonly Mock<IDepartmentService> _mockService;
    private readonly DepartmentsController _controller;

    public DepartmentsControllerUpdateDepartmentTests()
    {
        _mockService = new Mock<IDepartmentService>();
        _controller = new DepartmentsController(_mockService.Object);
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

    // ── Success cases ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDepartment_NormalCase_ValidCodeNameStatusZero_ReturnsNoContent()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = "Updated IT Department", Status = 0 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_NormalCase_ValidCodeNameStatusOne_ReturnsNoContent()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "HR-UPDATED", Name = "Updated HR Department", Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_AbnormalCase_StatusTwo_ReturnsNoContent()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "FIN-UPDATED", Name = "Updated Finance Department", Status = 2 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_AbnormalCase_StatusNegative_ReturnsNoContent()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "MKT-UPDATED", Name = "Updated Marketing Department", Status = -1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_UniqueCode_ReturnsNoContent()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 2, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "IT-UNIQUE", Name = "Updated Second Department", Status = 1 };
        var result = await _controller.UpdateDepartment(2, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_CodeWithWhitespace_TrimsCode()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "  IT-TRIMMED  ", Name = "Updated IT Department", Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_NameWithWhitespace_TrimsName()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = "  Updated IT Department  ", Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_MultipleFields_AllUpdated()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "MULTI", Name = "Multi Field Update", Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_StatusZero_UpdatesToInactive()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = "Updated IT Department", Status = 0 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_LargeStatusValue_ReturnsNoContent()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = "Updated IT Department", Status = 9999 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<NoContentResult>(result);
    }

    // ── BadRequest cases ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDepartment_AbnormalCase_EmptyCode_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Code is required"));

        var dto = new UpdateDepartmentDTO { Code = "", Name = "Updated IT Department", Status = 0 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_AbnormalCase_EmptyName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Name is required"));

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = "", Status = 0 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_NullCode_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Code is required"));

        var dto = new UpdateDepartmentDTO { Code = null!, Name = "Updated IT Department", Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_NullName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Name is required"));

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = null!, Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_WhitespaceCode_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Code cannot be empty"));

        var dto = new UpdateDepartmentDTO { Code = "   ", Name = "Updated IT Department", Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_WhitespaceName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 1, It.IsAny<UpdateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Name cannot be empty"));

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = "   ", Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDepartment_DuplicateCodeCaseInsensitive_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 2, It.IsAny<UpdateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Department code already exists"));

        var dto = new UpdateDepartmentDTO { Code = "it-first", Name = "Updated IT Department", Status = 1 };
        var result = await _controller.UpdateDepartment(2, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── NotFound ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDepartment_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.UpdateAsync(1, 999, It.IsAny<UpdateDepartmentDTO>()))
            .ThrowsAsync(new KeyNotFoundException("Department not found"));

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = "Updated IT Department", Status = 1 };
        var result = await _controller.UpdateDepartment(999, dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── Unauthorized ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDepartment_NoUser_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var dto = new UpdateDepartmentDTO { Code = "IT-UPDATED", Name = "Updated IT Department", Status = 1 };
        var result = await _controller.UpdateDepartment(1, dto);

        Assert.IsType<UnauthorizedResult>(result);
    }
}
