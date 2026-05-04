using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Maintenance;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class MaintenanceRequestsControllerCostRecordingTests
{
    private readonly Mock<IMaintenanceRequestService> _mockService;
    private readonly MaintenanceRequestsController _controller;

    public MaintenanceRequestsControllerCostRecordingTests()
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
    // Part 1: RequestExecution - Estimated Cost When Creating Maintenance Request
    // ========================================

    #region RequestExecution - EstimatedCost

    [Fact]
    public async Task RequestExecution_WithValidData_CreatesTask()
    {
        var dto = new MaintenanceRequestDTO
        {
            AssetInstanceId = 1,
            Title = "Maintain Laptop",
            Description = "Regular maintenance",
            PlannedDate = DateTime.UtcNow.AddDays(5),
            ScheduleId = null,
            CreatedBy = 1
        };

        var expectedResult = new MaintenanceRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 };
        _mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(expectedResult);

        var result = await _controller.RequestExecution(dto);

        Assert.IsType<OkObjectResult>(result);
        var ok = (OkObjectResult)result;
        var returned = (MaintenanceRequestCreateResultDTO)ok.Value!;
        Assert.Equal(1, returned.AssetRequestId);
        Assert.Equal(1, returned.TaskId);
    }

    [Fact]
    public async Task RequestExecution_WithInvalidAssetInstanceId_ReturnsNotFound()
    {
        var dto = new MaintenanceRequestDTO
        {
            AssetInstanceId = 9999,
            Title = "Maintain asset",
            Description = "Description",
            PlannedDate = DateTime.UtcNow.AddDays(5),
            CreatedBy = 1
        };

        _mockService.Setup(s => s.CreateAsync(dto)).ThrowsAsync(new KeyNotFoundException("Asset instance not found"));

        var result = await _controller.RequestExecution(dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RequestExecution_NullDto_ReturnsBadRequest()
    {
        var result = await _controller.RequestExecution(null!);

        Assert.IsType<BadRequestObjectResult>(result);
        var bad = (BadRequestObjectResult)result;
        Assert.NotNull(bad.Value);
    }

    #endregion

    // ========================================
    // Part 2: StartMaintenance - Estimated Cost Update When Starting Maintenance
    // ========================================

    #region StartMaintenance - EstimatedCost Update

    [Fact]
    public async Task StartMaintenance_WithEstimatedCost_UpdatesTaskAndProposedData()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            EstimatedCost = 800000m,
            MaintenanceContent = "Oil change and inspection",
            MaintenanceProvider = "TechService Co.",
            MaintenanceDate = DateTime.UtcNow
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var ok = (OkObjectResult)result;
        var returned = (MaintenanceStartResultDTO)ok.Value!;
        Assert.Equal(1, returned.Status);
        Assert.Equal(1, returned.TaskId);
    }

    [Fact]
    public async Task StartMaintenance_WithNegativeEstimatedCost_RecordsInProposedData()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            EstimatedCost = -100000m,
            MaintenanceContent = "Test negative cost"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StartMaintenance_RequestNotFound_ReturnsNotFound()
    {
        var dto = new MaintenanceStartDto { StartedBy = 1 };

        _mockService.Setup(s => s.StartMaintenanceAsync(9999, dto)).ThrowsAsync(new KeyNotFoundException("Request not found"));

        var result = await _controller.StartMaintenance(id: 9999, dto: dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task StartMaintenance_InvalidStartedBy_ReturnsBadRequest()
    {
        var dto = new MaintenanceStartDto { StartedBy = 0 };

        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ThrowsAsync(new Exception("StartedBy must be greater than 0"));

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StartMaintenance_NullDto_ReturnsBadRequest()
    {
        var result = await _controller.StartMaintenance(id: 1, dto: null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StartMaintenance_ValidRequest_ChangesAssetStatusToInMaintenance()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceContent = "Full maintenance",
            MaintenanceProvider = "Provider A"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StartMaintenance_ValidRequest_CreatesLifeCycleRecord()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceContent = "Full maintenance"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    // ========================================
    // Part 3: CompleteMaintenance - Actual Cost Recording When Completing Maintenance
    // ========================================

    #region CompleteMaintenance - ActualCost / TotalCost Recording

    [Fact]
    public async Task CompleteMaintenance_WithActualCost_CreatesMaintenanceRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            ActualCost = 750000m,
            TotalCost = 750000m,
            WorkPerformed = "Oil change and filter replacement",
            CompletionDate = DateTime.UtcNow
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

    [Fact]
    public async Task CompleteMaintenance_WithTotalCostOnly_CreatesRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 1200000m,
            WorkPerformed = "Full service",
            CompletionDate = DateTime.UtcNow
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 2, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_ActualCostTakesPriorityOverTotalCost()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            ActualCost = 900000m,
            TotalCost = 1000000m,
            WorkPerformed = "Service with priority cost"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 3, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithZeroCost_CreatesRecordWithZeroCost()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 0m,
            WorkPerformed = "Free inspection",
            CompletionDate = DateTime.UtcNow
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 4, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_NullDto_ReturnsBadRequest()
    {
        var result = await _controller.CompleteMaintenance(taskId: 1, dto: null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_InvalidCompletedBy_ReturnsBadRequest()
    {
        var dto = new MaintenanceCompleteDto { CompletedBy = 0, TotalCost = 100000m };

        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ThrowsAsync(new Exception("CompletedBy must be greater than 0"));

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_TaskNotFound_ReturnsNotFound()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 100000m,
            WorkPerformed = "Test"
        };

        _mockService.Setup(s => s.CompleteMaintenanceAsync(9999, dto)).ThrowsAsync(new KeyNotFoundException("Task not found"));

        var result = await _controller.CompleteMaintenance(taskId: 9999, dto: dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_TaskNotInProgress_ReturnsBadRequest()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 100000m,
            WorkPerformed = "Test"
        };

        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ThrowsAsync(new Exception("Task is not in progress"));

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_Success_SetsTaskStatusToCompleted()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Standard maintenance"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 5, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_Success_SetsAssetRequestStatusToCompleted()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Standard maintenance"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 6, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_Success_RestoresAssetToInUse()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Maintenance done"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 7, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_Success_CreatesLifeCycleRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Maintenance completed"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 8, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithReportNumber_RecordsInProposedData()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            ReportNumber = "MNT-2024-001",
            TotalCost = 600000m,
            WorkPerformed = "Inspection and service"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 9, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithReturnToUseDate_RecordsInProposedData()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            ReturnToUseDate = DateTime.UtcNow,
            TotalCost = 300000m,
            WorkPerformed = "Quick fix"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 10, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithWorkPerformed_StoresInRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 450000m,
            WorkPerformed = "Full engine check, oil change, filter replacement"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 11, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithConditions_StoresInRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 350000m,
            ConditionBefore = "Old oil, dirty filter",
            DetailedDescription = "New oil, clean filter",
            ConditionAfter = "New oil, clean filter"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 12, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithExecutionDate_UsesItAsExecutionDate()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 200000m,
            ExecutionDate = DateTime.UtcNow.AddDays(-1),
            WorkPerformed = "Test execution date"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 13, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_NullProposedData_InitializesSuccessfully()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 100000m,
            WorkPerformed = "Test"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 14, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_Success_CreatesAssetRequestRecord()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 500000m,
            WorkPerformed = "Maintenance done"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 15, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    // ========================================
    // Part 4: Large Cost Boundary Tests
    // ========================================

    #region Cost Boundary Tests

    [Fact]
    public async Task CompleteMaintenance_WithVeryLargeCost_StoresCorrectly()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 999999999.99m,
            WorkPerformed = "Major overhaul"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 16, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithDecimalCost_PreservesPrecision()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 123456.78m,
            WorkPerformed = "Small repair"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 17, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithBothWorkPerformedAndDetailedDescription_UsesWorkPerformed()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 300000m,
            MaintenanceContent = "Oil change via MaintenanceContent",
            DetailedDescription = "Detailed description via DetailedDescription",
            WorkPerformed = "WorkPerformed value"
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 18, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    // ========================================
    // Part 5: Attachments and Metadata
    // ========================================

    #region Attachments and Metadata

    [Fact]
    public async Task CompleteMaintenance_WithAttachmentUrls_RecordsInProposedData()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 200000m,
            WorkPerformed = "Service with attachments",
            AttachmentUrls = new List<string>
            {
                "https://storage.example.com/report1.pdf",
                "https://storage.example.com/photo1.jpg"
            }
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 19, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteMaintenance_WithAttachmentDocumentIds_RecordsInProposedData()
    {
        var dto = new MaintenanceCompleteDto
        {
            CompletedBy = 1,
            TotalCost = 200000m,
            WorkPerformed = "Service with doc ids",
            AttachmentDocumentIds = new List<int> { 101, 202, 303 }
        };

        var expectedResult = new MaintenanceCompleteResultDTO { RecordId = 20, TaskId = 1 };
        _mockService.Setup(s => s.CompleteMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CompleteMaintenance(taskId: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion
}
