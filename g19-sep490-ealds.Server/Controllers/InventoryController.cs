using System.Security.Claims;
using g19_sep490_ealds.Server.DTOs.Inventory;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    [HttpGet("sessions")]
    public async Task<ActionResult> GetSessions(
        [FromQuery] int? departmentId,
        [FromQuery] int? status,
        [FromQuery] string? keyword,
        [FromQuery] bool directorInventoryReport = false)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.GetSessionsAsync(userId, departmentId, status, keyword, directorInventoryReport));
    }

    [HttpGet("sessions/{id:int}")]
    public async Task<ActionResult> GetSessionById(int id)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.GetSessionByIdAsync(userId, id));
    }

    [HttpGet("sessions/{sessionId:int}/assets")]
    public async Task<ActionResult> GetSessionAssets(
        int sessionId,
        [FromQuery] string? keyword,
        [FromQuery] int? checkStatus)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.GetSessionAssetsAsync(userId, sessionId, keyword, checkStatus));
    }

    [HttpGet("sessions/{sessionId:int}/assets/{assetId:int}/items")]
    public async Task<ActionResult> GetSessionAssetsForCatalogAsset(
        int sessionId,
        int assetId,
        [FromQuery] string? keyword,
        [FromQuery] int? checkStatus)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.GetSessionAssetsForCatalogAssetAsync(userId, sessionId, assetId, keyword, checkStatus));
    }

    [HttpGet("sessions/{sessionId:int}/instances/{assetInstanceId:int}")]
    [HttpGet("sessions/{sessionId:int}/assets/{assetInstanceId:int}")]
    public async Task<ActionResult> GetAssetInventoryDetail(int sessionId, int assetInstanceId)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.GetAssetInventoryDetailAsync(userId, sessionId, assetInstanceId));
    }

    [HttpPut("sessions/{sessionId:int}/instances/{assetInstanceId:int}")]
    [HttpPut("sessions/{sessionId:int}/assets/{assetInstanceId:int}")]
    public async Task<ActionResult> SaveAssetInventory(int sessionId, int assetInstanceId, [FromBody] SaveAssetInventoryDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            await _inventoryService.SaveAssetInventoryAsync(userId, sessionId, assetInstanceId, dto);
            return (object)new { message = "Đã lưu thông tin kiểm kê." };
        });
    }

    [HttpPost("sessions")]
    public async Task<ActionResult> CreateSession([FromBody] CreateInventorySessionDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.CreateSessionAsync(userId, dto));
    }

    [HttpPost("sessions/{id:int}/tasks/{taskId:int}/record")]
    public async Task<ActionResult> SubmitTaskRecord(int id, int taskId, [FromBody] SubmitInventoryTaskDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.SubmitTaskRecordAsync(userId, id, taskId, dto));
    }

    [HttpPost("sessions/{id:int}/complete")]
    public async Task<ActionResult> CompleteSession(int id)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.CompleteSessionAsync(userId, id));
    }

    [HttpGet("sessions/{id:int}/review-summary")]
    public async Task<ActionResult> GetReviewSummary(int id)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.GetReviewSummaryAsync(userId, id));
    }

    [HttpPost("sessions/{id:int}/director-approve")]
    public async Task<ActionResult> DirectorApproveSession(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.DirectorApproveSessionAsync(userId, id, dto));
    }

    [HttpPost("sessions/{id:int}/reject")]
    public async Task<ActionResult> RequestInventoryRecheck(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            await _inventoryService.RequestInventoryRecheckAsync(userId, id, dto);
            return (object)new { message = "Đã gửi yêu cầu kiểm kê lại. Phiên chuyển sang Đang thực hiện.", sessionId = id };
        });
    }

    [HttpPost("sessions/{id:int}/confirm")]
    public async Task<ActionResult> DepartmentHeadFinishInventoryResolution(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            await _inventoryService.DepartmentHeadFinishInventoryResolutionAsync(userId, id, dto);
            return (object)new { message = "Đã hoàn tất. Phiên kiểm kê được đánh dấu Đã xử lý.", sessionId = id };
        });
    }

    [HttpPost("sessions/{sessionId:int}/discrepancies/{discrepancyId:int}/apply-actual")]
    public async Task<ActionResult> AccountantApplyDiscrepancyActual(int sessionId, int discrepancyId)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            await _inventoryService.AccountantApplyDiscrepancyActualAsync(userId, sessionId, discrepancyId);
            return (object)new { message = "Đã cập nhật sổ sách theo thông tin thực tế kiểm kê." };
        });
    }

    [HttpPut("sessions/{id:int}")]
    public async Task<ActionResult> UpdateSession(int id, [FromBody] UpdateInventorySessionDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            await _inventoryService.UpdateSessionAsync(userId, id, dto);
            return (object)new { message = "Đã cập nhật thông tin phiên kiểm kê." };
        });
    }

    [HttpPost("sessions/{id:int}/activate")]
    public async Task<ActionResult> ActivateSession(int id)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(async () =>
        {
            await _inventoryService.ActivateSessionAsync(userId, id);
            return (object)new { message = "Phiên kiểm kê đã được kích hoạt (Đang thực hiện)." };
        });
    }

    [HttpPost("sessions/{id:int}/cancel")]
    public async Task<ActionResult> CancelSession(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.CancelSessionAsync(userId, id, dto));
    }

    [HttpGet("sessions/{id:int}/discrepancies")]
    public async Task<ActionResult> GetDiscrepancies(int id)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await ExecuteAsync(() => _inventoryService.GetDiscrepanciesAsync(userId, id));
    }

    [HttpGet("meta/departments")]
    public async Task<ActionResult> GetDepartments()
        => await ExecuteAsync(() => _inventoryService.GetDepartmentsAsync());

    [HttpGet("meta/asset-categories")]
    public async Task<ActionResult> GetAssetCategories()
        => await ExecuteAsync(() => _inventoryService.GetAssetCategoriesAsync());

    [HttpGet("meta/asset-types")]
    public async Task<ActionResult> GetAssetTypes([FromQuery] int? categoryId)
        => await ExecuteAsync(() => _inventoryService.GetAssetTypesAsync(categoryId));

    [HttpGet("meta/users")]
    public async Task<ActionResult> GetUsers()
        => await ExecuteAsync(() => _inventoryService.GetUsersAsync());

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId) && userId > 0;
    }

    private async Task<ActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
