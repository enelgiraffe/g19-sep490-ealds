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

public class RepairRequestsControllerCompleteRepairTests
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

    // ─── Null / Invalid Input ──────────────────────────────────────────────────

    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CompletedByZero_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new Exception("CompletedBy must be greater than 0."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 0 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CompletedByNegative_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new Exception("CompletedBy must be greater than 0."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = -1 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Task not found / wrong status ────────────────────────────────────────

    [Fact]
    public async Task TaskNotFound_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new KeyNotFoundException("Repair task not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 999, dto: new RepairCompleteDto { CompletedBy = 1 });
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task TaskStatusZero_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new Exception("Task status is not valid for completion."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1 });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task TaskStatusTwo_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new Exception("Task status is not valid for completion."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1 });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── AssetRequest wrong state ─────────────────────────────────────────────

    [Fact]
    public async Task RequestStatusNot4_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new Exception("Asset request status must be 'In Progress' (4) to complete repair."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1 });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RequestTypeNotRepair_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new Exception("Asset request type must be 'Repair' (4) to complete repair."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1 });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidData_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1, Result = "Fixed" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidData_ReturnsRecordIdAndTaskId()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 42, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var okResult = (OkObjectResult)await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1, Result = "Fixed" });
        var response = okResult.Value!;
        var recordId = (int)response.GetType().GetProperty("RecordId")!.GetValue(response)!;
        var taskId = (int)response.GetType().GetProperty("TaskId")!.GetValue(response)!;

        Assert.True(recordId > 0);
        Assert.Equal(1, taskId);
    }

    [Fact]
    public async Task ValidData_CreatesRepairRecord()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1, Result = "Fixed" });
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task ValidData_SetsTaskStatusToCompleted()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1, Result = "Fixed" });
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task ValidData_SetsAssetRequestStatusToCompleted()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1, Result = "Fixed" });
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    // ─── ReturnToUseDate ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReturnToUseDateToday_RestoresAssetToInUse()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", ReturnToUseDate = DateTime.UtcNow };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task ReturnToUseDateFuture_DoesNotRestoreAsset()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", ReturnToUseDate = DateTime.UtcNow.AddDays(10) };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task NoReturnToUseDate_DoesNotRestoreAsset()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1, Result = "Fixed" });
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    // ─── AssetLifeCycle ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReturnToUseDateToday_CreatesLifeCycleRecord()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", ReturnToUseDate = DateTime.UtcNow };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    // ─── ProposedData ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidData_AppendsRepairCompletionToProposedData()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", ReportNumber = "RPR-001", ActualCost = 3000000m };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task ProposedDataNull_InitializesSuccessfully()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: new RepairCompleteDto { CompletedBy = 1, Result = "Fixed" });
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    // ─── Specific Test Cases ─────────────────────────────────────────────────

    [Fact]
    public async Task CompleteRepair_NormalCase_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow, ReturnToUseDate = DateTime.UtcNow, ActualCost = 1 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task CompleteRepair_RepairDateInPast_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow.AddDays(-1), ReturnToUseDate = DateTime.UtcNow.AddDays(-1), ActualCost = 1 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteRepair_RepairDateInFuture_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow.AddDays(1), ReturnToUseDate = DateTime.UtcNow.AddDays(1), ActualCost = 1 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteRepair_TaskIdZero_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new KeyNotFoundException("Repair task not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow, ReturnToUseDate = DateTime.UtcNow, ActualCost = 1 };
        var result = await _controller.CompleteRepair(taskId: 0, dto: dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CompleteRepair_TaskIdNonExistent_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new KeyNotFoundException("Repair task 999 not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow, ReturnToUseDate = DateTime.UtcNow, ActualCost = 1 };
        var result = await _controller.CompleteRepair(taskId: 999, dto: dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CompleteRepair_ReturnToUseDateInPast_DoesNotRestoreAssetToInUse()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow, ReturnToUseDate = DateTime.UtcNow.AddDays(-1), ActualCost = 1 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task CompleteRepair_ReturnToUseDateTodayOrFuture_RestoresAssetToInUse()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow, ReturnToUseDate = DateTime.UtcNow, ActualCost = 1 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task CompleteRepair_ActualCostZero_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow, ReturnToUseDate = DateTime.UtcNow, ActualCost = 0 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }

    [Fact]
    public async Task CompleteRepair_ActualCostNegative_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { CompletedBy = 1, Result = "Fixed", RepairDate = DateTime.UtcNow, ReturnToUseDate = DateTime.UtcNow, ActualCost = -1, RepairWarrantyPeriodValue = -1 };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CompleteRepairAsync(1, It.IsAny<RepairCompleteDto>()), Times.Once);
    }
}
