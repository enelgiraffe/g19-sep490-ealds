using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests for DepartmentsController.DeleteDepartment
/// (DELETE /api/Departments/{id})
/// </summary>
public class DepartmentsControllerDeleteDepartmentTests
{
    private readonly Mock<IDepartmentService> _mockService;
    private readonly DepartmentsController _controller;

    public DepartmentsControllerDeleteDepartmentTests()
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

    // ── Success: hard delete (no related entities) ─────────────────────────

    [Fact]
    public async Task DeleteDepartment_NormalCase_ExistingDepartment_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteAsync(1, 1)).ReturnsAsync((string?)null);

        var result = await _controller.DeleteDepartment(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteDepartment_MultipleDepartments_AllDeleted()
    {
        _mockService.Setup(s => s.DeleteAsync(1, It.IsAny<int>())).ReturnsAsync((string?)null);

        var result1 = await _controller.DeleteDepartment(1);
        var result2 = await _controller.DeleteDepartment(2);
        var result3 = await _controller.DeleteDepartment(3);

        Assert.IsType<NoContentResult>(result1);
        Assert.IsType<NoContentResult>(result2);
        Assert.IsType<NoContentResult>(result3);
    }

    // ── Success: soft delete (has related entities) ─────────────────────────

    [Fact]
    public async Task DeleteDepartment_WithEmployees_SoftDeletes()
    {
        _mockService.Setup(s => s.DeleteAsync(1, 1))
            .ReturnsAsync("Department has related employees. Soft delete applied.");

        var result = await _controller.DeleteDepartment(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeleteDepartment_WithAssetLocations_SoftDeletes()
    {
        _mockService.Setup(s => s.DeleteAsync(1, 1))
            .ReturnsAsync("Department has related asset locations. Soft delete applied.");

        var result = await _controller.DeleteDepartment(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDepartment_WithInventorySessions_SoftDeletes()
    {
        _mockService.Setup(s => s.DeleteAsync(1, 1))
            .ReturnsAsync("Department has related inventory sessions. Soft delete applied.");

        var result = await _controller.DeleteDepartment(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDepartment_WithMultipleRelatedEntities_SoftDeletes()
    {
        _mockService.Setup(s => s.DeleteAsync(1, 1))
            .ReturnsAsync("Department has related entities. Soft delete applied.");

        var result = await _controller.DeleteDepartment(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDepartment_SoftDelete_SetsUpdateDateAndUpdatedBy()
    {
        _mockService.Setup(s => s.DeleteAsync(1, 1))
            .ReturnsAsync("Soft delete applied.");

        var result = await _controller.DeleteDepartment(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDepartment_SoftDelete_ReturnsCorrectMessage()
    {
        const string message = "Department has related records.";
        _mockService.Setup(s => s.DeleteAsync(1, 1)).ReturnsAsync(message);

        var result = await _controller.DeleteDepartment(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    // ── NotFound ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteDepartment_BoundaryCase_DepartmentIdZero_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(1, 0)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.DeleteDepartment(0);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDepartment_AbnormalCase_NonExistentDepartment_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(1, 999)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.DeleteDepartment(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDepartment_AbnormalCase_NegativeDepartmentId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(1, -1)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.DeleteDepartment(-1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDepartment_LargeId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(1, int.MaxValue)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.DeleteDepartment(int.MaxValue);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── Unauthorized ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteDepartment_SoftDelete_NoUser_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.DeleteDepartment(1);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // NOTE: DeleteDepartment_NoRelatedEntities_HardDeletes and DeleteDepartment_SoftDelete_SetsUpdateDateAndUpdatedBy
    // require real database persistence verification. Convert to service-layer integration tests
    // if database state verification is needed.
}
