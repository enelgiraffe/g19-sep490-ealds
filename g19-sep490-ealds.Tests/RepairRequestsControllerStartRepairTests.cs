using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Repair;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests for RepairRequestsController.StartRepair
/// (POST /api/Assets/Requests/repair/{id}/start)
/// Uses mock-based testing — IRepairRequestService is mocked per test.
/// </summary>
public class RepairRequestsControllerStartRepairTests
{
    private Mock<IRepairRequestService> _mockService = null!;
    private RepairRequestsController _controller = null!;

    // ─── Setup helpers ───────────────────────────────────────────────────────

    private void SetUser(int userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ─── StartRepair: null / validation input ────────────────────────────────

    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.StartRepair(id: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StartedByZero_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ThrowsAsync(new Exception("StartedBy must be greater than 0."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = 0 };
        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StartedByNegative_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ThrowsAsync(new Exception("StartedBy must be greater than 0."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = -1 };
        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── StartRepair: not-found scenarios ────────────────────────────────────

    [Fact]
    public async Task RequestNotFound_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ThrowsAsync(new KeyNotFoundException("Request not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 999, dto: dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RequestNotApproved_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ThrowsAsync(new Exception("Yêu cầu chưa được phê duyệt."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── StartRepair: authorization ─────────────────────────────────────────

    [Fact]
    public async Task UserWithInvalidRole_ReturnsForbid()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized to start repair."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 3, dto: dto);
        Assert.IsType<ForbidResult>(result);
    }

    // ─── StartRepair: happy path ─────────────────────────────────────────────

    [Fact]
    public async Task ValidRequest_WithDirectorRole_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidRequest_WithDepartmentManagerRole_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 4, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidRequest_WithAccountantRole_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 5, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidRequest_UpdatesAssetStatusToInRepair()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidRequest_SetsTaskStatusToInProgress()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidRequest_SetsAssetRequestStatusToInRepair()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidRequest_WithSupplierId_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress",
            SupplierId = 1
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AlreadyInRepair_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 6, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m,
            RepairProgressStatus = "InProgress"
        };

        var result = await _controller.StartRepair(id: 6, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RequestWithoutAssetId_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ThrowsAsync(new Exception("AssetId is required."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 7, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── StartRepair: date-edge cases ─────────────────────────────────────────

    [Fact]
    public async Task StartRepair_NormalCase_RepairDateEqualsDamageDate_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var damageDate = DateTime.UtcNow;
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = damageDate,
            ExpectedCompletionDate = damageDate,
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StartRepair_RepairDateBeforeDamageDate_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var damageDate = DateTime.UtcNow;
        var repairDate = damageDate.AddDays(-1);
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate,
            ExpectedCompletionDate = repairDate,
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StartRepair_RepairDateAfterDamageDate_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var damageDate = DateTime.UtcNow.AddDays(-5);
        var repairDate = DateTime.UtcNow;
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate,
            ExpectedCompletionDate = repairDate,
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StartRepair_ExpectedCompletionBeforeRepairDate_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var damageDate = DateTime.UtcNow.AddDays(-10);
        var repairDate = DateTime.UtcNow;
        var expectedCompletion = repairDate.AddDays(-1);
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate,
            ExpectedCompletionDate = expectedCompletion,
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StartRepair_ExpectedCompletionAfterRepairDate_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var damageDate = DateTime.UtcNow.AddDays(-5);
        var repairDate = DateTime.UtcNow;
        var expectedCompletion = repairDate.AddDays(7);
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate,
            ExpectedCompletionDate = expectedCompletion,
            RepairProgressStatus = "InProgress",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StartRepair_EmptyRepairProgressStatus_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var damageDate = DateTime.UtcNow.AddDays(-5);
        var repairDate = DateTime.UtcNow;
        var dto = new RepairStartDto
        {
            StartedBy = 1,
            DamageDate = damageDate,
            RepairDate = repairDate,
            ExpectedCompletionDate = repairDate,
            RepairProgressStatus = "",
            DamageCondition = "Screen broken",
            EstimatedCost = 500000m
        };

        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }
}
