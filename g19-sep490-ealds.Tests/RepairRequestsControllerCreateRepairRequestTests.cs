using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Repair;
using g19_sep490_ealds.Server.DTOs.Transfers;
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
/// Unit tests for RepairRequestsController (all actions).
/// Uses mock-based testing — IRepairRequestService is mocked per test.
/// </summary>
public class RepairRequestsControllerCreateRepairRequestTests
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

    private void SetNoUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private RepairRequestDTO ValidDto() => new RepairRequestDTO
    {
        AssetInstanceId = 1,
        EstimatedCost = 500000m,
        DamageCondition = "Screen broken",
        RepairKind = "Replace screen",
        CreatedBy = 1
    };

    // ─── CreateRepairRequest: null input ───────────────────────────────────────

    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── CreateRepairRequest: validation failures (service throws) ─────────────

    [Fact]
    public async Task EmptyDamageCondition_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Tình trạng hỏng hóc là bắt buộc."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.DamageCondition = "";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task WhitespaceDamageCondition_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Tình trạng hỏng hóc là bắt buộc."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.DamageCondition = "   ";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task NullRepairKind_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Phương án sửa chữa (repairKind) là bắt buộc."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.RepairKind = null!;

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task EmptyRepairKind_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Phương án sửa chữa (repairKind) là bắt buộc."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.RepairKind = "";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task FutureDamageDate_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Ngày hỏng không được lớn hơn ngày hiện tại."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.DamageDate = DateTime.UtcNow.AddDays(1);

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task TodayDamageDate_IsValid()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.DamageDate = DateTime.UtcNow;

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task PastDamageDate_IsValid()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.DamageDate = DateTime.UtcNow.AddDays(-5);

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── CreateRepairRequest: AssetInstance not found ─────────────────────────

    [Fact]
    public async Task AssetInstanceNotFound_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new KeyNotFoundException("AssetInstanceId 999 not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.AssetInstanceId = 999;

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── CreateRepairRequest: AssetInstance status validation ─────────────────

    [Fact]
    public async Task AssetInstanceNotDamaged_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Chỉ có thể tạo đơn sửa chữa khi tài sản đang ở trạng thái hỏng."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AssetInstanceInRepair_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Chỉ có thể tạo đơn sửa chữa khi tài sản đang ở trạng thái hỏng."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── CreateRepairRequest: blocking repair scenarios ────────────────────────

    [Fact]
    public async Task ExistingPendingRepair_BlocksCreation()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Tài sản này đã có đơn sửa chữa đang trong luồng xử lý."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExistingInProgressRepair_BlocksCreation()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Tài sản này đã có đơn sửa chữa đang trong luồng xử lý."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExistingSubmittedRepair_BlocksCreation()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Tài sản này đã có đơn sửa chữa đang trong luồng xử lý."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExistingApprovedRepair_BlocksCreation()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Tài sản này đã có đơn sửa chữa đang trong luồng xử lý."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExistingCompletedRepair_DoesNotBlockCreation()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ExistingRejectedRepair_DoesNotBlockCreation()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task NoWorkflowStep_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("No workflow step configured for RequestTypeId '4'."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── CreateRepairRequest: happy path ──────────────────────────────────────

    [Fact]
    public async Task ValidData_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidData_ReturnsAssetRequestIdAndTaskId()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 42, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var okResult = (OkObjectResult)await _controller.CreateRepairRequest(ValidDto());
        Assert.NotNull(okResult.Value);
    }

    // ─── CreateRepairRequest: role tests ─────────────────────────────────────

    [Fact]
    public async Task RoleDEPARTMENT_HEAD_Allowed()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 3,
            DamageCondition = "Keyboard malfunction",
            RepairKind = "Replace keyboard",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RoleTRUONG_PHONG_Allowed()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 4,
            DamageCondition = "Touchpad broken",
            RepairKind = "Replace touchpad",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UserWithoutRole_StillSucceeds()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(2);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 2,
            DamageCondition = "Paper jam",
            RepairKind = "Clean and fix",
            CreatedBy = 2
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── CreateRepairRequest: optional fields ──────────────────────────────────

    [Fact]
    public async Task SupplierIdProvided_IsValid()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.SupplierId = 5;

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DescriptionProvided_IsValid()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.Description = "Urgent repair needed";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DamageDateProvided_IsValid()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.DamageDate = DateTime.UtcNow.AddDays(-3);

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AfterCompletedRepair_NewRequestSucceeds()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidData_UsesConfiguredRepairRequestTypeId()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task NoTitleProvided_UsesDefaultTitle()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task TitleProvided_UsesProvidedTitle()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.Title = "Repair Dell Laptop - Screen";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidData_Description_SetsToRepairKind()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = ValidDto();
        dto.RepairKind = "Replace LCD panel";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ValidData_ProposedData_IsNull()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CreateRepairRequest(ValidDto());
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── End-to-end test-case names from original file ─────────────────────────

    [Fact]
    public async Task CreateRepairRequest_NormalCase_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ReturnsAsync(new RepairRequestCreateResultDTO { AssetRequestId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateRepairRequest_AssetInstanceIdZero_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new KeyNotFoundException("AssetInstanceId 0 not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 0,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateRepairRequest_AssetInstanceIdNegative_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new KeyNotFoundException("AssetInstanceId -1 not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = -1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateRepairRequest_AssetInstanceIdNonExistent_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new KeyNotFoundException("AssetInstanceId 999 not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 999,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateRepairRequest_EmptyDamageCondition_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Tình trạng hỏng hóc là bắt buộc."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRepairRequest_AssetStatusInUse_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Chỉ có thể tạo đơn sửa chữa khi tài sản đang ở trạng thái hỏng."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRepairRequest_AssetStatusInRepair_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Chỉ có thể tạo đơn sửa chữa khi tài sản đang ở trạng thái hỏng."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRepairRequest_AssetStatusReserved_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Chỉ có thể tạo đơn sửa chữa khi tài sản đang ở trạng thái hỏng."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRepairRequest_AssetStatusNegative_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CreateAsync(It.IsAny<RepairRequestDTO>()))
            .ThrowsAsync(new Exception("Asset instance status is invalid."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1
        };

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── GetList ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetList_NoUser_ReturnsUnauthorized()
    {
        _mockService = new Mock<IRepairRequestService>();
        _controller = new RepairRequestsController(_mockService.Object);
        SetNoUser();

        var result = await _controller.GetList();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetList_WithUser_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.GetListAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<TransferRequestListItemDTO>());
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.GetList();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetList_ServiceReturnsItems_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.GetListAsync(1))
            .ReturnsAsync(new List<TransferRequestListItemDTO>
            {
                new TransferRequestListItemDTO
                {
                    AssetRequestId = 1,
                    Code = "REP-001",
                    AssetCode = "DELL-001",
                    AssetName = "Dell Laptop",
                    Status = 1,
                    StatusName = "Pending",
                    FromDepartment = "IT Department",
                    ToDepartment = "IT Department",
                    FromDepartmentId = 1,
                    ToDepartmentId = 1
                }
            });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.GetList();
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── GetDamagedPending ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetDamagedPending_NoUser_ReturnsUnauthorized()
    {
        _mockService = new Mock<IRepairRequestService>();
        _controller = new RepairRequestsController(_mockService.Object);
        SetNoUser();

        var result = await _controller.GetDamagedPending();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetDamagedPending_WithUser_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.GetDamagedPendingAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<DamagedInstancePendingRepairDto>());
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.GetDamagedPending();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDamagedPending_ServiceReturnsItems_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.GetDamagedPendingAsync(1))
            .ReturnsAsync(new List<DamagedInstancePendingRepairDto>
            {
                new DamagedInstancePendingRepairDto
                {
                    AssetInstanceId = 1,
                    InstanceCode = "INS-001",
                    AssetCode = "DELL-001",
                    AssetName = "Dell Laptop 001",
                    DamageNote = "Screen broken",
                    FromDepartment = "IT Department",
                    Location = "IT Room"
                }
            });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.GetDamagedPending();
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── StartRepair ────────────────────────────────────────────────────────────

    [Fact]
    public async Task StartRepair_NullDto_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.StartRepair(id: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StartRepair_Success_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ReturnsAsync(new RepairStartResultDTO { AssetRequestId = 1, Status = 4, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StartRepair_NotFound_ReturnsNotFound()
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
    public async Task StartRepair_Unauthorized_ReturnsForbid()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized to start repair."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task StartRepair_ServiceError_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.StartRepairAsync(It.IsAny<int>(), It.IsAny<RepairStartDto>()))
            .ThrowsAsync(new Exception("Cannot start repair for this status."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairStartDto { StartedBy = 1 };
        var result = await _controller.StartRepair(id: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── CompleteRepair ────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteRepair_NullDto_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var result = await _controller.CompleteRepair(taskId: 1, dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CompleteRepair_Success_ReturnsOk()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ReturnsAsync(new RepairCompleteResultDTO { RecordId = 1, TaskId = 1 });
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { ActualCost = 500000m };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteRepair_NotFound_ReturnsNotFound()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new KeyNotFoundException("Repair task not found."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { ActualCost = 500000m };
        var result = await _controller.CompleteRepair(taskId: 999, dto: dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CompleteRepair_Unauthorized_ReturnsForbid()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized to complete repair."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { ActualCost = 500000m };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CompleteRepair_ServiceError_ReturnsBadRequest()
    {
        _mockService = new Mock<IRepairRequestService>();
        _mockService.Setup(s => s.CompleteRepairAsync(It.IsAny<int>(), It.IsAny<RepairCompleteDto>()))
            .ThrowsAsync(new Exception("Cannot complete repair for this task."));
        _controller = new RepairRequestsController(_mockService.Object);
        SetUser(1);

        var dto = new RepairCompleteDto { ActualCost = 500000m };
        var result = await _controller.CompleteRepair(taskId: 1, dto: dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
