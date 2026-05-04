using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Maintenance;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests for MaintenanceRequestsController.CompleteMaintenance
/// (POST /api/Assets/Requests/maintenance/tasks/{taskId}/complete)
/// </summary>
public class MaintenanceRequestsControllerCompleteMaintenanceTests
{
    private readonly Mock<IMaintenanceRequestService> _mockService = null!;
    private readonly MaintenanceRequestsController _controller;

    public MaintenanceRequestsControllerCompleteMaintenanceTests()
    {
        _mockService = new Mock<IMaintenanceRequestService>();
        _controller = new MaintenanceRequestsController(_mockService.Object);
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

    // ========================================
    // Normal Cases - ExecutionDate variations
    // ========================================

    /// <summary>
    /// Test case 1 (Normal):
    /// ExecutionDate = today, ActualCost = 1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_NormalCase_ExecutionDateToday_ReturnsOk()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 1,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 1, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var ok = (OkObjectResult)result;
        var returned = (MaintenanceCompleteResultDTO)ok.Value!;
        Assert.Equal(1, returned.RecordId);
        Assert.Equal(1, returned.TaskId);
    }

    /// <summary>
    /// Test case 2 (Normal):
    /// ExecutionDate = less than today, ActualCost = 1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_NormalCase_ExecutionDateInPast_ReturnsOk()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow.AddDays(-2),
            ActualCost = 1,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 2, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Normal):
    /// ExecutionDate = greater than today, ActualCost = 1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_NormalCase_ExecutionDateInFuture_ReturnsOk()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow.AddDays(2),
            ActualCost = 1,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 3, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // ========================================
    // Abnormal / Boundary Cases - ActualCost
    // ========================================

    /// <summary>
    /// Test case 4 (Abnormal):
    /// ExecutionDate = today, ActualCost = 0, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK (no validation on ActualCost being 0)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_AbnormalCase_ActualCostZero_ReturnsOk()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 0,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 4, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Boundary):
    /// ExecutionDate = today, ActualCost = 0.1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_BoundaryCase_ActualCostMinValue_ReturnsOk()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 0.1m,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 5, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Abnormal):
    /// ExecutionDate = today, ActualCost = -1, WorkPerformed = Valid, ConditionAfter = Valid.
    /// Expected output: 200 OK (no validation on ActualCost being negative)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_AbnormalCase_ActualCostNegative_ReturnsOk()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = -1,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = "Asset in good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 6, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // ========================================
    // Abnormal Cases - WorkPerformed / ConditionAfter
    // ========================================

    /// <summary>
    /// Test case 7 (Abnormal):
    /// ExecutionDate = today, ActualCost = 1, WorkPerformed = Empty, ConditionAfter = Valid.
    /// Expected output: 200 OK (no validation on WorkPerformed being empty)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_AbnormalCase_WorkPerformedEmpty_ReturnsOk()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 1,
            WorkPerformed = "",
            ConditionAfter = "Asset in good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 7, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal):
    /// ExecutionDate = today, ActualCost = 1, WorkPerformed = Valid, ConditionAfter = Empty.
    /// Expected output: 200 OK (no validation on ConditionAfter being empty)
    /// </summary>
    [Fact]
    public async Task CompleteMaintenance_AbnormalCase_ConditionAfterEmpty_ReturnsOk()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = 1,
            WorkPerformed = "Regular maintenance completed",
            ConditionAfter = ""
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 8, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // ========================================
    // Validation Tests - BadRequest
    // ========================================

    /// <summary>
    /// Input:  dto = null
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  CompletedBy = 0
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task CompletedByZero_ReturnsBadRequest()
    {
        var dto = new MaintenanceCompleteDto { CompletedBy = 0 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto))
            .ThrowsAsync(new Exception("CompletedBy must be greater than 0"));

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  CompletedBy = -1
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task CompletedByNegative_ReturnsBadRequest()
    {
        var dto = new MaintenanceCompleteDto { CompletedBy = -1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto))
            .ThrowsAsync(new Exception("CompletedBy must be greater than 0"));

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Task with id = 999 (does not exist)
    /// Expected return: NotFoundObjectResult
    /// </summary>
    [Fact]
    public async Task TaskNotFound_ReturnsNotFound()
    {
        var dto = new MaintenanceCompleteDto { CompletedBy = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(999, dto))
            .ThrowsAsync(new KeyNotFoundException("Task not found"));

        var result = await _controller.CompleteMaintenance(taskId: 999, dto: dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Input:  Task with Status = 0 (Pending, not started)
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task TaskStatusPending_ReturnsBadRequest()
    {
        var dto = new MaintenanceCompleteDto { CompletedBy = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(2, dto))
            .ThrowsAsync(new Exception("Task is not in progress"));

        var result = await _controller.CompleteMaintenance(taskId: 2, dto: dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  Task with Status = 2 (Completed)
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task TaskStatusCompleted_ReturnsBadRequest()
    {
        var dto = new MaintenanceCompleteDto { CompletedBy = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(3, dto))
            .ThrowsAsync(new Exception("Task is not in progress"));

        var result = await _controller.CompleteMaintenance(taskId: 3, dto: dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  AssetRequest with Status != 4 (not in progress)
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task AssetRequestStatusNotInProgress_ReturnsBadRequest()
    {
        var dto = new MaintenanceCompleteDto { CompletedBy = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(4, dto))
            .ThrowsAsync(new Exception("Asset request is not in progress"));

        var result = await _controller.CompleteMaintenance(taskId: 4, dto: dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ========================================
    // Success Outcome Tests
    // ========================================

    /// <summary>
    /// Input:  Valid request, sets task status to Completed (2)
    /// Expected return: MaintenanceCompleteResultDTO with correct RecordId and TaskId
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsTaskStatusToCompleted()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 9, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request, sets AssetRequest status to Completed (5)
    /// Expected return: MaintenanceCompleteResultDTO with correct RecordId and TaskId
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsAssetRequestStatusToCompleted()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 10, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request, creates MaintenanceRecord
    /// Expected return: MaintenanceCompleteResultDTO
    /// </summary>
    [Fact]
    public async Task ValidRequest_CreatesMaintenanceRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 150000m,
            WorkPerformed = "Full inspection",
            ConditionAfter = "All systems operational"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 11, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request, updates asset status to InUse
    /// Expected return: MaintenanceCompleteResultDTO
    /// </summary>
    [Fact]
    public async Task ValidRequest_UpdatesAssetStatusToInUse()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 12, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // ========================================
    // DTO Field Priority Tests
    // ========================================

    /// <summary>
    /// Input:  MaintenanceContent provided instead of WorkPerformed
    /// Expected return: WorkPerformed uses MaintenanceContent value
    /// </summary>
    [Fact]
    public async Task MaintenanceContentProvided_UsesAsWorkPerformed()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            MaintenanceContent = "Oil change and filter replacement",
            WorkPerformed = null,
            ConditionAfter = "Engine running smooth"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 13, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  DetailedDescription provided instead of ConditionAfter
    /// Expected return: ConditionAfter uses DetailedDescription value
    /// </summary>
    [Fact]
    public async Task DetailedDescriptionProvided_UsesAsConditionAfter()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            WorkPerformed = "Inspection completed",
            DetailedDescription = "All components checked and working",
            ConditionAfter = null
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 14, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  ReturnToUseDate provided
    /// Expected return: ProposedData contains returnToUseDate
    /// </summary>
    [Fact]
    public async Task ReturnToUseDateProvided_StoredInProposedData()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition",
            ReturnToUseDate = DateTime.UtcNow.AddDays(1)
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 15, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  TotalCost used when ActualCost is null
    /// Expected return: Record TotalCost = TotalCost value
    /// </summary>
    [Fact]
    public async Task TotalCostUsed_WhenActualCostNull()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            ActualCost = null,
            TotalCost = 200000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 16, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  ExecutionDate used when CompletionDate is null
    /// Expected return: Record ExecutionDate = ExecutionDate value
    /// </summary>
    [Fact]
    public async Task ExecutionDateUsed_WhenCompletionDateNull()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = null,
            ExecutionDate = DateTime.UtcNow.AddDays(-1),
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 17, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Both CompletionDate and ExecutionDate null
    /// Expected return: ExecutionDate = DateTime.UtcNow
    /// </summary>
    [Fact]
    public async Task BothDatesNull_UsesCurrentTime()
    {
        var beforeCall = DateTime.UtcNow;
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = null,
            ExecutionDate = null,
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 18, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // ========================================
    // Record Creation Tests
    // ========================================

    /// <summary>
    /// Input:  Valid request, creates AssetRequestRecord
    /// Expected return: Record with Action = 3 (Complete)
    /// </summary>
    [Fact]
    public async Task ValidRequest_CreatesAssetRequestRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 19, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  ConditionBefore provided
    /// Expected return: Record ConditionBefore is set
    /// </summary>
    [Fact]
    public async Task ConditionBeforeProvided_StoredInRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionBefore = "Worn components",
            ConditionAfter = "Replaced and working"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 20, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  AttachmentUrls provided
    /// Expected return: ProposedData contains attachmentUrls
    /// </summary>
    [Fact]
    public async Task AttachmentUrlsProvided_StoredInProposedData()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            CompletionDate = DateTime.UtcNow,
            TotalCost = 100000m,
            WorkPerformed = "Maintenance completed",
            ConditionAfter = "Good condition",
            AttachmentUrls = new List<string> { "http://example.com/report.pdf" }
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 21, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }
}
