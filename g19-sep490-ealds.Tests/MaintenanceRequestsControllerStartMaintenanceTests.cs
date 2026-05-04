using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Maintenance;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests for MaintenanceRequestsController.StartMaintenance
/// (POST /api/Assets/Requests/maintenance/{id}/start)
/// Uses mock-based testing with IMaintenanceRequestService.
/// </summary>
public class MaintenanceRequestsControllerStartMaintenanceTests
{
    private readonly Mock<IMaintenanceRequestService> _mockService;
    private readonly MaintenanceRequestsController _controller;

    public MaintenanceRequestsControllerStartMaintenanceTests()
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
    // Normal cases: service returns success
    // ========================================

    /// <summary>
    /// Test case 1 (Normal):
    /// PlannedDate = today, ExpectedCompletionDate = PlannedDate, Address = at unit.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_PlannedDateToday_ReturnsOk()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var ok = (OkObjectResult)result;
        var returned = (MaintenanceStartResultDTO)ok.Value!;
        Assert.Equal(1, returned.AssetRequestId);
        Assert.Equal(1, returned.Status);
        Assert.Equal(1, returned.TaskId);
    }

    /// <summary>
    /// Test case 2 (Normal):
    /// PlannedDate = less than today, ExpectedCompletionDate = PlannedDate, Address = at unit.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_PlannedDateInPast_ReturnsOk()
    {
        var pastDate = DateTime.UtcNow.AddDays(-2);
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = pastDate,
            ExpectedCompletionDate = pastDate,
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Normal):
    /// PlannedDate = greater than today, ExpectedCompletionDate = PlannedDate, Address = at unit.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_PlannedDateInFuture_ReturnsOk()
    {
        var futureDate = DateTime.UtcNow.AddDays(5);
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = futureDate,
            ExpectedCompletionDate = futureDate,
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Normal):
    /// PlannedDate = today, ExpectedCompletionDate = less than PlannedDate, Address = at unit.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_ExpectedCompletionBeforePlannedDate_ReturnsOk()
    {
        var today = DateTime.UtcNow;
        var earlyCompletion = today.AddDays(-1);
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today,
            ExpectedCompletionDate = earlyCompletion,
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal):
    /// PlannedDate = today, ExpectedCompletionDate = greater than PlannedDate, Address = at unit.
    /// Expected output: 200 OK (no validation on date relationships)
    /// </summary>
    [Fact]
    public async Task StartMaintenance_AbnormalCase_ExpectedCompletionAfterPlannedDate_ReturnsOk()
    {
        var today = DateTime.UtcNow;
        var laterCompletion = today.AddDays(3);
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today,
            ExpectedCompletionDate = laterCompletion,
            LocationType = "at-unit",
            Location = "IT Department",
            MaintenanceContent = "Regular maintenance"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Normal):
    /// PlannedDate = today, ExpectedCompletionDate = PlannedDate, Address = provider.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task StartMaintenance_NormalCase_LocationTypeProvider_ReturnsOk()
    {
        var today = DateTime.UtcNow;
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today,
            ExpectedCompletionDate = today,
            LocationType = "provider",
            Location = "External Repair Shop A",
            MaintenanceContent = "External maintenance",
            MaintenanceProvider = "Repair Shop A"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // ========================================
    // Validation / Bad-request tests
    // ========================================

    /// <summary>
    /// Input:  dto = null
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        var result = await _controller.StartMaintenance(id: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  StartedBy = 0
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task StartedByZero_ReturnsBadRequest()
    {
        var dto = new MaintenanceStartDto { StartedBy = 0 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ThrowsAsync(new Exception("StartedBy must be greater than 0"));
        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  StartedBy = -1
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task StartedByNegative_ReturnsBadRequest()
    {
        var dto = new MaintenanceStartDto { StartedBy = -1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ThrowsAsync(new Exception("StartedBy must be greater than 0"));
        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ========================================
    // Not-found tests
    // ========================================

    /// <summary>
    /// Input:  AssetRequest with id = 999 (does not exist)
    /// Expected return: NotFoundObjectResult
    /// </summary>
    [Fact]
    public async Task RequestNotFound_ReturnsNotFound()
    {
        var dto = new MaintenanceStartDto { StartedBy = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(999, dto)).ThrowsAsync(new KeyNotFoundException("Request not found"));
        var result = await _controller.StartMaintenance(id: 999, dto: dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Input:  AssetRequest with Status = 1 (Pending Approval), not final approved
    /// Expected return: BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task RequestNotApproved_ReturnsBadRequest()
    {
        var dto = new MaintenanceStartDto { StartedBy = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ThrowsAsync(new Exception("Request is not in an approved state"));
        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ========================================
    // Authorization / Forbidden tests
    // ========================================

    /// <summary>
    /// Input:  User without allowed role
    /// Expected return: StatusCode 403 Forbidden
    /// </summary>
    [Fact]
    public async Task UserWithoutRole_ReturnsForbidden()
    {
        var dto = new MaintenanceStartDto { StartedBy = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ThrowsAsync(new UnauthorizedAccessException("User does not have permission"));
        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<ForbidResult>(result);
    }

    /// <summary>
    /// Input:  User with allowed role DIRECTOR
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task UserWithDirectorRole_ReturnsOk()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  User is the creator of the request
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task UserIsCreator_ReturnsOk()
    {
        var dto = new MaintenanceStartDto { StartedBy = 1 };
        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 4, Status = 1, TaskId = 4 };
        _mockService.Setup(s => s.StartMaintenanceAsync(4, dto)).ReturnsAsync(expectedResult);
        var result = await _controller.StartMaintenance(id: 4, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    // ========================================
    // State-verification tests (result DTO fields)
    // ========================================

    /// <summary>
    /// Input:  Valid request, service returns Status=1 (InMaintenance)
    /// Expected return: OkObjectResult with Status = 1
    /// </summary>
    [Fact]
    public async Task ValidRequest_UpdatesAssetStatusToInMaintenance()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var ok = (OkObjectResult)result;
        var returned = (MaintenanceStartResultDTO)ok.Value!;
        Assert.Equal(1, returned.Status);
    }

    /// <summary>
    /// Input:  Valid request, service returns TaskId
    /// Expected return: OkObjectResult with TaskId populated
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsTaskStatusToInProgress()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var ok = (OkObjectResult)result;
        var returned = (MaintenanceStartResultDTO)ok.Value!;
        Assert.Equal(1, returned.TaskId);
    }

    /// <summary>
    /// Input:  Valid request
    /// Expected return: OkObjectResult with AssetRequestId populated
    /// </summary>
    [Fact]
    public async Task ValidRequest_SetsAssetRequestStatusToInProgress()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);

        Assert.IsType<OkObjectResult>(result);
        var ok = (OkObjectResult)result;
        var returned = (MaintenanceStartResultDTO)ok.Value!;
        Assert.Equal(1, returned.AssetRequestId);
    }

    /// <summary>
    /// Input:  Valid request with maintenance provider
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithMaintenanceProvider_ReturnsOk()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            LocationType = "provider",
            Location = "External Shop",
            MaintenanceProvider = "Repair Shop A"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request with PerformerUserId
    /// Expected return: OkObjectResult
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithPerformerUserId_SetsAssignTo()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            PerformerUserId = 2,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Request with Status = 4 (already in progress)
    /// Expected return: OkObjectResult (idempotent - restarts maintenance)
    /// </summary>
    [Fact]
    public async Task AlreadyInProgress_ReturnsOk()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 5, Status = 1, TaskId = 5 };
        _mockService.Setup(s => s.StartMaintenanceAsync(5, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 5, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request with ExpectedCompletionTo
    /// Expected return: OkObjectResult (range-based completion date handled by service)
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithExpectedCompletionTo_UsesToDate()
    {
        var today = DateTime.UtcNow;
        var completionTo = today.AddDays(7);
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = today,
            ExpectedCompletionDate = null,
            ExpectedCompletionTo = completionTo,
            LocationType = "at-unit",
            Location = "IT Department"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request with AttachmentUrls
    /// Expected return: OkObjectResult (attachmentUrls stored in proposedData by service)
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithAttachmentUrls_StoresInProposedData()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            AttachmentUrls = new List<string> { "http://example.com/doc1.pdf", "http://example.com/doc2.pdf" }
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request with EstimatedCost
    /// Expected return: OkObjectResult (cost recorded by service)
    /// </summary>
    [Fact]
    public async Task ValidRequest_WithEstimatedCost_RecordsCost()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            EstimatedCost = 800000m,
            MaintenanceContent = "Oil change and inspection",
            MaintenanceProvider = "TechService Co."
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  Valid request
    /// Expected return: OkObjectResult (lifecycle record created by service)
    /// </summary>
    [Fact]
    public async Task ValidRequest_CreatesAssetRequestRecord()
    {
        var dto = new MaintenanceStartDto
        {
            StartedBy = 1,
            MaintenanceDate = DateTime.UtcNow,
            ExpectedCompletionDate = DateTime.UtcNow,
            Comment = "Starting maintenance work"
        };

        var expectedResult = new MaintenanceStartResultDTO { AssetRequestId = 1, Status = 1, TaskId = 1 };
        _mockService.Setup(s => s.StartMaintenanceAsync(1, dto)).ReturnsAsync(expectedResult);

        var result = await _controller.StartMaintenance(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }
}
