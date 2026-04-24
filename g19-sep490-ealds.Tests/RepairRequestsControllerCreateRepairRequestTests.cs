using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using Xunit;

namespace g19_sep490_ealds.Tests;

/// <summary>
/// Unit tests for RepairRequestsController.CreateRepairRequest
/// (POST /api/Assets/Requests/repair)
/// Role under test: Head of Department
/// </summary>
public class RepairRequestsControllerCreateRepairRequestTests
{
    private readonly EaldsDbContext _context;
    private readonly RepairRequestsController _controller;
    private readonly Mock<IAssetRequestNotificationService> _mockNotification;

    public RepairRequestsControllerCreateRepairRequestTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EaldsDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "App:RepairRequestTypeId", "4" }
            })
            .Build();

        _mockNotification = new Mock<IAssetRequestNotificationService>();
        _controller = new RepairRequestsController(_context, configuration, _mockNotification.Object);
        SetUser(actorUserId: 1);
    }

    // ─── Setup helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Simulates a logged-in user by setting the HttpContext.User with a NameIdentifier claim.
    /// Controller action: CreateRepairRequest → uses GetUserIdOrZero() to read user identity.
    /// </summary>
    private void SetUser(int actorUserId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, actorUserId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>
    /// Seeds: RequestType(RequestTypeId=4, WorkflowId=1) + WorkflowStep(StepId=10).
    /// Used by: All tests that call CreateRepairRequest (needs workflow to resolve initial StepId).
    /// </summary>
    private async Task SeedWorkflowAsync()
    {
        _context.RequestTypes.Add(new RequestType
        {
            RequestTypeId = 4,
            WorkflowId = 1
        });

        _context.WorkflowSteps.Add(new WorkflowStep
        {
            StepId = 10,
            WorkflowId = 1,
            StepOrder = 1,
            RoleId = 1,
            IsFinalStep = true
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds minimal data for a repair request to succeed:
    ///   Asset(AssetId=1) + AssetInstance(AssetInstanceId=1, Status=Damaged)
    ///   Employee(UserId=1) + Department(DepartmentId=1)
    ///   Role(RoleId=3, Code=HEAD_OF_DEPARTMENT) + UserRole(UserId=1 → RoleId=3)
    /// Used by: All happy-path and validation tests.
    /// </summary>
    private async Task SeedMinimalAsync()
    {
        await SeedWorkflowAsync();

        _context.Assets.Add(new Asset
        {
            AssetId = 1,
            Code = "DELL-001",
            Name = "Dell Laptop 001",
            AssetTypeId = 1,
            Status = 1,
            Unit = "pcs"
        });

        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 1,
            AssetId = 1,
            WarehouseId = 1,
            InstanceCode = "INS-001",
            Status = (int)AssetStatus.Damaged,
            Note = "Screen broken",
            InUseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-12)),
            OriginalPrice = 15000000m,
            CurrentValue = 12000000m
        });

        _context.Employees.Add(new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Nguyen Van A",
            Code = "NV001",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });

        _context.Departments.Add(new Department
        {
            DepartmentId = 1,
            Name = "IT Department"
        });

        _context.Roles.Add(new Role
        {
            RoleId = 3,
            Code = "HEAD_OF_DEPARTMENT",
            Name = "Head of Department"
        });

        _context.UserRoles.Add(new UserRole
        {
            UserId = 1,
            RoleId = 3
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Returns a minimal valid RepairRequestDTO.
    ///   AssetInstanceId=1, EstimatedCost=500000, DamageCondition="Screen broken",
    ///   RepairKind="Replace screen", CreatedBy=1
    /// Used by: All tests that pass a valid DTO.
    /// </summary>
    private RepairRequestDTO MinimalDto() => new RepairRequestDTO
    {
        AssetInstanceId = 1,
        EstimatedCost = 500000m,
        DamageCondition = "Screen broken",
        RepairKind = "Replace screen",
        CreatedBy = 1
    };

    // ─── Null / Invalid Input ──────────────────────────────────────────────────

    /// <summary>
    /// Input:  dto = null
    /// Expected return: BadRequestObjectResult (line: "Request body is required.")
    /// Controller line: if (dto == null) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task NullDto_ReturnsBadRequest()
    {
        var result = await _controller.CreateRepairRequest(dto: null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with DamageCondition = "" (empty string)
    /// Expected return: BadRequestObjectResult (line: "Tình trạng hỏng hóc là bắt buộc.")
    /// Controller line: if (string.IsNullOrWhiteSpace(dto.DamageCondition)) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task EmptyDamageCondition_ReturnsBadRequest()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.DamageCondition = "";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with DamageCondition = "   " (whitespace only)
    /// Expected return: BadRequestObjectResult (line: "Tình trạng hỏng hóc là bắt buộc.")
    /// Controller line: if (string.IsNullOrWhiteSpace(dto.DamageCondition)) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task WhitespaceDamageCondition_ReturnsBadRequest()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.DamageCondition = "   ";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with RepairKind = null
    /// Expected return: BadRequestObjectResult (line: "Phương án sửa chữa (repairKind) là bắt buộc.")
    /// Controller line: if (string.IsNullOrWhiteSpace(dto.RepairKind)) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task NullRepairKind_ReturnsBadRequest()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.RepairKind = null!;

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with RepairKind = "" (empty string)
    /// Expected return: BadRequestObjectResult (line: "Phương án sửa chữa (repairKind) là bắt buộc.")
    /// Controller line: if (string.IsNullOrWhiteSpace(dto.RepairKind)) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task EmptyRepairKind_ReturnsBadRequest()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.RepairKind = "";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with DamageDate = DateTime.UtcNow.AddDays(1) (future date)
    /// Expected return: BadRequestObjectResult (line: "Ngày hỏng không được lớn hơn ngày hiện tại.")
    /// Controller line: if (dto.DamageDate.HasValue && dto.DamageDate.Value.Date > DateTime.UtcNow.Date) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task FutureDamageDate_ReturnsBadRequest()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.DamageDate = DateTime.UtcNow.AddDays(1);

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with DamageDate = DateTime.UtcNow (today, boundary case)
    /// Expected return: OkObjectResult (date is not in the future, passes validation)
    /// Controller line: passes the date validation check, proceeds to asset lookup
    /// </summary>
    [Fact]
    public async Task TodayDamageDate_IsValid()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.DamageDate = DateTime.UtcNow;

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with DamageDate = DateTime.UtcNow.AddDays(-5) (past date)
    /// Expected return: OkObjectResult (past dates are valid)
    /// Controller line: passes the date validation check, proceeds to asset lookup
    /// </summary>
    [Fact]
    public async Task PastDamageDate_IsValid()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.DamageDate = DateTime.UtcNow.AddDays(-5);

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── AssetInstance not found ───────────────────────────────────────────────

    /// <summary>
    /// Input:  RepairRequestDTO with AssetInstanceId = 999 (does not exist in DB)
    /// Expected return: NotFoundObjectResult (line: "AssetInstanceId 999 not found.")
    /// Controller line: if (instance == null) return NotFound($"AssetInstanceId {dto.AssetInstanceId} not found.")
    /// </summary>
    [Fact]
    public async Task AssetInstanceNotFound_ReturnsNotFound()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.AssetInstanceId = 999;

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── AssetInstance status validation ──────────────────────────────────────

    /// <summary>
    /// Input:  RepairRequestDTO + AssetInstance with Status = InUse (not Damaged)
    /// Expected return: BadRequestObjectResult (line: "Chỉ có thể tạo đơn sửa chữa khi tài sản đang ở trạng thái hỏng.")
    /// Controller line: if (instance.Status != (int)AssetStatus.Damaged) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task AssetInstanceNotDamaged_ReturnsBadRequest()
    {
        await SeedMinimalAsync();
        var instance = await _context.AssetInstances.FindAsync(1);
        instance!.Status = (int)AssetStatus.InUse;
        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO + AssetInstance with Status = InRepair
    /// Expected return: BadRequestObjectResult (line: "Chỉ có thể tạo đơn sửa chữa khi tài sản đang ở trạng thái hỏng.")
    /// Controller line: if (instance.Status != (int)AssetStatus.Damaged) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task AssetInstanceInRepair_ReturnsBadRequest()
    {
        await SeedMinimalAsync();
        var instance = await _context.AssetInstances.FindAsync(1);
        instance!.Status = (int)AssetStatus.InRepair;
        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Blocking repair request ─────────────────────────────────────────────────

    /// <summary>
    /// Input:  MinimalDto + an existing AssetRequest with Status=1 (Pending Approval)
    ///         for the same AssetInstanceId=1
    /// Expected return: BadRequestObjectResult (line: "Tài sản này đã có đơn sửa chữa đang trong luồng xử lý.")
    /// Controller line: blockingStatuses = [0, 1, 2, 4]; if (hasBlocking) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task ExistingPendingRepair_BlocksCreation()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 100,
            UserId = 1,
            RequestTypeId = 4,
            Title = "Pending repair",
            Status = 1,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow.AddDays(-1),
            StepId = 10
        });

        _context.RepairTasks.Add(new RepairTask
        {
            TaskId = 100,
            AssetRequestId = 100,
            AssetInstanceId = 1,
            Status = 0,
            Reason = "Pending repair"
        });

        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  MinimalDto + an existing AssetRequest with Status=4 (In Repair)
    ///         for the same AssetInstanceId=1
    /// Expected return: BadRequestObjectResult (line: "Tài sản này đã có đơn sửa chữa đang trong luồng xử lý.")
    /// Controller line: blockingStatuses = [0, 1, 2, 4]; if (hasBlocking) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task ExistingInProgressRepair_BlocksCreation()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 101,
            UserId = 1,
            RequestTypeId = 4,
            Title = "In-progress repair",
            Status = 4,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow.AddDays(-2),
            StepId = 10
        });

        _context.RepairTasks.Add(new RepairTask
        {
            TaskId = 101,
            AssetRequestId = 101,
            AssetInstanceId = 1,
            Status = 1,
            Reason = "In-progress repair"
        });

        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  MinimalDto + an existing AssetRequest with Status=0 (Submitted)
    ///         for the same AssetInstanceId=1
    /// Expected return: BadRequestObjectResult (line: "Tài sản này đã có đơn sửa chữa đang trong luồng xử lý.")
    /// Controller line: blockingStatuses = [0, 1, 2, 4]; if (hasBlocking) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task ExistingSubmittedRepair_BlocksCreation()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 102,
            UserId = 1,
            RequestTypeId = 4,
            Title = "Submitted repair",
            Status = 0,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow.AddDays(-2),
            StepId = 10
        });

        _context.RepairTasks.Add(new RepairTask
        {
            TaskId = 102,
            AssetRequestId = 102,
            AssetInstanceId = 1,
            Status = 0,
            Reason = "Submitted repair"
        });

        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  MinimalDto + an existing AssetRequest with Status=2 (Approved)
    ///         for the same AssetInstanceId=1
    /// Expected return: BadRequestObjectResult (line: "Tài sản này đã có đơn sửa chữa đang trong luồng xử lý.")
    /// Controller line: blockingStatuses = [0, 1, 2, 4]; if (hasBlocking) return BadRequest(...)
    /// </summary>
    [Fact]
    public async Task ExistingApprovedRepair_BlocksCreation()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 103,
            UserId = 1,
            RequestTypeId = 4,
            Title = "Approved repair",
            Status = 2,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow.AddDays(-2),
            StepId = 10
        });

        _context.RepairTasks.Add(new RepairTask
        {
            TaskId = 103,
            AssetRequestId = 103,
            AssetInstanceId = 1,
            Status = 0,
            Reason = "Approved repair"
        });

        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Input:  MinimalDto + an existing AssetRequest with Status=5 (Completed)
    ///         for the same AssetInstanceId=1
    /// Expected return: OkObjectResult (completed repairs do not block new ones)
    /// Controller line: blockingStatuses = [0, 1, 2, 4]; status=5 is NOT in the blocklist, so hasBlocking=false
    /// </summary>
    [Fact]
    public async Task ExistingCompletedRepair_DoesNotBlockCreation()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 104,
            UserId = 1,
            RequestTypeId = 4,
            Title = "Completed repair",
            Status = 5,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow.AddDays(-10),
            StepId = 10
        });

        _context.RepairTasks.Add(new RepairTask
        {
            TaskId = 104,
            AssetRequestId = 104,
            AssetInstanceId = 1,
            Status = 2,
            Reason = "Completed repair"
        });

        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  MinimalDto + an existing AssetRequest with Status=3 (Rejected)
    ///         for the same AssetInstanceId=1
    /// Expected return: OkObjectResult (rejected repairs do not block new ones)
    /// Controller line: blockingStatuses = [0, 1, 2, 4]; status=3 is NOT in the blocklist, so hasBlocking=false
    /// </summary>
    [Fact]
    public async Task ExistingRejectedRepair_DoesNotBlockCreation()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 105,
            UserId = 1,
            RequestTypeId = 4,
            Title = "Rejected repair",
            Status = 3,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow.AddDays(-5),
            StepId = 10
        });

        _context.RepairTasks.Add(new RepairTask
        {
            TaskId = 105,
            AssetRequestId = 105,
            AssetInstanceId = 1,
            Status = 0,
            Reason = "Rejected repair"
        });

        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── No workflow step configured ─────────────────────────────────────────

    /// <summary>
    /// Input:  MinimalDto + WorkflowStep for RequestTypeId=4 has been deleted
    ///         (no initial step can be resolved)
    /// Expected return: BadRequestObjectResult (line: "No workflow step configured for RequestTypeId '4'.")
    /// Controller line: if (!initialStepId.HasValue) return BadRequest($"No workflow step configured...")
    /// </summary>
    [Fact]
    public async Task NoWorkflowStep_ReturnsBadRequest()
    {
        await SeedMinimalAsync();

        var step = await _context.WorkflowSteps.FindAsync(10);
        _context.WorkflowSteps.Remove(step!);
        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    /// <summary>
    /// Input:  MinimalDto (all required fields valid, asset is Damaged, no blocking repair)
    /// Expected return: OkObjectResult (controller creates the repair request successfully)
    /// </summary>
    [Fact]
    public async Task ValidData_ReturnsOk()
    {
        await SeedMinimalAsync();
        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: OkObjectResult with response object containing:
    ///                    - assetRequestId (int > 0)
    ///                    - taskId (int > 0)
    /// Controller line: return Ok(new { assetRequestId = ..., taskId = ... })
    /// </summary>
    [Fact]
    public async Task ValidData_ReturnsAssetRequestIdAndTaskId()
    {
        await SeedMinimalAsync();
        var okResult = (OkObjectResult)await _controller.CreateRepairRequest(MinimalDto());
        var response = okResult.Value;
        var requestId = (int)response!.GetType().GetProperty("assetRequestId")!.GetValue(response)!;
        var taskId = (int)response.GetType().GetProperty("taskId")!.GetValue(response)!;

        Assert.True(requestId > 0);
        Assert.True(taskId > 0);
    }

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: On success, an AssetRequest record with RequestTypeId=4 is persisted in DB.
    /// Controller line: _db.AssetRequests.Add(assetRequest); await _db.SaveChangesAsync();
    /// </summary>
    [Fact]
    public async Task ValidData_CreatesAssetRequest()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var request = await _context.AssetRequests.FirstOrDefaultAsync(r => r.RequestTypeId == 4);
        Assert.NotNull(request);
    }

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: On success, AssetRequest.Status = 1 (Pending Approval)
    /// Controller line: Status = 1 (set on the assetRequest object before saving)
    /// </summary>
    [Fact]
    public async Task ValidData_AssetRequestStatus_IsPendingApproval()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var request = await _context.AssetRequests.FirstOrDefaultAsync(r => r.RequestTypeId == 4);
        Assert.Equal(1, request!.Status); // 1 = Pending Approval
    }

    /// <summary>
    /// Input:  MinimalDto (AssetInstanceId=1, AssetId=1)
    /// Expected return: On success, AssetRequest.AssetInstanceId=1 and AssetRequest.AssetId=1
    /// Controller line: AssetId = instance.AssetId; AssetInstanceId = dto.AssetInstanceId;
    /// </summary>
    [Fact]
    public async Task ValidData_AssetRequestLinkedToCorrectInstance()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var request = await _context.AssetRequests.FirstOrDefaultAsync(r => r.RequestTypeId == 4);
        Assert.Equal(1, request!.AssetInstanceId);
        Assert.Equal(1, request.AssetId);
    }

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: On success, a RepairTask record linked to AssetInstanceId=1 is persisted.
    /// Controller line: _db.RepairTasks.Add(repairTask); await _db.SaveChangesAsync();
    /// </summary>
    [Fact]
    public async Task ValidData_CreatesRepairTask()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetInstanceId == 1);
        Assert.NotNull(task);
    }

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: On success, RepairTask.Status = 0 (Pending, not yet started)
    /// Controller line: repairTask.Status = 0 (set before saving)
    /// </summary>
    [Fact]
    public async Task ValidData_RepairTaskStatus_IsPending()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetInstanceId == 1);
        Assert.Equal(0, task!.Status); // 0 = Pending
    }

    /// <summary>
    /// Input:  RepairRequestDTO with DamageCondition = "Keyboard damaged"
    /// Expected return: On success, RepairTask.Reason = "Keyboard damaged"
    /// Controller line: repairTask.Reason = dto.DamageCondition.Trim();
    /// </summary>
    [Fact]
    public async Task ValidData_RepairTaskReason_SetsDamageCondition()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.DamageCondition = "Keyboard damaged";
        await _controller.CreateRepairRequest(dto);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetInstanceId == 1);
        Assert.Equal("Keyboard damaged", task!.Reason);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with EstimatedCost = 2000000
    /// Expected return: On success, RepairTask.EstimatedCost = 2000000
    /// Controller line: repairTask.EstimatedCost = dto.EstimatedCost;
    /// </summary>
    [Fact]
    public async Task ValidData_RepairTaskEstimatedCost_SetsFromDto()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.EstimatedCost = 2000000m;
        await _controller.CreateRepairRequest(dto);

        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetInstanceId == 1);
        Assert.Equal(2000000m, task!.EstimatedCost);
    }

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: On success, an AssetRequestRecord is persisted (action log for the creation event).
    /// Controller line: _db.AssetRequestRecords.Add(record); await _db.SaveChangesAsync();
    /// </summary>
    [Fact]
    public async Task ValidData_CreatesAssetRequestRecord()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var record = await _context.AssetRequestRecords.FirstOrDefaultAsync(r => r.ActionByUserId == 1);
        Assert.NotNull(record);
    }

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: On success, AssetRequestRecord.FromStatus == ToStatus (both = 1, pending)
    /// Controller line: new AssetRequestRecord { FromStatus = assetRequest.Status, ToStatus = assetRequest.Status }
    /// </summary>
    [Fact]
    public async Task ValidData_AssetRequestRecord_FromStatusEqualsToStatus()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var record = await _context.AssetRequestRecords.FirstOrDefaultAsync(r => r.ActionByUserId == 1);
        Assert.Equal(record!.FromStatus, record.ToStatus);
    }

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: On success, IAssetRequestNotificationService.NotifyFirstApproversAsync
    ///                  is called exactly once with the new assetRequestId.
    /// Controller line: await _requestNotifications.NotifyFirstApproversAsync(assetRequest.AssetRequestId);
    /// </summary>
    [Fact]
    public async Task ValidData_CallsNotifyFirstApprovers()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        _mockNotification.Verify(
            s => s.NotifyFirstApproversAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── Auto-generated title ───────────────────────────────────────────────────

    /// <summary>
    /// Input:  MinimalDto (Title = null)
    /// Expected return: On success, AssetRequest.Title = "Repair request for instance 1"
    ///                  (auto-generated from AssetInstanceId)
    /// Controller line: var title = dto.Title ?? $"Repair request for instance {dto.AssetInstanceId}";
    /// </summary>
    [Fact]
    public async Task NoTitleProvided_UsesDefaultTitle()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var request = await _context.AssetRequests.FirstOrDefaultAsync(r => r.RequestTypeId == 4);
        Assert.Contains("Repair request for instance 1", request!.Title);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with Title = "Repair Dell Laptop - Screen"
    /// Expected return: On success, AssetRequest.Title = "Repair Dell Laptop - Screen" (provided value used)
    /// Controller line: var title = dto.Title ?? $"Repair request for instance {dto.AssetInstanceId}";
    /// </summary>
    [Fact]
    public async Task TitleProvided_UsesProvidedTitle()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.Title = "Repair Dell Laptop - Screen";
        await _controller.CreateRepairRequest(dto);

        var request = await _context.AssetRequests.FirstOrDefaultAsync(r => r.RequestTypeId == 4);
        Assert.Equal("Repair Dell Laptop - Screen", request!.Title);
    }

    // ─── Description from RepairKind ──────────────────────────────────────────

    /// <summary>
    /// Input:  RepairRequestDTO with RepairKind = "Replace LCD panel"
    /// Expected return: On success, AssetRequest.Description = "Replace LCD panel"
    ///                  (RepairKind is stored as the request's Description field)
    /// Controller line: Description = dto.RepairKind!.Trim();
    /// </summary>
    [Fact]
    public async Task ValidData_Description_SetsToRepairKind()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.RepairKind = "Replace LCD panel";
        await _controller.CreateRepairRequest(dto);

        var request = await _context.AssetRequests.FirstOrDefaultAsync(r => r.RequestTypeId == 4);
        Assert.Equal("Replace LCD panel", request!.Description);
    }

    // ─── ProposedData null ─────────────────────────────────────────────────────

    /// <summary>
    /// Input:  MinimalDto (ProposedData not provided)
    /// Expected return: On success, AssetRequest.ProposedData = null (no proposal data set at creation)
    /// Controller line: ProposedData = null (explicitly set on assetRequest before saving)
    /// </summary>
    [Fact]
    public async Task ValidData_ProposedData_IsNull()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var request = await _context.AssetRequests.FirstOrDefaultAsync(r => r.RequestTypeId == 4);
        Assert.Null(request!.ProposedData);
    }

    // ─── Supplier & optional fields ─────────────────────────────────────────────

    /// <summary>
    /// Input:  RepairRequestDTO with SupplierId = 5 (optional field)
    /// Expected return: OkObjectResult (SupplierId does not block creation; stored in DTO only)
    /// Controller line: no SupplierId validation; no blocking effect
    /// </summary>
    [Fact]
    public async Task SupplierIdProvided_IsValid()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.SupplierId = 5;

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Input:  RepairRequestDTO with Description = "Urgent repair needed" (optional field)
    /// Expected return: OkObjectResult (Description does not block creation)
    /// Controller line: no Description validation; no blocking effect
    /// </summary>
    [Fact]
    public async Task DescriptionProvided_IsValid()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.Description = "Urgent repair needed";

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── DamageDate stored ──────────────────────────────────────────────────────

    /// <summary>
    /// Input:  RepairRequestDTO with DamageDate = DateTime.UtcNow.AddDays(-3) (past, valid)
    /// Expected return: OkObjectResult (DamageDate passes the future-date check)
    /// Controller line: DamageDate is assigned to dto.DamageDate but not persisted to any entity at creation
    ///                  (it passes validation but is not written to DB at this stage)
    /// </summary>
    [Fact]
    public async Task DamageDateProvided_IsValid()
    {
        await SeedMinimalAsync();
        var dto = MinimalDto();
        dto.DamageDate = DateTime.UtcNow.AddDays(-3);

        var result = await _controller.CreateRepairRequest(dto);
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── Role not found → fallback roleId = 1 ─────────────────────────────────

    /// <summary>
    /// Input:  RepairRequestDTO with CreatedBy = 2 (user has no UserRole record)
    /// Expected return: OkObjectResult (creation succeeds; roleId falls back to 1)
    /// Controller line: var userRole = await _db.UserRoles.AsNoTracking()
    ///                   .FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
    ///                  var actionRoleId = userRole?.RoleId ?? 1;
    /// </summary>
    [Fact]
    public async Task UserWithoutRole_StillSucceeds()
    {
        await SeedWorkflowAsync();

        _context.Assets.Add(new Asset
        {
            AssetId = 2,
            Code = "HP-001",
            Name = "HP Printer 001",
            AssetTypeId = 1,
            Status = 1,
            Unit = "pcs"
        });

        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 2,
            AssetId = 2,
            WarehouseId = 1,
            InstanceCode = "INS-002",
            Status = (int)AssetStatus.Damaged,
            Note = "Paper jam"
        });

        _context.Employees.Add(new Employee
        {
            EmployeeId = 2,
            UserId = 2,
            DepartmentId = 1,
            Name = "Nguyen Van B",
            Code = "NV002",
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreatedBy = 1
        });

        await _context.SaveChangesAsync();

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

    // ─── Multiple requests for same instance ──────────────────────────────────

    /// <summary>
    /// Input:  MinimalDto + an existing completed repair + AssetInstance marked Damaged again
    /// Expected return: OkObjectResult (completed repair is not blocking; re-damage allows new request)
    /// Controller line: blockingStatuses = [0, 1, 2, 4]; status=5 is NOT blocked
    /// </summary>
    [Fact]
    public async Task AfterCompletedRepair_NewRequestSucceeds()
    {
        await SeedMinimalAsync();

        _context.AssetRequests.Add(new AssetRequest
        {
            AssetRequestId = 200,
            UserId = 1,
            RequestTypeId = 4,
            Title = "Old completed repair",
            Status = 5,
            CreatedBy = 1,
            CreateDate = DateTime.UtcNow.AddDays(-30),
            StepId = 10
        });

        _context.RepairTasks.Add(new RepairTask
        {
            TaskId = 200,
            AssetRequestId = 200,
            AssetInstanceId = 1,
            Status = 2,
            Reason = "Old repair"
        });

        await _context.SaveChangesAsync();

        // Re-mark the asset as Damaged (simulating re-damage)
        var instance = await _context.AssetInstances.FindAsync(1);
        instance!.Status = (int)AssetStatus.Damaged;
        await _context.SaveChangesAsync();

        var result = await _controller.CreateRepairRequest(MinimalDto());
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── RequestTypeId from configuration ─────────────────────────────────────

    /// <summary>
    /// Input:  MinimalDto
    /// Expected return: On success, AssetRequest.RequestTypeId = 4
    ///                  (matches App:RepairRequestTypeId = "4" in configuration)
    /// Controller line: _repairRequestTypeId = configuration.GetValue<int>("App:RepairRequestTypeId", 4);
    ///                  RequestTypeId = _repairRequestTypeId;
    /// </summary>
    [Fact]
    public async Task ValidData_UsesConfiguredRepairRequestTypeId()
    {
        await SeedMinimalAsync();
        await _controller.CreateRepairRequest(MinimalDto());

        var request = await _context.AssetRequests.FirstOrDefaultAsync(r => r.RequestTypeId == 4);
        Assert.NotNull(request);
    }

    // ─── Different head department role variants ────────────────────────────────

    /// <summary>
    /// Input:  RepairRequestDTO with user assigned Role(Code="DEPARTMENT_HEAD")
    /// Expected return: OkObjectResult (DEPARTMENT_HEAD is a valid head-of-department role)
    /// Note: CreateRepairRequest does NOT restrict by role (StartRepair does), so this
    ///       confirms the endpoint is accessible to any authenticated user with a valid DTO.
    ///       The role is only recorded in AssetRequestRecord.ActionRoleId for audit purposes.
    /// </summary>
    [Fact]
    public async Task RoleDEPARTMENT_HEAD_Allowed()
    {
        await SeedWorkflowAsync();

        _context.Roles.Add(new Role
        {
            RoleId = 4,
            Code = "DEPARTMENT_HEAD",
            Name = "Department Head"
        });

        _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 4 });

        _context.Assets.Add(new Asset
        {
            AssetId = 3,
            Code = "LENOVO-001",
            Name = "Lenovo Laptop 001",
            AssetTypeId = 1,
            Status = 1,
            Unit = "pcs"
        });

        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 3,
            AssetId = 3,
            WarehouseId = 1,
            InstanceCode = "INS-003",
            Status = (int)AssetStatus.Damaged
        });

        await _context.SaveChangesAsync();

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

    /// <summary>
    /// Input:  RepairRequestDTO with user assigned Role(Code="TRUONG_PHONG", Name="Trưởng phòng")
    /// Expected return: OkObjectResult (TRUONG_PHONG is a Vietnamese head-of-department role variant)
    /// Note: CreateRepairRequest does NOT restrict by role; the role is recorded for audit only.
    /// </summary>
    [Fact]
    public async Task RoleTRUONG_PHONG_Allowed()
    {
        await SeedWorkflowAsync();

        _context.Roles.Add(new Role
        {
            RoleId = 5,
            Code = "TRUONG_PHONG",
            Name = "Trưởng phòng"
        });

        _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 5 });

        _context.Assets.Add(new Asset
        {
            AssetId = 4,
            Code = "ASUS-001",
            Name = "Asus Laptop 001",
            AssetTypeId = 1,
            Status = 1,
            Unit = "pcs"
        });

        _context.AssetInstances.Add(new AssetInstance
        {
            AssetInstanceId = 4,
            AssetId = 4,
            WarehouseId = 1,
            InstanceCode = "INS-004",
            Status = (int)AssetStatus.Damaged
        });

        await _context.SaveChangesAsync();

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

    /// <summary>
    /// Test case 1 (Normal): AssetInstanceId = 1, DamageCondition = Valid reason, Status = 0 (Damaged).
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_NormalCase_ReturnsOk()
    {
        // Arrange
        await SeedMinimalAsync();
        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = 0
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var task = await _context.RepairTasks.FirstOrDefaultAsync(t => t.AssetInstanceId == 1);
        Assert.NotNull(task);
        Assert.Equal(0, task.Status); // Status = Pending
    }

    /// <summary>
    /// Test case 2 (Abnormal): AssetInstanceId = 0, DamageCondition = Valid reason, Status = 0.
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_AssetInstanceIdZero_ReturnsNotFound()
    {
        // Arrange
        await SeedMinimalAsync();
        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 0,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = 0
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 3 (Abnormal): AssetInstanceId = -1, DamageCondition = Valid reason, Status = 0.
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_AssetInstanceIdNegative_ReturnsNotFound()
    {
        // Arrange
        await SeedMinimalAsync();
        var dto = new RepairRequestDTO
        {
            AssetInstanceId = -1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = 0
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): AssetInstanceId = 999, DamageCondition = Valid reason, Status = 0.
    /// Expected output: 404 Not Found
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_AssetInstanceIdNonExistent_ReturnsNotFound()
    {
        // Arrange
        await SeedMinimalAsync();
        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 999,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = 0
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Abnormal): AssetInstanceId = 1, DamageCondition = Empty, Status = 0.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_EmptyDamageCondition_ReturnsBadRequest()
    {
        // Arrange
        await SeedMinimalAsync();
        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = 0
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Abnormal): AssetInstanceId = 1, DamageCondition = Valid reason, Status = 1 (InUse).
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_AssetStatusInUse_ReturnsBadRequest()
    {
        // Arrange
        await SeedMinimalAsync();
        var instance = await _context.AssetInstances.FindAsync(1);
        instance!.Status = 1; // InUse
        await _context.SaveChangesAsync();

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = 1
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): AssetInstanceId = 1, DamageCondition = Valid reason, Status = 2 (InRepair).
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_AssetStatusInRepair_ReturnsBadRequest()
    {
        // Arrange
        await SeedMinimalAsync();
        var instance = await _context.AssetInstances.FindAsync(1);
        instance!.Status = 2; // InRepair
        await _context.SaveChangesAsync();

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = 2
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): AssetInstanceId = 1, DamageCondition = Valid reason, Status = 3 (Reserved).
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_AssetStatusReserved_ReturnsBadRequest()
    {
        // Arrange
        await SeedMinimalAsync();
        var instance = await _context.AssetInstances.FindAsync(1);
        instance!.Status = 3; // Reserved
        await _context.SaveChangesAsync();

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = 3
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 9 (Abnormal): AssetInstanceId = 1, DamageCondition = Valid reason, Status = -1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateRepairRequest_AssetStatusNegative_ReturnsBadRequest()
    {
        // Arrange
        await SeedMinimalAsync();
        var instance = await _context.AssetInstances.FindAsync(1);
        instance!.Status = -1;
        await _context.SaveChangesAsync();

        var dto = new RepairRequestDTO
        {
            AssetInstanceId = 1,
            DamageCondition = "Screen broken",
            RepairKind = "Replace screen",
            CreatedBy = 1,
            Status = -1
        };

        // Act
        var result = await _controller.CreateRepairRequest(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
