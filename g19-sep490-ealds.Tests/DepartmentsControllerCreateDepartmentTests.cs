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
/// Unit tests for DepartmentsController.CreateDepartment
/// (POST /api/Departments)
/// </summary>
public class DepartmentsControllerCreateDepartmentTests
{
    private readonly Mock<IDepartmentService> _mockService;
    private readonly DepartmentsController _controller;

    public DepartmentsControllerCreateDepartmentTests()
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

    private static DepartmentDTO MakeDto(string code, string name, int status, int id = 1)
        => new DepartmentDTO { DepartmentId = id, Code = code, Name = name, Status = status };

    // ── Success cases ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDepartment_NormalCase_ValidCodeNameStatusZero_ReturnsCreated()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("IT-001", "Information Technology Department", 0));

        var dto = new CreateDepartmentDTO { Code = "IT-001", Name = "Information Technology Department", Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal("IT-001", response.Code);
        Assert.Equal("Information Technology Department", response.Name);
        Assert.Equal(0, response.Status);
    }

    [Fact]
    public async Task CreateDepartment_NormalCase_ValidCodeNameStatusOne_ReturnsCreated()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("HR-001", "Human Resources Department", 1));

        var dto = new CreateDepartmentDTO { Code = "HR-001", Name = "Human Resources Department", Status = 1 };
        var result = await _controller.CreateDepartment(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal("HR-001", response.Code);
        Assert.Equal("Human Resources Department", response.Name);
        Assert.Equal(1, response.Status);
    }

    [Fact]
    public async Task CreateDepartment_AbnormalCase_StatusTwo_ReturnsCreated()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("FIN-001", "Finance Department", 2));

        var dto = new CreateDepartmentDTO { Code = "FIN-001", Name = "Finance Department", Status = 2 };
        var result = await _controller.CreateDepartment(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal(2, response.Status);
    }

    [Fact]
    public async Task CreateDepartment_AbnormalCase_StatusNegative_ReturnsCreated()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("MKT-001", "Marketing Department", -1));

        var dto = new CreateDepartmentDTO { Code = "MKT-001", Name = "Marketing Department", Status = -1 };
        var result = await _controller.CreateDepartment(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal(-1, response.Status);
    }

    [Fact]
    public async Task CreateDepartment_DefaultStatus_ReturnsCreated()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("IT-002", "IT Department", 1));

        var dto = new CreateDepartmentDTO { Code = "IT-002", Name = "IT Department" };
        var result = await _controller.CreateDepartment(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal(1, response.Status);
    }

    [Fact]
    public async Task CreateDepartment_CodeWithWhitespace_TrimsCode()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("IT-004", "IT Department", 1));

        var dto = new CreateDepartmentDTO { Code = "  IT-004  ", Name = "IT Department", Status = 1 };
        var result = await _controller.CreateDepartment(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal("IT-004", response.Code);
    }

    [Fact]
    public async Task CreateDepartment_NameWithWhitespace_TrimsName()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("IT-005", "IT Department", 1));

        var dto = new CreateDepartmentDTO { Code = "IT-005", Name = "  IT Department  ", Status = 1 };
        var result = await _controller.CreateDepartment(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<DepartmentDTO>(createdResult.Value);
        Assert.Equal("IT Department", response.Name);
    }

    [Fact]
    public async Task CreateDepartment_CodeWithSpecialChars_ReturnsCreated()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("IT-DEPT_001", "IT Department", 1));

        var dto = new CreateDepartmentDTO { Code = "IT-DEPT_001", Name = "IT Department", Status = 1 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task CreateDepartment_LongName_ReturnsCreated()
    {
        var longName = new string('A', 255);
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("IT-006", longName, 1));

        var dto = new CreateDepartmentDTO { Code = "IT-006", Name = longName, Status = 1 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task CreateDepartment_ResponseHasCorrectRouteValues()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync(MakeDto("IT-007", "IT Department", 1, id: 42));

        var dto = new CreateDepartmentDTO { Code = "IT-007", Name = "IT Department", Status = 1 };
        var result = await _controller.CreateDepartment(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(DepartmentsController.GetDepartment), createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.True(createdResult.RouteValues.ContainsKey("id"));
    }

    [Fact]
    public async Task CreateDepartment_MultipleUniqueCodes_AllCreated()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ReturnsAsync((int _, CreateDepartmentDTO d) => MakeDto(d.Code, d.Name, d.Status));

        var dto1 = new CreateDepartmentDTO { Code = "IT-008", Name = "IT 1", Status = 1 };
        var dto2 = new CreateDepartmentDTO { Code = "IT-009", Name = "IT 2", Status = 1 };
        var dto3 = new CreateDepartmentDTO { Code = "IT-010", Name = "IT 3", Status = 1 };

        var result1 = await _controller.CreateDepartment(dto1);
        var result2 = await _controller.CreateDepartment(dto2);
        var result3 = await _controller.CreateDepartment(dto3);

        Assert.IsType<CreatedAtActionResult>(result1);
        Assert.IsType<CreatedAtActionResult>(result2);
        Assert.IsType<CreatedAtActionResult>(result3);
    }

    // ── BadRequest / error cases ────────────────────────────────────────────

    [Fact]
    public async Task CreateDepartment_AbnormalCase_EmptyCode_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Code is required"));

        var dto = new CreateDepartmentDTO { Code = "", Name = "IT Department", Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDepartment_AbnormalCase_EmptyName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Name is required"));

        var dto = new CreateDepartmentDTO { Code = "IT-001", Name = "", Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDepartment_NullCode_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Code is required"));

        var dto = new CreateDepartmentDTO { Code = null!, Name = "IT Department", Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDepartment_NullName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Name is required"));

        var dto = new CreateDepartmentDTO { Code = "IT-001", Name = null!, Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDepartment_WhitespaceCode_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Code cannot be empty"));

        var dto = new CreateDepartmentDTO { Code = "   ", Name = "IT Department", Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDepartment_WhitespaceName_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Name cannot be empty"));

        var dto = new CreateDepartmentDTO { Code = "IT-001", Name = "   ", Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDepartment_DuplicateCode_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateDepartmentDTO>()))
            .ThrowsAsync(new ArgumentException("Department code already exists"));

        var dto = new CreateDepartmentDTO { Code = "it001", Name = "IT Department", Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── Unauthorized ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDepartment_NoUser_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var dto = new CreateDepartmentDTO { Code = "IT-001", Name = "IT Department", Status = 0 };
        var result = await _controller.CreateDepartment(dto);

        Assert.IsType<UnauthorizedResult>(result);
    }
}
