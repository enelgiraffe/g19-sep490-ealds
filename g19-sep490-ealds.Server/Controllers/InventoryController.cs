using System.Security.Claims;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.DTOs.Inventory;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<InventoryController> _logger;
    private readonly IInventoryNotificationService _inventoryNotifications;

    public InventoryController(
        EaldsDbContext context,
        ILogger<InventoryController> logger,
        IInventoryNotificationService inventoryNotifications)
    {
        _context = context;
        _logger = logger;
        _inventoryNotifications = inventoryNotifications;
    }

    /// <summary>
    /// GET /api/inventory/sessions - List all inventory sessions with optional filters
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<InventorySessionListItemDTO>>> GetSessions(
        [FromQuery] int? departmentId,
        [FromQuery] int? status,
        [FromQuery] string? keyword)
    {
        var access = await GetInventoryAccessAsync();
        if (access.RestrictToDepartment)
        {
            if (!access.DepartmentId.HasValue)
                return BadRequest(new { message = "Không xác định được phòng ban của bạn." });
        }

        var query = _context.InventorySessions
            .Include(s => s.Department)
            .Include(s => s.AssetCategory)
            .Include(s => s.AssetType)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryDiscrepancies)
            .AsNoTracking()
            .AsQueryable();

        if (access.RestrictToDepartment)
            query = query.Where(s => s.DepartmentId == access.DepartmentId!.Value);
        else if (departmentId.HasValue)
            query = query.Where(s => s.DepartmentId == departmentId.Value);

        if (status.HasValue)
        {
            // Status 5 ("Đến lịch") is a computed display status: DB status=0 with StartDate ≤ today ≤ EndDate
            var timeNow = DateTime.UtcNow;
            if (status.Value == 5)
                query = query.Where(s => s.Status == 0 && s.StartDate <= timeNow && s.EndDate >= timeNow);
            else
                query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(s =>
                s.Code.ToLower().Contains(kw) ||
                s.Purpose.ToLower().Contains(kw));
        }

        var sessions = await query
            .OrderByDescending(s => s.CreateDate)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var result = sessions.Select(s =>
        {
            var displayStatus = GetDisplayStatus(s, now);
            return new InventorySessionListItemDTO
            {
                SessionId = s.SessionId,
                Code = s.Code,
                Purpose = s.Purpose,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department.Name,
                AssetCategoryName = s.AssetCategory?.Name ?? string.Empty,
                AssetTypeName = s.AssetType?.Name ?? string.Empty,
                Status = displayStatus,
                StatusName = GetSessionStatusName(displayStatus),
                ProgressPercent = s.ProgressPercent,
                TotalTasks = s.InventoryTasks.Count(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status)),
                CompletedTasks = s.InventoryTasks.Count(t =>
                    !IsExcludedFromInventoryExecution(t.AssetInstance.Status) &&
                    t.Status == (int)InventoryTaskStatus.Checked),
                CreateDate = s.CreateDate,
                IsPeriodic = s.IsPeriodic,
                PeriodDays = s.PeriodDays,
                UnresolvedDiscrepancyCount = s.InventoryTasks
                    .Where(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status))
                    .SelectMany(t => t.InventoryDiscrepancies)
                    .Count(d => d.ResolvedAt == null)
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// GET /api/inventory/sessions/{id} - Get session detail with all tasks, records, and discrepancies
    /// </summary>
    [HttpGet("sessions/{id:int}")]
    public async Task<ActionResult<InventorySessionDetailDTO>> GetSessionById(int id)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions
            .Include(s => s.Department)
            .Include(s => s.AssetCategory)
            .Include(s => s.AssetType)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.AssetLocations)
                        .ThenInclude(al => al.Department)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryRecords)
                    .ThenInclude(r => r.ActualLocation)
                        .ThenInclude(al => al.Department)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryDiscrepancies)
                    .ThenInclude(d => d.ActualLocation)
                        .ThenInclude(al => al.Department)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryDiscrepancies)
                    .ThenInclude(d => d.BookLocation)
                        .ThenInclude(bl => bl.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        // Load book user names (employee name via AssetUsage on instance)
        var instanceIds = session.InventoryTasks.Select(t => t.AssetInstanceId).ToList();
        var bookUserMap = await _context.AssetUsages
            .Where(u => instanceIds.Contains(u.AssetInstanceId) && u.IsCurrent)
            .Include(u => u.Employee)
            .AsNoTracking()
            .ToDictionaryAsync(
                u => u.AssetInstanceId,
                u => new { UserId = u.Employee.UserId, UserName = u.Employee.Name });

        // Load actual user names for inventory records
        var actualUserIds = session.InventoryTasks
            .SelectMany(t => t.InventoryRecords)
            .Where(r => r.ActualUserId.HasValue)
            .Select(r => r.ActualUserId!.Value)
            .Distinct()
            .ToList();

        var actualUserMap = await _context.Users
            .Where(u => actualUserIds.Contains(u.UserId))
            .AsNoTracking()
            .ToDictionaryAsync(u => u.UserId, u => u.Email);

        var discrepancyUserNameMap = await GetUserDisplayNamesAsync(
            session.InventoryTasks
                .SelectMany(t => t.InventoryDiscrepancies)
                .SelectMany(d => new[] { d.BookUserId, d.ActualUserId }));

        const int bookQtyPerInstance = 1;
        var taskDTOs = session.InventoryTasks.Select(t =>
        {
            var inst = t.AssetInstance;
            var asset = inst.Asset;
            var bookLocation = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent);
            bookUserMap.TryGetValue(t.AssetInstanceId, out var bookUser);

            var record = t.InventoryRecords.FirstOrDefault();
            InventoryRecordDTO? recordDTO = null;
            if (record != null)
            {
                actualUserMap.TryGetValue(record.ActualUserId ?? 0, out var actualUserEmail);
                recordDTO = new InventoryRecordDTO
                {
                    RecordId = record.RecordId,
                    IsFound = record.IsFound,
                    ActualCondition = record.ActualCondition,
                    ActualDepartmentId = record.ActualLocation.DepartmentId,
                    ActualDepartmentName = record.ActualLocation.Department?.Name,
                    ActualUserId = record.ActualUserId,
                    ActualUserName = record.ActualUserId.HasValue ? actualUserEmail : null,
                    CheckedDate = record.CheckedDate
                };
            }

            var discrepancyDTOs = t.InventoryDiscrepancies.Select(d => new InventoryDiscrepancyDTO
            {
                DiscrepancyId = d.DiscrepancyId,
                DiscrepancyType = d.DiscrepancyType,
                DiscrepancyTypeName = BuildDiscrepancyTypeName(d.DiscrepancyType),
                BookValue = d.BookValue,
                BookQuantity = bookQtyPerInstance,
                ActualQuantity = record?.ActualQuantity,
                TaskNote = t.Note,
                BookDepartmentName = d.BookLocation?.Department?.Name,
                BookUserId = d.BookUserId,
                BookUserName = ResolveDisplayName(discrepancyUserNameMap, d.BookUserId),
                BookCondition = d.BookCondition,
                ActualValue = d.ActualValue,
                ActualDepartmentName = d.ActualLocation?.Department?.Name,
                ActualUserId = d.ActualUserId,
                ActualUserName = ResolveDisplayName(discrepancyUserNameMap, d.ActualUserId),
                ActualCondition = record?.ActualCondition ?? d.ActualCondition,
                ResolvedAt = d.ResolvedAt
            }).ToList();

            return new InventoryTaskDTO
            {
                TaskId = t.TaskId,
                AssetId = asset.AssetId,
                AssetInstanceId = inst.AssetInstanceId,
                AssetCode = asset.Code,
                InstanceCode = inst.InstanceCode,
                AssetName = asset.Name,
                BookCondition = ((AssetStatus)inst.Status).ToString(),
                BookDepartmentId = bookLocation?.DepartmentId,
                BookDepartmentName = bookLocation?.Department?.Name,
                BookUserId = bookUser?.UserId,
                BookUserName = bookUser?.UserName,
                BookValue = inst.CurrentValue,
                Status = t.Status,
                StatusName = t.Status == (int)InventoryTaskStatus.Checked ? "Đã kiểm kê" : "Chưa kiểm kê",
                CheckDate = t.CheckDate,
                Note = t.Note,
                Record = recordDTO,
                Discrepancies = discrepancyDTOs
            };
        }).ToList();

        var allDiscrepancies = session.InventoryTasks
            .SelectMany(t => t.InventoryDiscrepancies)
            .ToList();

        var detailDisplayStatus = GetDisplayStatus(session, DateTime.UtcNow);
        var dto = new InventorySessionDetailDTO
        {
            SessionId = session.SessionId,
            Code = session.Code,
            Purpose = session.Purpose,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            DepartmentId = session.DepartmentId,
            DepartmentName = session.Department.Name,
            AssetCategoryName = session.AssetCategory?.Name ?? string.Empty,
            AssetTypeName = session.AssetType?.Name ?? string.Empty,
            Status = detailDisplayStatus,
            StatusName = GetSessionStatusName(detailDisplayStatus),
            ProgressPercent = session.ProgressPercent,
            TotalTasks = session.InventoryTasks.Count(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status)),
            CompletedTasks = session.InventoryTasks.Count(t =>
                !IsExcludedFromInventoryExecution(t.AssetInstance.Status) &&
                t.Status == (int)InventoryTaskStatus.Checked),
            CreateDate = session.CreateDate,
            UnresolvedDiscrepancyCount = allDiscrepancies.Count(d => d.ResolvedAt == null),
            QuantityDiffCount = allDiscrepancies.Count(d =>
                (d.DiscrepancyType & (int)DiscrepancyType.AssetNotFound) != 0 ||
                (d.DiscrepancyType & (int)DiscrepancyType.QuantityMismatch) != 0),
            LocationChangeCount = allDiscrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.LocationMismatch) != 0),
            DepartmentChangeCount = allDiscrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.LocationMismatch) != 0),
            ConditionChangeCount = allDiscrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.ConditionMismatch) != 0),
            Tasks = taskDTOs
        };

        return Ok(dto);
    }

    /// <summary>
    /// GET /api/inventory/sessions/{sessionId}/assets — List every <b>asset instance</b> check row for the session (one row per physical item).
    /// </summary>
    [HttpGet("sessions/{sessionId:int}/assets")]
    public async Task<ActionResult<IEnumerable<SessionAssetCheckItemDTO>>> GetSessionAssets(
        int sessionId,
        [FromQuery] string? keyword,
        [FromQuery] int? checkStatus)
    {
        return await GetSessionAssetCheckItemsAsync(sessionId, catalogAssetId: null, keyword, checkStatus);
    }

    /// <summary>
    /// GET /api/inventory/sessions/{sessionId}/assets/{assetId}/items — Same as /assets, but only instances belonging to catalog asset <paramref name="assetId"/>.
    /// </summary>
    [HttpGet("sessions/{sessionId:int}/assets/{assetId:int}/items")]
    public async Task<ActionResult<IEnumerable<SessionAssetCheckItemDTO>>> GetSessionAssetsForCatalogAsset(
        int sessionId,
        int assetId,
        [FromQuery] string? keyword,
        [FromQuery] int? checkStatus)
    {
        return await GetSessionAssetCheckItemsAsync(sessionId, catalogAssetId: assetId, keyword, checkStatus);
    }

    private async Task<ActionResult<IEnumerable<SessionAssetCheckItemDTO>>> GetSessionAssetCheckItemsAsync(
        int sessionId,
        int? catalogAssetId,
        string? keyword,
        int? checkStatus)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(sessionId);
        if (gate != null) return gate;

        var sessionExists = await _context.InventorySessions.AnyAsync(s => s.SessionId == sessionId);
        if (!sessionExists) return NotFound();

        var query = _context.InventoryTasks
            .Where(t => t.SessionId == sessionId)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(t => t.InventoryRecords)
            .Include(t => t.Department)
            .AsNoTracking()
            .AsQueryable();

        if (catalogAssetId.HasValue)
            query = query.Where(t => t.AssetInstance.AssetId == catalogAssetId.Value);

        var tasks = await query.ToListAsync();

        tasks = tasks
            .Where(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status))
            .ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            tasks = tasks
                .Where(t =>
                {
                    var a = t.AssetInstance.Asset;
                    return a.Code.ToLower().Contains(kw) ||
                           a.Name.ToLower().Contains(kw) ||
                           t.AssetInstance.InstanceCode.ToLower().Contains(kw);
                })
                .ToList();
        }

        var result = tasks.Select(t =>
        {
            var inst = t.AssetInstance;
            var asset = inst.Asset;
            var record = t.InventoryRecords.FirstOrDefault();
            int cs = t.Status == (int)InventoryTaskStatus.Checked ? 2 : 0;
            var currentLoc = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent);

            return new SessionAssetCheckItemDTO
            {
                AssetId = asset.AssetId,
                AssetInstanceId = inst.AssetInstanceId,
                AssetCode = asset.Code,
                InstanceCode = inst.InstanceCode,
                AssetName = asset.Name,
                DepartmentName = currentLoc?.Department?.Name ?? t.Department?.Name ?? string.Empty,
                BookStatus = inst.Status,
                ActualStatus = ResolveRecordedActualStatus(record, inst.Status),
                CheckStatus = cs
            };
        }).ToList();

        if (checkStatus.HasValue)
            result = result.Where(r => r.CheckStatus == checkStatus.Value).ToList();

        return Ok(result);
    }

    /// <summary>
    /// GET /api/inventory/sessions/{sessionId}/instances/{assetInstanceId} — Inventory form for one <b>instance</b> (task).
    /// </summary>
    [HttpGet("sessions/{sessionId:int}/instances/{assetInstanceId:int}")]
    [HttpGet("sessions/{sessionId:int}/assets/{assetInstanceId:int}")]
    public async Task<ActionResult<AssetInventoryDetailDTO>> GetAssetInventoryDetail(
        int sessionId, int assetInstanceId)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(sessionId);
        if (gate != null) return gate;

        var task = await _context.InventoryTasks
            .Where(t => t.SessionId == sessionId && t.AssetInstanceId == assetInstanceId)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
                    .ThenInclude(a => a.AssetType)
                        .ThenInclude(at => at.Category)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(t => t.InventoryRecords)
                .ThenInclude(r => r.ActualLocation)
                    .ThenInclude(al => al.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (task == null) return NotFound();

        var inst = task.AssetInstance;
        var asset = inst.Asset;
        if (IsExcludedFromInventoryExecution(inst.Status))
            return NotFound(new { message = "Thể hiện tài sản này không thuộc phạm vi kiểm kê." });

        var currentLoc = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent);

        var record = task.InventoryRecords.FirstOrDefault();
        int? actualLocationId = record?.ActualLocation?.DepartmentId;
        var actualStatusResolved = ResolveRecordedActualStatus(record, inst.Status);

        var departments = await _context.Departments
            .AsNoTracking()
            .Select(d => new DropdownItemDTO { Id = d.DepartmentId, Name = d.Name })
            .ToListAsync();

        var dto = new AssetInventoryDetailDTO
        {
            AssetId = asset.AssetId,
            AssetInstanceId = inst.AssetInstanceId,
            AssetCode = asset.Code,
            InstanceCode = inst.InstanceCode,
            AssetName = asset.Name,
            CategoryName = asset.AssetType?.Category?.Name ?? string.Empty,
            TypeName = asset.AssetType?.Name ?? string.Empty,
            BookStatus = inst.Status,
            BookAssetStatus = ((AssetStatus)inst.Status).ToString(),
            ActualStatus = actualStatusResolved,
            ActualCondition = record?.ActualCondition ?? string.Empty,
            BookLocationId = currentLoc?.DepartmentId,
            BookLocationName = currentLoc?.Department?.Name ?? string.Empty,
            ActualLocationId = actualLocationId,
            Locations = departments
        };

        return Ok(dto);
    }

    /// <summary>
    /// PUT /api/inventory/sessions/{sessionId}/instances/{assetInstanceId} — Save result for one <b>instance</b> task.
    /// </summary>
    [HttpPut("sessions/{sessionId:int}/instances/{assetInstanceId:int}")]
    [HttpPut("sessions/{sessionId:int}/assets/{assetInstanceId:int}")]
    public async Task<ActionResult> SaveAssetInventory(
        int sessionId, int assetInstanceId, [FromBody] SaveAssetInventoryDTO dto)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(sessionId);
        if (gate != null) return gate;

        if (dto.AssetInstanceId > 0 && dto.AssetInstanceId != assetInstanceId)
            return BadRequest(new { message = "AssetInstanceId trong body không khớp với đường dẫn." });

        var session = await _context.InventorySessions.FindAsync(sessionId);
        if (session == null) return NotFound(new { message = "Phiên kiểm kê không tồn tại." });

        if (session.Status != (int)InventorySessionStatus.InProgress)
            return BadRequest(new { message = "Chỉ có thể lưu kết quả khi phiên đang thực hiện." });

        var task = await _context.InventoryTasks
            .Where(t => t.SessionId == sessionId && t.AssetInstanceId == assetInstanceId)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(t => t.InventoryRecords)
            .Include(t => t.InventoryDiscrepancies)
            .FirstOrDefaultAsync();

        if (task == null) return NotFound(new { message = "Nhiệm vụ kiểm kê không tồn tại." });

        var inst = task.AssetInstance;
        var asset = inst.Asset;
        var bookLocation = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent);
        if (IsExcludedFromInventoryExecution(inst.Status))
            return BadRequest(new { message = "Thể hiện tài sản này không thuộc phạm vi kiểm kê (đã thanh lý / mất / hỏng…)." });

        if (!Enum.IsDefined(typeof(AssetStatus), dto.ActualStatus))
            return BadRequest(new { message = "Trạng thái tài sản không hợp lệ." });

        var reported = (AssetStatus)dto.ActualStatus;
        var storedCondition = reported.ToString();
        bool actualInUseBucket = BookImpliesInUse(dto.ActualStatus);
        int bookStatusInt = inst.Status;
        bool bookInUseBucket = BookImpliesInUse(bookStatusInt);

        // Resolve actual location: dto.ActualLocationId is a DepartmentId
        AssetLocation? actualLocation = null;
        if (dto.ActualLocationId.HasValue)
        {
            actualLocation = inst.AssetLocations
                .FirstOrDefault(al => al.DepartmentId == dto.ActualLocationId.Value);

            if (actualLocation == null)
            {
                actualLocation = await _context.AssetLocations
                    .FirstOrDefaultAsync(al =>
                        al.AssetInstanceId == assetInstanceId && al.DepartmentId == dto.ActualLocationId.Value);
            }

            if (actualLocation == null)
            {
                actualLocation = new AssetLocation
                {
                    AssetInstanceId = assetInstanceId,
                    DepartmentId = dto.ActualLocationId.Value,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    IsCurrent = false,
                    Note = "Ghi nhận từ kiểm kê tài sản"
                };
                _context.AssetLocations.Add(actualLocation);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            actualLocation = bookLocation;
        }

        if (actualLocation == null)
            return BadRequest(new { message = "Tài sản không có thông tin vị trí trong hệ thống." });

        // Create or update inventory record
        var record = task.InventoryRecords.FirstOrDefault();
        int? actualQtyLegacy = actualInUseBucket ? 1 : 0;

        if (record == null)
        {
            record = new InventoryRecord
            {
                TaskId = task.TaskId,
                ActualLocationId = actualLocation.LocationId,
                ActualUserId = null,
                ActualCondition = storedCondition,
                IsFound = actualInUseBucket,
                ActualQuantity = actualQtyLegacy,
                CheckedBy = dto.CheckedBy > 0 ? dto.CheckedBy : 1,
                CheckedDate = DateTime.UtcNow,
                DateCheckCompleted = DateTime.UtcNow
            };
            _context.InventoryRecords.Add(record);
        }
        else
        {
            record.ActualLocationId = actualLocation.LocationId;
            record.ActualUserId = null;
            record.ActualCondition = storedCondition;
            record.IsFound = actualInUseBucket;
            record.ActualQuantity = actualQtyLegacy;
            record.CheckedDate = DateTime.UtcNow;
            record.DateCheckCompleted = DateTime.UtcNow;
        }

        // Detect discrepancies (per instance; no quantity comparison)
        var bookUsage = await _context.AssetUsages
            .Where(u => u.AssetInstanceId == assetInstanceId && u.IsCurrent)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync();
        var bookManagerId = bookUsage?.Employee?.UserId;

        int discrepancyFlags = 0;
        if (dto.ActualStatus != bookStatusInt)
            discrepancyFlags |= (int)DiscrepancyType.ConditionMismatch;
        if (!actualInUseBucket && bookInUseBucket)
            discrepancyFlags |= (int)DiscrepancyType.AssetNotFound;

        if (BookImpliesInUse(dto.ActualStatus))
        {
            if (bookLocation != null && dto.ActualLocationId.HasValue &&
                bookLocation.DepartmentId != dto.ActualLocationId.Value)
                discrepancyFlags |= (int)DiscrepancyType.LocationMismatch;
        }

        // Replace existing discrepancies for this task
        _context.InventoryDiscrepancies.RemoveRange(task.InventoryDiscrepancies);

        if (discrepancyFlags != 0)
        {
            _context.InventoryDiscrepancies.Add(new InventoryDiscrepancy
            {
                TaskId = task.TaskId,
                DiscrepancyType = discrepancyFlags,
                BookValue = inst.CurrentValue,
                BookLocationId = bookLocation?.LocationId ?? actualLocation.LocationId,
                BookUserId = bookManagerId,
                BookCondition = ((AssetStatus)inst.Status).ToString(),
                ActualValue = inst.CurrentValue,
                ActualLocationId = actualLocation.LocationId,
                ActualUserId = null,
                ActualCondition = storedCondition
            });
        }

        task.Status = (int)InventoryTaskStatus.Checked;
        await _context.SaveChangesAsync();

        await UpdateSessionProgressPercentForEligibleTasksAsync(session);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã lưu thông tin kiểm kê." });
    }

    /// <summary>
    /// POST /api/inventory/sessions - Create a new inventory session and auto-generate tasks for matching assets
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult> CreateSession([FromBody] CreateInventorySessionDTO dto)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        var access = await GetInventoryAccessAsync();
        if (access.RestrictToDepartment)
        {
            if (!access.DepartmentId.HasValue)
                return BadRequest(new { message = "Không xác định được phòng ban của bạn." });
            dto.DepartmentId = access.DepartmentId.Value;
        }

        dto.CreatedBy = currentUserId;

        var department = await _context.Departments.FindAsync(dto.DepartmentId);
        if (department == null)
            return BadRequest(new { message = "Phòng ban không tồn tại." });

        // One inventory task per physical asset instance in the department.
        var instances = await _context.AssetInstances
            .Where(ai =>
                ai.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == dto.DepartmentId) &&
                ai.Status != (int)AssetStatus.Disposed &&
                ai.Status != (int)AssetStatus.Lost &&
                ai.Status != (int)AssetStatus.Liquidated &&
                ai.Status != (int)AssetStatus.Damaged)
            .AsNoTracking()
            .ToListAsync();

        if (!instances.Any())
            return BadRequest(new { message = "Không có tài sản hợp lệ nào trong phòng ban này." });

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var code = await GenerateSessionCode();
            var session = new InventorySession
            {
                Code = code,
                Purpose = dto.Purpose ?? string.Empty,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DepartmentId = dto.DepartmentId,
                AssetCategoryId = null,
                AssetTypeId = null,
                Status = (int)InventorySessionStatus.Scheduled,
                ProgressPercent = 0,
                CreatedBy = dto.CreatedBy,
                CreateDate = DateTime.UtcNow,
                IsPeriodic = dto.IsPeriodic,
                PeriodDays = dto.IsPeriodic ? dto.PeriodDays : null
            };

            foreach (var inst in instances)
            {
                session.InventoryTasks.Add(new InventoryTask
                {
                    AssetInstanceId = inst.AssetInstanceId,
                    AssignedUserId = dto.CreatedBy,
                    DepartmentId = dto.DepartmentId,
                    Status = (int)InventoryTaskStatus.Pending,
                    CheckDate = dto.EndDate
                });
            }

            _context.InventorySessions.Add(session);
            await _context.SaveChangesAsync();
            // Thông báo "đến lịch" cho trưởng phòng do Quartz (InventoryScheduledCheckNotificationJob) khi tới khung ngày.
            await transaction.CommitAsync();

            return Ok(new { message = "Đã lên lịch kiểm kê thành công.", sessionIds = new[] { session.SessionId }, count = 1 });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/tasks/{taskId}/record - Submit inventory result for a single asset task.
    /// Detects discrepancies between book records and actual findings.
    /// </summary>
    [HttpPost("sessions/{id:int}/tasks/{taskId:int}/record")]
    public async Task<ActionResult> SubmitTaskRecord(
        int id, int taskId, [FromBody] SubmitInventoryTaskDTO dto)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions.FindAsync(id);
        if (session == null)
            return NotFound(new { message = "Phiên kiểm kê không tồn tại." });

        if (session.Status != (int)InventorySessionStatus.InProgress)
            return BadRequest(new { message = "Chỉ có thể ghi nhận kết quả khi phiên kiểm kê đang thực hiện." });

        var task = await _context.InventoryTasks
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(t => t.InventoryRecords)
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.SessionId == id);

        if (task == null)
            return NotFound(new { message = "Nhiệm vụ kiểm kê không tồn tại." });

        if (task.Status == (int)InventoryTaskStatus.Checked)
            return BadRequest(new { message = "Nhiệm vụ này đã được kiểm kê rồi." });

        var inst = task.AssetInstance;
        var bookLocation = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent);
        if (bookLocation == null)
            return BadRequest(new { message = "Tài sản không có thông tin vị trí trong hệ thống." });

        // Determine actual location record (find or create an AssetLocation entry)
        AssetLocation actualLocation;
        if (!dto.IsFound || bookLocation.DepartmentId == dto.ActualDepartmentId)
        {
            // Not found or found at the same location as on book → reuse book location
            actualLocation = bookLocation;
        }
        else
        {
            // Found at a different location → find or create a non-current AssetLocation entry
            actualLocation = await _context.AssetLocations
                .FirstOrDefaultAsync(al =>
                    al.AssetInstanceId == task.AssetInstanceId &&
                    al.DepartmentId == dto.ActualDepartmentId &&
                    !al.IsCurrent)
                ?? new AssetLocation();

            if (actualLocation.LocationId == 0)
            {
                actualLocation = new AssetLocation
                {
                    AssetInstanceId = task.AssetInstanceId,
                    DepartmentId = dto.ActualDepartmentId,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    IsCurrent = false,
                    Note = "Ghi nhận từ kiểm kê tài sản"
                };
                _context.AssetLocations.Add(actualLocation);
                await _context.SaveChangesAsync();
            }
        }

        // Create inventory record
        var record = new InventoryRecord
        {
            TaskId = taskId,
            ActualLocationId = actualLocation.LocationId,
            ActualUserId = dto.ActualUserId,
            ActualCondition = dto.ActualCondition,
            IsFound = dto.IsFound,
            CheckedBy = dto.CheckedBy,
            CheckedDate = DateTime.UtcNow,
            DateCheckCompleted = DateTime.UtcNow
        };
        _context.InventoryRecords.Add(record);

        // Detect discrepancies: compare book vs actual
        var bookUsage = await _context.AssetUsages
            .Where(u => u.AssetInstanceId == task.AssetInstanceId && u.IsCurrent)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync();
        var bookUserId = bookUsage?.Employee?.UserId;
        var bookCondition = ((AssetStatus)inst.Status).ToString();
        var bookValue = inst.CurrentValue;

        int discrepancyFlags = 0;

        if (!dto.IsFound)
        {
            discrepancyFlags |= (int)DiscrepancyType.AssetNotFound;
        }
        else
        {
            if (bookLocation.DepartmentId != dto.ActualDepartmentId)
                discrepancyFlags |= (int)DiscrepancyType.LocationMismatch;

            if (bookUserId != dto.ActualUserId && (bookUserId.HasValue || dto.ActualUserId.HasValue))
                discrepancyFlags |= (int)DiscrepancyType.UserMismatch;

            if (dto.ActualValue.HasValue && Math.Abs(bookValue - dto.ActualValue.Value) > 0.01m)
                discrepancyFlags |= (int)DiscrepancyType.ValueMismatch;

            if (!string.Equals(bookCondition, dto.ActualCondition, StringComparison.OrdinalIgnoreCase))
                discrepancyFlags |= (int)DiscrepancyType.ConditionMismatch;
        }

        if (discrepancyFlags != 0)
        {
            _context.InventoryDiscrepancies.Add(new InventoryDiscrepancy
            {
                TaskId = taskId,
                DiscrepancyType = discrepancyFlags,
                BookValue = bookValue,
                BookLocationId = bookLocation.LocationId,
                BookUserId = bookUserId,
                BookCondition = bookCondition,
                ActualValue = dto.ActualValue ?? bookValue,
                ActualLocationId = actualLocation.LocationId,
                ActualUserId = dto.ActualUserId,
                ActualCondition = dto.ActualCondition
            });
        }

        // Mark task as checked
        task.Status = (int)InventoryTaskStatus.Checked;
        if (dto.Note != null) task.Note = dto.Note;

        await _context.SaveChangesAsync();

        await UpdateSessionProgressPercentForEligibleTasksAsync(session);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Đã ghi nhận kết quả kiểm kê.",
            discrepancyDetected = discrepancyFlags != 0,
            discrepancyType = discrepancyFlags,
            discrepancyTypeName = BuildDiscrepancyTypeName(discrepancyFlags),
            progressPercent = session.ProgressPercent
        });
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/complete - Mark an inventory session as completed
    /// </summary>
    [HttpPost("sessions/{id:int}/complete")]
    public async Task<ActionResult> CompleteSession(int id)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        if (session.Status == (int)InventorySessionStatus.Completed)
            return BadRequest(new { message = "Phiên kiểm kê này đã hoàn thành." });

        if (session.Status != (int)InventorySessionStatus.InProgress)
            return BadRequest(new { message = "Chỉ có thể hoàn thành kiểm kê khi phiên đang ở trạng thái Đang thực hiện." });

        var eligibleTasks = session.InventoryTasks
            .Where(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status))
            .ToList();
        var totalTasks = eligibleTasks.Count;
        var checkedTasks = eligibleTasks.Count(t => t.Status == (int)InventoryTaskStatus.Checked);

        if (totalTasks == 0 || checkedTasks < totalTasks)
            return BadRequest(new { message = "Cần hoàn tất kiểm kê 100% tài sản trước khi kết thúc phiên." });

        // Trưởng phòng xử lý chênh lệch trước; chỉ sau đó hệ thống mới báo Giám đốc.
        session.Status = (int)InventorySessionStatus.PendingAccountant;
        session.ProgressPercent = totalTasks > 0
            ? (int)Math.Round((double)checkedTasks / totalTasks * 100)
            : 0;

        var taskIds = session.InventoryTasks.Select(t => t.TaskId).ToList();
        var discrepancies = await _context.InventoryDiscrepancies
            .Where(d => taskIds.Contains(d.TaskId))
            .AsNoTracking()
            .ToListAsync();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Phiên kiểm kê đã được hoàn thành.",
            progressPercent = session.ProgressPercent,
            checkedTasks,
            totalTasks,
            quantityDiffCount = discrepancies.Count(d =>
                (d.DiscrepancyType & (int)DiscrepancyType.AssetNotFound) != 0 ||
                (d.DiscrepancyType & (int)DiscrepancyType.QuantityMismatch) != 0),
            locationChangeCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.LocationMismatch) != 0),
            departmentChangeCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.LocationMismatch) != 0),
            conditionChangeCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.ConditionMismatch) != 0)
        });
    }

    /// <summary>
    /// GET /api/inventory/sessions/{id}/review-summary — Báo cáo chênh lệch (trưởng phòng / giám đốc)
    /// </summary>
    [HttpGet("sessions/{id:int}/review-summary")]
    public async Task<ActionResult<InventoryReviewSummaryDTO>> GetReviewSummary(int id)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions
            .Include(s => s.Department)
            .Include(s => s.AssetCategory)
            .Include(s => s.AssetType)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        var discrepancies = await _context.InventoryDiscrepancies
            .Where(d => d.Task.SessionId == id)
            .Include(d => d.Task).ThenInclude(t => t.AssetInstance).ThenInclude(ai => ai.Asset)
            .Include(d => d.Task).ThenInclude(t => t.InventoryRecords)
            .Include(d => d.BookLocation).ThenInclude(bl => bl.Department)
            .Include(d => d.ActualLocation).ThenInclude(al => al.Department)
            .Include(d => d.BookUser)
            .Include(d => d.ActualUser)
            .AsNoTracking()
            .ToListAsync();

        var userNameMap = await GetUserDisplayNamesAsync(
            discrepancies.SelectMany(d => new[] { d.BookUserId, d.ActualUserId }));

        const int bookQtyPerInstance = 1;
        var detailList = discrepancies.Select(d =>
        {
            var record = d.Task.InventoryRecords
                .OrderByDescending(r => r.RecordId)
                .FirstOrDefault();
            var inst = d.Task.AssetInstance;
            var asset = inst.Asset;
            return new InventoryDiscrepancyDetailDTO
            {
                DiscrepancyId = d.DiscrepancyId,
                TaskId = d.Task.TaskId,
                AssetId = asset.AssetId,
                AssetInstanceId = inst.AssetInstanceId,
                AssetCode = asset.Code,
                InstanceCode = inst.InstanceCode,
                AssetName = asset.Name,
                DiscrepancyType = d.DiscrepancyType,
                DiscrepancyTypeName = BuildDiscrepancyTypeName(d.DiscrepancyType),
                BookValue = d.BookValue,
                BookQuantity = bookQtyPerInstance,
                ActualQuantity = record?.ActualQuantity,
                TaskNote = d.Task.Note,
                BookDepartmentName = d.BookLocation?.Department?.Name,
                BookUserId = d.BookUserId,
                BookUserName = ResolveDisplayName(userNameMap, d.BookUserId),
                BookCondition = d.BookCondition,
                ActualValue = d.ActualValue,
                ActualDepartmentName = d.ActualLocation?.Department?.Name,
                ActualUserId = d.ActualUserId,
                ActualUserName = ResolveDisplayName(userNameMap, d.ActualUserId),
                ActualCondition = record?.ActualCondition ?? d.ActualCondition,
                ResolvedAt = d.ResolvedAt
            };
        }).ToList();

        var now = DateTime.UtcNow;
        var displayStatus = GetDisplayStatus(session, now);

        var dto = new InventoryReviewSummaryDTO
        {
            SessionId = session.SessionId,
            Code = session.Code,
            Purpose = session.Purpose,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            DepartmentName = session.Department?.Name ?? string.Empty,
            AssetCategoryName = session.AssetCategory?.Name,
            AssetTypeName = session.AssetType?.Name,
            Status = displayStatus,
            StatusName = GetSessionStatusName(displayStatus),
            TotalTasks = session.InventoryTasks.Count(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status)),
            CompletedTasks = session.InventoryTasks.Count(t =>
                !IsExcludedFromInventoryExecution(t.AssetInstance.Status) &&
                t.Status == (int)InventoryTaskStatus.Checked),
            ProgressPercent = session.ProgressPercent,
            TotalDiscrepancies = detailList.Count,
            AssetNotFoundCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.AssetNotFound) != 0),
            QuantityMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.QuantityMismatch) != 0),
            LocationMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.LocationMismatch) != 0),
            UserMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.UserMismatch) != 0),
            ValueMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.ValueMismatch) != 0),
            ConditionMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.ConditionMismatch) != 0),
            Discrepancies = detailList
        };

        return Ok(dto);
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/director-approve — Chờ xác nhận → Chờ xử lý (trưởng phòng xử lý sổ) hoặc Đã xử lý
    /// </summary>
    [HttpPost("sessions/{id:int}/director-approve")]
    public async Task<ActionResult> DirectorApproveSession(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryRecords)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryDiscrepancies)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.Completed)
            return BadRequest(new { message = "Chỉ có thể xác nhận khi phiên đang ở trạng thái Chờ xác nhận." });

        const int bookQtyPerInstance = 1;
        var hasMismatch = false;
        foreach (var task in session.InventoryTasks.Where(t =>
                     !IsExcludedFromInventoryExecution(t.AssetInstance.Status)))
        {
            var record = task.InventoryRecords.FirstOrDefault();
            if (record == null)
            {
                hasMismatch = true;
                break;
            }

            if (record.IsFound == false)
            {
                hasMismatch = true;
                break;
            }

            var actualQty = record.ActualQuantity ?? (record.IsFound == true ? bookQtyPerInstance : 0);
            if (actualQty != bookQtyPerInstance)
            {
                hasMismatch = true;
                break;
            }
        }

        // Any unresolved discrepancy row (tình trạng, vị trí, giá trị, không tìm thấy, …) → Chờ xử lý (kế toán).
        if (!hasMismatch)
        {
            hasMismatch = session.InventoryTasks
                .Where(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status))
                .Any(t => t.InventoryDiscrepancies.Any(d => d.ResolvedAt == null));
        }

        session.Status = hasMismatch
            ? (int)InventorySessionStatus.PendingAccountant
            : (int)InventorySessionStatus.Confirmed;

        await _context.SaveChangesAsync();

        await SafeInventoryNotifyAsync(
            () => _inventoryNotifications.NotifyAfterDirectorApprovalAsync(session, hasMismatch),
            "director-approve → heads/accountants");

        var displayStatus = GetDisplayStatus(session, DateTime.UtcNow);

        return Ok(new
        {
            message = hasMismatch
                ? "Đã xác nhận. Có chênh lệch so với sổ — phiên chuyển sang Chờ xử lý (trưởng phòng xử lý trên sổ)."
                : "Đã xác nhận. Không có chênh lệch so với sổ — phiên đã xử lý.",
            newStatus = displayStatus,
            statusName = GetSessionStatusName(displayStatus),
            hasQuantityOrUserDiscrepancy = hasMismatch
        });
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/reject — Yêu cầu kiểm kê lại: Chờ xác nhận → Đang thực hiện (reset nhiệm vụ để kiểm lại)
    /// </summary>
    [HttpPost("sessions/{id:int}/reject")]
    public async Task<ActionResult> RequestInventoryRecheck(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryRecords)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryDiscrepancies)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.Completed)
            return BadRequest(new { message = "Chỉ có thể yêu cầu kiểm kê lại khi phiên đang chờ xác nhận." });

        foreach (var task in session.InventoryTasks)
        {
            _context.InventoryDiscrepancies.RemoveRange(task.InventoryDiscrepancies);
            _context.InventoryRecords.RemoveRange(task.InventoryRecords);
            task.Status = (int)InventoryTaskStatus.Pending;
            task.Note = null;
        }

        session.Status = (int)InventorySessionStatus.InProgress;
        session.ProgressPercent = 0;

        await _context.SaveChangesAsync();

        await SafeInventoryNotifyAsync(
            () => _inventoryNotifications.NotifyDepartmentHeadsRecheckRequestedAsync(session),
            "reject-recheck → heads");

        return Ok(new { message = "Đã gửi yêu cầu kiểm kê lại. Phiên chuyển sang Đang thực hiện.", sessionId = id });
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/confirm — Trưởng phòng: Chờ xử lý → Đã xử lý (không bắt buộc giám đốc xác nhận).
    /// </summary>
    [HttpPost("sessions/{id:int}/confirm")]
    public async Task<ActionResult> DepartmentHeadFinishInventoryResolution(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        var actorGate = await EnsureDepartmentHeadOrAdminForSessionAsync(id);
        if (actorGate != null) return actorGate;

        var session = await _context.InventorySessions.FindAsync(id);
        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.PendingAccountant)
            return BadRequest(new { message = "Chỉ có thể hoàn tất khi phiên đang ở trạng thái Chờ xử lý." });

        var unresolved = await _context.InventoryDiscrepancies
            .CountAsync(d => d.Task.SessionId == id && d.ResolvedAt == null);
        if (unresolved > 0)
            return BadRequest(new { message = "Còn chênh lệch chưa cập nhật lên sổ. Vui lòng xử lý hết trước khi hoàn tất." });

        if (dto.ApplyCorrections)
        {
            // Áp dụng chỉnh sửa sổ từ chênh lệch — có thể mở rộng sau.
        }

        session.Status = (int)InventorySessionStatus.Confirmed;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Đã hoàn tất. Phiên kiểm kê được đánh dấu Đã xử lý.",
            sessionId = id
        });
    }

    /// <summary>
    /// POST /api/inventory/sessions/{sessionId}/discrepancies/{discrepancyId}/apply-actual —
    /// Trưởng phòng (phòng ban phiên) hoặc Admin: cập nhật sổ theo kết quả thực tế đã ghi nhận; đánh dấu dòng chênh lệch đã xử lý.
    /// Chỉ khi phiên ở trạng thái Chờ xử lý.
    /// </summary>
    [HttpPost("sessions/{sessionId:int}/discrepancies/{discrepancyId:int}/apply-actual")]
    public async Task<ActionResult> AccountantApplyDiscrepancyActual(int sessionId, int discrepancyId)
    {
        var actorGate = await EnsureDepartmentHeadOrAdminForSessionAsync(sessionId);
        if (actorGate != null) return actorGate;

        var discrepancy = await _context.InventoryDiscrepancies
            .Include(d => d.Task)
                .ThenInclude(t => t.Session)
            .Include(d => d.Task)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.AssetLocations)
            .Include(d => d.Task)
                .ThenInclude(t => t.InventoryRecords)
            .FirstOrDefaultAsync(d => d.DiscrepancyId == discrepancyId && d.Task.SessionId == sessionId);

        if (discrepancy == null)
            return NotFound(new { message = "Không tìm thấy chênh lệch trong phiên này." });

        var session = discrepancy.Task.Session;
        if (session.Status != (int)InventorySessionStatus.PendingAccountant)
            return BadRequest(new { message = "Chỉ có thể cập nhật sổ khi phiên đang ở trạng thái Chờ xử lý." });

        if (discrepancy.ResolvedAt.HasValue)
            return BadRequest(new { message = "Chênh lệch này đã được cập nhật lên sổ trước đó." });

        var record = discrepancy.Task.InventoryRecords
            .OrderByDescending(r => r.RecordId)
            .FirstOrDefault();
        if (record == null)
            return BadRequest(new { message = "Không có bản ghi kiểm kê cho nhiệm vụ này." });

        var inst = discrepancy.Task.AssetInstance;
        var effective = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!Enum.TryParse<AssetStatus>(record.ActualCondition, true, out var newStatus))
        {
            if (record.IsFound == false)
                newStatus = AssetStatus.Lost;
            else
                return BadRequest(new { message = "Không đọc được tình trạng thực tế (ActualCondition) từ bản ghi kiểm kê." });
        }

        inst.Status = (int)newStatus;

        if ((discrepancy.DiscrepancyType & (int)DiscrepancyType.ValueMismatch) != 0)
            inst.CurrentValue = discrepancy.ActualValue;

        var targetLocation = await _context.AssetLocations
            .FirstOrDefaultAsync(al => al.LocationId == record.ActualLocationId);
        if (targetLocation == null || targetLocation.AssetInstanceId != inst.AssetInstanceId)
            return BadRequest(new { message = "Vị trí thực tế không hợp lệ cho thể hiện tài sản này." });

        await CloseCurrentAssetLocationsExceptAsync(inst.AssetInstanceId, targetLocation.LocationId, effective);
        targetLocation.IsCurrent = true;
        targetLocation.EndDate = null;
        if (targetLocation.StartDate > effective)
            targetLocation.StartDate = effective;

        await CloseCurrentAssetUsagesAsync(inst.AssetInstanceId, effective);
        if (record.ActualUserId.HasValue)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == record.ActualUserId.Value);
            if (employee == null)
                return BadRequest(new { message = "Không tìm thấy nhân viên gắn với người dùng được ghi nhận khi kiểm kê." });

            if (employee.DepartmentId != targetLocation.DepartmentId)
                return BadRequest(new { message = "Phòng ban của nhân viên phụ trách phải trùng với phòng ban vị trí thực tế." });

            _context.AssetUsages.Add(new AssetUsage
            {
                AssetInstanceId = inst.AssetInstanceId,
                EmployeeId = employee.EmployeeId,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true,
                Note = "Cập nhật từ xử lý chênh lệch kiểm kê"
            });
        }

        discrepancy.ResolvedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã cập nhật sổ sách theo thông tin thực tế kiểm kê." });
    }

    /// <summary>
    /// PUT /api/inventory/sessions/{id} - Update purpose and dates of a scheduled session
    /// </summary>
    [HttpPut("sessions/{id:int}")]
    public async Task<ActionResult> UpdateSession(int id, [FromBody] UpdateInventorySessionDTO dto)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions.FindAsync(id);
        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.Scheduled)
            return BadRequest(new { message = "Chỉ có thể chỉnh sửa phiên kiểm kê ở trạng thái 'Đã lên lịch'." });

        if (dto.EndDate <= dto.StartDate)
            return BadRequest(new { message = "Ngày kết thúc phải sau ngày bắt đầu." });

        session.Purpose = dto.Purpose ?? string.Empty;
        session.StartDate = dto.StartDate;
        session.EndDate = dto.EndDate;

        if (session.IsPeriodic && dto.PeriodDays.HasValue && dto.PeriodDays.Value > 0)
            session.PeriodDays = dto.PeriodDays.Value;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã cập nhật thông tin phiên kiểm kê." });
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/activate
    /// </summary>
    [HttpPost("sessions/{id:int}/activate")]
    public async Task<ActionResult> ActivateSession(int id)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions.FindAsync(id);
        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.Scheduled)
            return BadRequest(new { message = "Chỉ có thể kích hoạt phiên kiểm kê ở trạng thái Đã lên lịch." });

        session.Status = (int)InventorySessionStatus.InProgress;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Phiên kiểm kê đã được kích hoạt (Đang thực hiện)." });
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/cancel
    /// Hủy phiên ở trạng thái Đã lên lịch hoặc Đang thực hiện → Đã hủy.
    /// </summary>
    [HttpPost("sessions/{id:int}/cancel")]
    public async Task<ActionResult> CancelSession(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.Scheduled &&
            session.Status != (int)InventorySessionStatus.InProgress)
            return BadRequest(new { message = "Chỉ có thể hủy phiên ở trạng thái Đã lên lịch hoặc Đang thực hiện." });

        var wasScheduled = session.Status == (int)InventorySessionStatus.Scheduled;

        session.Status = (int)InventorySessionStatus.Cancelled;

        // Notification.RefId is an FK to User in the schema, not a generic ref — do not store session id there.
        int? notifyUserId = dto.ReviewedBy > 0 ? dto.ReviewedBy : null;
        if (notifyUserId == null && TryGetCurrentUserId(out var curId))
            notifyUserId = curId;
        if (notifyUserId is > 0)
        {
            _context.Notifications.Add(new Notification
            {
                Title = wasScheduled
                    ? $"Lịch kiểm kê bị hủy: {session.Code}"
                    : $"Phiên kiểm kê bị hủy: {session.Code}",
                Content = TruncateNotificationContent($"Phiên {session.Code} đã bị hủy. Lý do: {dto.ReviewNotes ?? "Không có ghi chú."}"),
                RefId = null,
                UserId = notifyUserId.Value,
                SentDate = DateTime.UtcNow,
                IsSend = true
            });
        }

        // Đã lên lịch + định kỳ: dừng luôn các phiên định kỳ đã lên lịch sau này.
        int cancelledChainCount = 0;
        if (session.IsPeriodic && wasScheduled)
        {
            var futurePeriodicSessions = await _context.InventorySessions
                .Where(s =>
                    s.SessionId != id &&
                    s.DepartmentId == session.DepartmentId &&
                    s.IsPeriodic &&
                    s.Status == (int)InventorySessionStatus.Scheduled)
                .ToListAsync();

            foreach (var future in futurePeriodicSessions)
            {
                future.Status = (int)InventorySessionStatus.Cancelled;
            }

            cancelledChainCount = futurePeriodicSessions.Count;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = cancelledChainCount > 0
                ? $"Phiên kiểm kê đã được hủy. Đã dừng {cancelledChainCount} lịch định kỳ tiếp theo."
                : "Phiên kiểm kê đã được hủy.",
            sessionId = id,
            reviewNotes = dto.ReviewNotes,
            cancelledChainCount
        });
    }

    /// <summary>
    /// GET /api/inventory/sessions/{id}/discrepancies - Get all discrepancies for a session
    /// </summary>
    [HttpGet("sessions/{id:int}/discrepancies")]
    public async Task<ActionResult<IEnumerable<InventoryDiscrepancyDTO>>> GetDiscrepancies(int id)
    {
        var gate = await EnsureInventorySessionDepartmentAccessAsync(id);
        if (gate != null) return gate;

        var sessionExists = await _context.InventorySessions.AnyAsync(s => s.SessionId == id);
        if (!sessionExists) return NotFound();

        var discrepancies = await _context.InventoryDiscrepancies
            .Where(d => d.Task.SessionId == id)
            .Include(d => d.Task).ThenInclude(t => t.AssetInstance).ThenInclude(ai => ai.Asset)
            .Include(d => d.Task).ThenInclude(t => t.InventoryRecords)
            .Include(d => d.BookLocation).ThenInclude(bl => bl.Department)
            .Include(d => d.ActualLocation).ThenInclude(al => al.Department)
            .Include(d => d.BookUser)
            .Include(d => d.ActualUser)
            .AsNoTracking()
            .ToListAsync();

        var userNameMap = await GetUserDisplayNamesAsync(
            discrepancies.SelectMany(d => new[] { d.BookUserId, d.ActualUserId }));

        const int bookQtyPerInstance = 1;
        var result = discrepancies.Select(d =>
        {
            var record = d.Task.InventoryRecords
                .OrderByDescending(r => r.RecordId)
                .FirstOrDefault();
            return new InventoryDiscrepancyDTO
            {
                DiscrepancyId = d.DiscrepancyId,
                DiscrepancyType = d.DiscrepancyType,
                DiscrepancyTypeName = BuildDiscrepancyTypeName(d.DiscrepancyType),
                BookValue = d.BookValue,
                BookQuantity = bookQtyPerInstance,
                ActualQuantity = record?.ActualQuantity,
                TaskNote = d.Task.Note,
                BookDepartmentName = d.BookLocation?.Department?.Name,
                BookUserId = d.BookUserId,
                BookUserName = ResolveDisplayName(userNameMap, d.BookUserId),
                BookCondition = d.BookCondition,
                ActualValue = d.ActualValue,
                ActualDepartmentName = d.ActualLocation?.Department?.Name,
                ActualUserId = d.ActualUserId,
                ActualUserName = ResolveDisplayName(userNameMap, d.ActualUserId),
                ActualCondition = record?.ActualCondition ?? d.ActualCondition,
                ResolvedAt = d.ResolvedAt
            };
        }).ToList();

        return Ok(result);
    }

    // ── Metadata endpoints for dropdowns ──────────────────────────────────────

    [HttpGet("meta/departments")]
    public async Task<ActionResult<IEnumerable<DropdownItemDTO>>> GetDepartments()
    {
        var access = await GetInventoryAccessAsync();
        if (access.RestrictToDepartment)
        {
            if (!access.DepartmentId.HasValue)
                return Ok(Array.Empty<DropdownItemDTO>());
            var one = await _context.Departments
                .AsNoTracking()
                .Where(d => d.DepartmentId == access.DepartmentId.Value)
                .Select(d => new DropdownItemDTO { Id = d.DepartmentId, Name = d.Name })
                .ToListAsync();
            return Ok(one);
        }

        var items = await _context.Departments
            .AsNoTracking()
            .Select(d => new DropdownItemDTO { Id = d.DepartmentId, Name = d.Name })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("meta/asset-categories")]
    public async Task<ActionResult<IEnumerable<DropdownItemDTO>>> GetAssetCategories()
    {
        var items = await _context.AssetCategories
            .AsNoTracking()
            .Select(c => new DropdownItemDTO { Id = c.CategoryId, Name = c.Name })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("meta/asset-types")]
    public async Task<ActionResult<IEnumerable<DropdownItemDTO>>> GetAssetTypes(
        [FromQuery] int? categoryId)
    {
        var query = _context.AssetTypes.AsNoTracking().AsQueryable();
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        var items = await query
            .Select(t => new DropdownItemDTO { Id = t.AssetTypeId, Name = t.Name })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("meta/users")]
    public async Task<ActionResult<IEnumerable<DropdownItemDTO>>> GetUsers()
    {
        var access = await GetInventoryAccessAsync();
        var q = _context.Employees.AsNoTracking().AsQueryable();
        if (access.RestrictToDepartment && access.DepartmentId.HasValue)
            q = q.Where(e => e.DepartmentId == access.DepartmentId.Value);

        var items = await q
            .Where(e => e.UserId != null)
            .Select(e => new DropdownItemDTO { Id = e.UserId!.Value, Name = e.Name })
            .ToListAsync();
        return Ok(items);
    }

    // ── Department head: scope to employee department ─────────────────────────

    private sealed class InventoryAccessInfo
    {
        public bool RestrictToDepartment { get; init; }
        public int? DepartmentId { get; init; }
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId) && userId > 0;
    }

    private async Task<InventoryAccessInfo> GetInventoryAccessAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
            return new InventoryAccessInfo { RestrictToDepartment = false };

        var roleCodes = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();

        if (roleCodes.Any(IsGlobalInventoryRole))
            return new InventoryAccessInfo { RestrictToDepartment = false };

        if (!roleCodes.Any(IsDepartmentHeadRole))
            return new InventoryAccessInfo { RestrictToDepartment = false };

        var deptId = await _context.Employees
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        return new InventoryAccessInfo { RestrictToDepartment = true, DepartmentId = deptId };
    }

    private async Task<ActionResult?> EnsureInventorySessionDepartmentAccessAsync(int sessionId)
    {
        var access = await GetInventoryAccessAsync();
        if (!access.RestrictToDepartment || !access.DepartmentId.HasValue)
            return null;

        var row = await _context.InventorySessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .Select(s => new { s.DepartmentId })
            .FirstOrDefaultAsync();

        if (row == null)
            return NotFound();

        if (row.DepartmentId != access.DepartmentId.Value)
            return Forbid();

        return null;
    }

    /// <summary>Trưởng phòng (đúng phòng ban phiên) hoặc Admin: cập nhật sổ / hoàn tất xử lý chênh lệch kiểm kê.</summary>
    private async Task<ActionResult?> EnsureDepartmentHeadOrAdminForSessionAsync(int sessionId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var roleCodes = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();

        if (roleCodes.Any(IsAdminRoleCode))
            return null;

        if (!roleCodes.Any(IsDepartmentHeadRole))
            return Forbid();

        var sessionDeptId = await _context.InventorySessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .Select(s => (int?)s.DepartmentId)
            .FirstOrDefaultAsync();
        if (!sessionDeptId.HasValue)
            return NotFound();

        var userDeptId = await _context.Employees
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        if (!userDeptId.HasValue || userDeptId.Value != sessionDeptId.Value)
            return Forbid();

        return null;
    }

    private static bool IsAdminRoleCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim().ToLowerInvariant().Replace(' ', '_');
        return c is "admin";
    }

    private async Task CloseCurrentAssetLocationsExceptAsync(
        int assetInstanceId,
        int exceptLocationId,
        DateOnly newStartDate)
    {
        var others = await _context.AssetLocations
            .Where(l => l.AssetInstanceId == assetInstanceId && l.IsCurrent && l.LocationId != exceptLocationId)
            .ToListAsync();

        foreach (var loc in others)
        {
            loc.IsCurrent = false;
            loc.EndDate = newStartDate.AddDays(-1);
        }
    }

    private async Task CloseCurrentAssetUsagesAsync(int assetInstanceId, DateOnly newStartDate)
    {
        var currents = await _context.AssetUsages
            .Where(u => u.AssetInstanceId == assetInstanceId && u.IsCurrent)
            .ToListAsync();

        foreach (var u in currents)
        {
            u.IsCurrent = false;
            u.EndDate = newStartDate.AddDays(-1);
        }
    }

    private static bool IsDepartmentHeadRole(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim().ToLowerInvariant().Replace(' ', '_');
        return c is "department_head" or "departmenthead" or "dept_head"
            or "trưởng_phòng" or "truong_phong";
    }

    private static bool IsGlobalInventoryRole(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim().ToLowerInvariant().Replace(' ', '_');
        return c is "director" or "accountant" or "admin"
            or "kế_toán" or "ke_toan"
            or "giám_đốc" or "giam_doc";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Notification.Content is VARCHAR(100) — hard-truncate to prevent DB exceptions.</summary>
    private static string? TruncateNotificationContent(string content) =>
        content.Length > 100 ? content[..97] + "..." : content;

    private async Task SafeInventoryNotifyAsync(Func<Task> notify, string step)
    {
        try
        {
            await notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory notification failed ({Step}). Session update already committed.", step);
        }
    }

    private async Task<string> GenerateSessionCode()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"KK-{today}-";
        var count = await _context.InventorySessions
            .CountAsync(s => s.Code.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }

    private async Task<InventorySessionDetailDTO> BuildSessionDetailDTO(int sessionId)
    {
        var session = await _context.InventorySessions
            .Include(s => s.Department)
            .Include(s => s.AssetCategory)
            .Include(s => s.AssetType)
            .Include(s => s.InventoryTasks).ThenInclude(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations).ThenInclude(al => al.Department)
            .Include(s => s.InventoryTasks).ThenInclude(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found after creation.");

        var buildDisplayStatus = GetDisplayStatus(session, DateTime.UtcNow);
        var dto = new InventorySessionDetailDTO
        {
            SessionId = session.SessionId,
            Code = session.Code,
            Purpose = session.Purpose,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            DepartmentId = session.DepartmentId,
            DepartmentName = session.Department?.Name ?? string.Empty,
            AssetCategoryName = session.AssetCategory?.Name ?? string.Empty,
            AssetTypeName = session.AssetType?.Name ?? string.Empty,
            Status = buildDisplayStatus,
            StatusName = GetSessionStatusName(buildDisplayStatus),
            ProgressPercent = session.ProgressPercent,
            TotalTasks = session.InventoryTasks.Count(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status)),
            CompletedTasks = 0,
            CreateDate = session.CreateDate,
            IsPeriodic = session.IsPeriodic,
            PeriodDays = session.PeriodDays,
            Tasks = session.InventoryTasks.Select(t =>
            {
                var inst = t.AssetInstance;
                var asset = inst.Asset;
                return new InventoryTaskDTO
                {
                    TaskId = t.TaskId,
                    AssetId = asset.AssetId,
                    AssetInstanceId = inst.AssetInstanceId,
                    AssetCode = asset.Code,
                    InstanceCode = inst.InstanceCode,
                    AssetName = asset.Name,
                    BookCondition = ((AssetStatus)inst.Status).ToString(),
                    BookDepartmentId = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent)?.DepartmentId,
                    BookDepartmentName = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent)?.Department?.Name,
                    BookValue = inst.CurrentValue,
                    Status = t.Status,
                    StatusName = "Chưa kiểm kê",
                    CheckDate = t.CheckDate,
                    Note = t.Note,
                    Discrepancies = new List<InventoryDiscrepancyDTO>()
                };
            }).ToList()
        };

        return dto;
    }

    private static string GetAssetStatusLabel(int status) => status switch
    {
        0 => "Sẵn sàng",
        1 => "Đang sử dụng",
        2 => "Đang bảo trì",
        3 => "Đặt trước",
        4 => "Đã thanh lý",
        5 => "Bị mất",
        6 => "Đã lý",
        7 => "Vốn hóa",
        _ => "Khác"
    };

    /// <summary>
    /// Book-side expectation for &quot;still in use&quot;: anything except idle/terminal statuses.
    /// </summary>
    private static bool BookImpliesInUse(int status) => (AssetStatus)status switch
    {
        AssetStatus.Available => false,
        AssetStatus.Disposed => false,
        AssetStatus.Lost => false,
        AssetStatus.Liquidated => false,
        _ => true
    };

    /// <summary>Parses reported status from inventory record; legacy rows may infer from book when IsFound only.</summary>
    private static int? ResolveRecordedActualStatus(InventoryRecord? record, int bookInstanceStatus)
    {
        if (record == null) return null;
        if (!string.IsNullOrWhiteSpace(record.ActualCondition) &&
            Enum.TryParse<AssetStatus>(record.ActualCondition, true, out var parsed))
            return (int)parsed;
        if (string.IsNullOrWhiteSpace(record.ActualCondition) && record.IsFound == true)
            return bookInstanceStatus;
        return null;
    }

    /// <summary>Excluded from execution list and from session progress / director checks.</summary>
    private static bool IsExcludedFromInventoryExecution(int instanceStatus) =>
        instanceStatus == (int)AssetStatus.Disposed ||
        instanceStatus == (int)AssetStatus.Lost ||
        instanceStatus == (int)AssetStatus.Liquidated ||
        instanceStatus == (int)AssetStatus.Damaged;

    private async Task UpdateSessionProgressPercentForEligibleTasksAsync(InventorySession session)
    {
        var rows = await _context.InventoryTasks
            .Where(t => t.SessionId == session.SessionId)
            .Select(t => new { t.Status, InstStatus = t.AssetInstance.Status })
            .ToListAsync();
        var eligible = rows.Where(r => !IsExcludedFromInventoryExecution(r.InstStatus)).ToList();
        var total = eligible.Count;
        var checkedCount = eligible.Count(t => t.Status == (int)InventoryTaskStatus.Checked);
        session.ProgressPercent = total > 0
            ? (int)Math.Round((double)checkedCount / total * 100)
            : 0;
    }

    /// <summary>
    /// Returns the display status for a session. Status 5 ("Đến lịch") is a computed status:
    /// the session is scheduled (DB status=0) and today falls within its start/end window.
    /// </summary>
    private static int GetDisplayStatus(InventorySession session, DateTime now) =>
        session.Status == (int)InventorySessionStatus.Scheduled
            && session.StartDate <= now && session.EndDate >= now
            ? 5
            : session.Status;

    private static string GetSessionStatusName(int status) => status switch
    {
        0 => "Đã lên lịch",
        1 => "Đang thực hiện",
        2 => "Chờ xác nhận",
        3 => "Đã hủy",
        4 => "Đã xử lý",
        5 => "Đến lịch",
        6 => "Chờ xử lý",
        _ => status.ToString()
    };

    private static string BuildDiscrepancyTypeName(int flags)
    {
        var parts = new List<string>();
        if ((flags & (int)DiscrepancyType.AssetNotFound) != 0) parts.Add("Không tìm thấy tài sản");
        if ((flags & (int)DiscrepancyType.QuantityMismatch) != 0) parts.Add("Chênh lệch số lượng");
        if ((flags & (int)DiscrepancyType.LocationMismatch) != 0) parts.Add("Sai vị trí");
        if ((flags & (int)DiscrepancyType.UserMismatch) != 0) parts.Add("Sai người sử dụng");
        if ((flags & (int)DiscrepancyType.ValueMismatch) != 0) parts.Add("Sai giá trị");
        if ((flags & (int)DiscrepancyType.ConditionMismatch) != 0) parts.Add("Sai tình trạng");
        return parts.Count > 0 ? string.Join(", ", parts) : "Không có lệch";
    }

    private async Task<Dictionary<int, string>> GetUserDisplayNamesAsync(IEnumerable<int?> userIds)
    {
        var ids = userIds
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        var map = await _context.Employees
            .Where(e => e.UserId != null && ids.Contains(e.UserId.Value))
            .AsNoTracking()
            .ToDictionaryAsync(e => e.UserId!.Value, e => e.Name);

        var missing = ids.Where(id => !map.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var emails = await _context.Users
                .Where(u => missing.Contains(u.UserId))
                .AsNoTracking()
                .ToDictionaryAsync(u => u.UserId, u => u.Email);
            foreach (var kv in emails)
                map[kv.Key] = kv.Value;
        }

        return map;
    }

    private static string? ResolveDisplayName(IReadOnlyDictionary<int, string> map, int? userId) =>
        userId.HasValue && map.TryGetValue(userId.Value, out var name) ? name : null;
}
