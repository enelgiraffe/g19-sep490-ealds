using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.DTOs.Inventory;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(EaldsDbContext context, ILogger<InventoryController> logger)
    {
        _context = context;
        _logger = logger;
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
        var query = _context.InventorySessions
            .Include(s => s.Department)
            .Include(s => s.AssetCategory)
            .Include(s => s.AssetType)
            .Include(s => s.InventoryTasks)
            .AsNoTracking()
            .AsQueryable();

        if (departmentId.HasValue)
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
                TotalTasks = s.InventoryTasks.Count,
                CompletedTasks = s.InventoryTasks.Count(t => t.Status == (int)InventoryTaskStatus.Checked),
                CreateDate = s.CreateDate,
                IsPeriodic = s.IsPeriodic,
                PeriodDays = s.PeriodDays
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
        var session = await _context.InventorySessions
            .Include(s => s.Department)
            .Include(s => s.AssetCategory)
            .Include(s => s.AssetType)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.Asset)
                    .ThenInclude(a => a.AssetLocations)
                        .ThenInclude(al => al.Department)
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

        // Load book user names (employee name via AssetUsage)
        var assetIds = session.InventoryTasks.Select(t => t.AssetId).ToList();
        var bookUserMap = await _context.AssetUsages
            .Where(u => assetIds.Contains(u.AssetId) && u.IsCurrent)
            .Include(u => u.Employee)
            .AsNoTracking()
            .ToDictionaryAsync(
                u => u.AssetId,
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

        var taskDTOs = session.InventoryTasks.Select(t =>
        {
            var bookLocation = t.Asset.AssetLocations.FirstOrDefault(al => al.IsCurrent);
            bookUserMap.TryGetValue(t.AssetId, out var bookUser);

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
                BookQuantity = t.Asset.Quantity,
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
                ActualCondition = record?.ActualCondition ?? d.ActualCondition
            }).ToList();

            return new InventoryTaskDTO
            {
                TaskId = t.TaskId,
                AssetId = t.AssetId,
                AssetCode = t.Asset.Code,
                AssetName = t.Asset.Name,
                BookCondition = ((AssetStatus)t.Asset.Status).ToString(),
                BookDepartmentId = bookLocation?.DepartmentId,
                BookDepartmentName = bookLocation?.Department?.Name,
                BookUserId = bookUser?.UserId,
                BookUserName = bookUser?.UserName,
                BookValue = t.Asset.CurrentValue,
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
            TotalTasks = session.InventoryTasks.Count,
            CompletedTasks = session.InventoryTasks.Count(t => t.Status == (int)InventoryTaskStatus.Checked),
            CreateDate = session.CreateDate,
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
    /// GET /api/inventory/sessions/{sessionId}/assets - List asset check items for a session
    /// </summary>
    [HttpGet("sessions/{sessionId:int}/assets")]
    public async Task<ActionResult<IEnumerable<SessionAssetCheckItemDTO>>> GetSessionAssets(
        int sessionId,
        [FromQuery] string? keyword,
        [FromQuery] int? checkStatus)
    {
        var sessionExists = await _context.InventorySessions.AnyAsync(s => s.SessionId == sessionId);
        if (!sessionExists) return NotFound();

        var tasks = await _context.InventoryTasks
            .Where(t => t.SessionId == sessionId)
            .Include(t => t.Asset)
                .ThenInclude(a => a.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(t => t.InventoryRecords)
            .Include(t => t.Department)
            .AsNoTracking()
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            tasks = tasks
                .Where(t => t.Asset.Code.ToLower().Contains(kw) || t.Asset.Name.ToLower().Contains(kw))
                .ToList();
        }

        var result = tasks.Select(t =>
        {
            var record = t.InventoryRecords.FirstOrDefault();
            int? actualQty = record == null
                ? null
                : record.ActualQuantity ?? (record.IsFound == true ? t.Asset.Quantity : 0);
            int? difference = actualQty.HasValue ? actualQty.Value - t.Asset.Quantity : null;
            int cs = t.Status == (int)InventoryTaskStatus.Checked ? 2 : 0;
            var currentLoc = t.Asset.AssetLocations.FirstOrDefault(al => al.IsCurrent);

            return new SessionAssetCheckItemDTO
            {
                AssetId = t.AssetId,
                AssetCode = t.Asset.Code,
                AssetName = t.Asset.Name,
                DepartmentName = currentLoc?.Department?.Name ?? t.Department?.Name ?? string.Empty,
                BookQty = t.Asset.Quantity,
                ActualQty = actualQty,
                Difference = difference,
                CheckStatus = cs
            };
        }).ToList();

        if (checkStatus.HasValue)
            result = result.Where(r => r.CheckStatus == checkStatus.Value).ToList();

        return Ok(result);
    }

    /// <summary>
    /// GET /api/inventory/sessions/{sessionId}/assets/{assetId} - Get detailed inventory form for one asset
    /// </summary>
    [HttpGet("sessions/{sessionId:int}/assets/{assetId:int}")]
    public async Task<ActionResult<AssetInventoryDetailDTO>> GetAssetInventoryDetail(
        int sessionId, int assetId)
    {
        var task = await _context.InventoryTasks
            .Where(t => t.SessionId == sessionId && t.AssetId == assetId)
            .Include(t => t.Asset)
                .ThenInclude(a => a.AssetType)
                    .ThenInclude(at => at.Category)
            .Include(t => t.Asset)
                .ThenInclude(a => a.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(t => t.InventoryRecords)
                .ThenInclude(r => r.ActualLocation)
                    .ThenInclude(al => al.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (task == null) return NotFound();

        var asset = task.Asset;
        var currentLoc = asset.AssetLocations.FirstOrDefault(al => al.IsCurrent);

        var bookUsage = await _context.AssetUsages
            .Where(u => u.AssetId == assetId && u.IsCurrent)
            .Include(u => u.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        var record = task.InventoryRecords.FirstOrDefault();
        int? actualQty = record == null ? null : (record.ActualQuantity ?? (record.IsFound == true ? asset.Quantity : 0));
        int? actualLocationId = record?.ActualLocation?.DepartmentId;

        var departments = await _context.Departments
            .AsNoTracking()
            .Select(d => new DropdownItemDTO { Id = d.DepartmentId, Name = d.Name })
            .ToListAsync();

        var managers = await _context.Employees
            .AsNoTracking()
            .Select(e => new DropdownItemDTO { Id = e.UserId, Name = e.Name })
            .ToListAsync();

        var dto = new AssetInventoryDetailDTO
        {
            AssetId = asset.AssetId,
            AssetCode = asset.Code,
            AssetName = asset.Name,
            CategoryName = asset.AssetType?.Category?.Name ?? string.Empty,
            TypeName = asset.AssetType?.Name ?? string.Empty,
            StatusEntries = new List<AssetStatusEntryDTO>
            {
                new AssetStatusEntryDTO
                {
                    StatusKey = "in_use",
                    StatusLabel = GetAssetStatusLabel(asset.Status),
                    BookQty = asset.Quantity,
                    ActualQty = actualQty
                }
            },
            BookLocationId = currentLoc?.DepartmentId,
            BookLocationName = currentLoc?.Department?.Name ?? string.Empty,
            ActualLocationId = actualLocationId,
            BookManagerId = bookUsage?.Employee?.UserId,
            BookManagerName = bookUsage?.Employee?.Name ?? string.Empty,
            ActualManagerId = record?.ActualUserId,
            Locations = departments,
            Managers = managers
        };

        return Ok(dto);
    }

    /// <summary>
    /// PUT /api/inventory/sessions/{sessionId}/assets/{assetId} - Save inventory result for an asset
    /// </summary>
    [HttpPut("sessions/{sessionId:int}/assets/{assetId:int}")]
    public async Task<ActionResult> SaveAssetInventory(
        int sessionId, int assetId, [FromBody] SaveAssetInventoryDTO dto)
    {
        var session = await _context.InventorySessions.FindAsync(sessionId);
        if (session == null) return NotFound(new { message = "Phiên kiểm kê không tồn tại." });

        if (session.Status != (int)InventorySessionStatus.InProgress)
            return BadRequest(new { message = "Chỉ có thể lưu kết quả khi phiên đang thực hiện." });

        var task = await _context.InventoryTasks
            .Where(t => t.SessionId == sessionId && t.AssetId == assetId)
            .Include(t => t.Asset)
                .ThenInclude(a => a.AssetLocations)
            .Include(t => t.InventoryRecords)
            .Include(t => t.InventoryDiscrepancies)
            .FirstOrDefaultAsync();

        if (task == null) return NotFound(new { message = "Nhiệm vụ kiểm kê không tồn tại." });

        var asset = task.Asset;
        var bookLocation = asset.AssetLocations.FirstOrDefault(al => al.IsCurrent);

        // Compute total actual qty from status entries
        var totalActualQty = dto.StatusEntries.Sum(e => e.ActualQty);
        bool isFound = totalActualQty > 0;

        // Resolve actual location: dto.ActualLocationId is a DepartmentId
        AssetLocation? actualLocation = null;
        if (dto.ActualLocationId.HasValue)
        {
            actualLocation = asset.AssetLocations
                .FirstOrDefault(al => al.DepartmentId == dto.ActualLocationId.Value);

            if (actualLocation == null)
            {
                actualLocation = await _context.AssetLocations
                    .FirstOrDefaultAsync(al => al.AssetId == assetId && al.DepartmentId == dto.ActualLocationId.Value);
            }

            if (actualLocation == null)
            {
                actualLocation = new AssetLocation
                {
                    AssetId = assetId,
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
        var resolvedCondition = !string.IsNullOrWhiteSpace(dto.ActualCondition)
            ? dto.ActualCondition
            : ((AssetStatus)asset.Status).ToString();

        if (record == null)
        {
            record = new InventoryRecord
            {
                TaskId = task.TaskId,
                ActualLocationId = actualLocation.LocationId,
                ActualUserId = dto.ActualManagerId,
                ActualCondition = resolvedCondition,
                IsFound = isFound,
                ActualQuantity = totalActualQty,
                CheckedBy = dto.CheckedBy > 0 ? dto.CheckedBy : (dto.ActualManagerId ?? 1),
                CheckedDate = DateTime.UtcNow,
                DateCheckCompleted = DateTime.UtcNow
            };
            _context.InventoryRecords.Add(record);
        }
        else
        {
            record.ActualLocationId = actualLocation.LocationId;
            record.ActualUserId = dto.ActualManagerId;
            record.ActualCondition = resolvedCondition;
            record.IsFound = isFound;
            record.ActualQuantity = totalActualQty;
            record.CheckedDate = DateTime.UtcNow;
            record.DateCheckCompleted = DateTime.UtcNow;
        }

        // Detect discrepancies
        var bookUsage = await _context.AssetUsages
            .Where(u => u.AssetId == assetId && u.IsCurrent)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync();
        var bookManagerId = bookUsage?.Employee?.UserId;

        int discrepancyFlags = 0;
        if (!isFound)
        {
            discrepancyFlags |= (int)DiscrepancyType.AssetNotFound;
        }
        else
        {
            if (totalActualQty != asset.Quantity)
                discrepancyFlags |= (int)DiscrepancyType.QuantityMismatch;

            if (bookLocation != null && dto.ActualLocationId.HasValue &&
                bookLocation.DepartmentId != dto.ActualLocationId.Value)
                discrepancyFlags |= (int)DiscrepancyType.LocationMismatch;

            if (dto.ActualManagerId != bookManagerId &&
                (dto.ActualManagerId.HasValue || bookManagerId.HasValue))
                discrepancyFlags |= (int)DiscrepancyType.UserMismatch;
        }

        // Replace existing discrepancies for this task
        _context.InventoryDiscrepancies.RemoveRange(task.InventoryDiscrepancies);

        if (discrepancyFlags != 0)
        {
            _context.InventoryDiscrepancies.Add(new InventoryDiscrepancy
            {
                TaskId = task.TaskId,
                DiscrepancyType = discrepancyFlags,
                BookValue = asset.CurrentValue,
                BookLocationId = bookLocation?.LocationId ?? actualLocation.LocationId,
                BookUserId = bookManagerId,
                BookCondition = ((AssetStatus)asset.Status).ToString(),
                ActualValue = asset.CurrentValue,
                ActualLocationId = actualLocation.LocationId,
                ActualUserId = dto.ActualManagerId,
                ActualCondition = resolvedCondition
            });
        }

        task.Status = (int)InventoryTaskStatus.Checked;
        await _context.SaveChangesAsync();

        var totalTasks = await _context.InventoryTasks.CountAsync(t => t.SessionId == sessionId);
        var checkedTasks = await _context.InventoryTasks
            .CountAsync(t => t.SessionId == sessionId && t.Status == (int)InventoryTaskStatus.Checked);
        session.ProgressPercent = totalTasks > 0
            ? (int)Math.Round((double)checkedTasks / totalTasks * 100)
            : 0;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã lưu thông tin kiểm kê." });
    }

    /// <summary>
    /// POST /api/inventory/sessions - Create a new inventory session and auto-generate tasks for matching assets
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult> CreateSession([FromBody] CreateInventorySessionDTO dto)
    {
        var department = await _context.Departments.FindAsync(dto.DepartmentId);
        if (department == null)
            return BadRequest(new { message = "Phòng ban không tồn tại." });

        // Collect all active assets in the department, regardless of category/type.
        var assets = await _context.Assets
            .Where(a =>
                a.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == dto.DepartmentId) &&
                a.Status != (int)AssetStatus.Disposed &&
                a.Status != (int)AssetStatus.Lost &&
                a.Status != (int)AssetStatus.Liquidated)
            .AsNoTracking()
            .ToListAsync();

        if (!assets.Any())
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

            foreach (var asset in assets)
            {
                session.InventoryTasks.Add(new InventoryTask
                {
                    AssetId = asset.AssetId,
                    AssignedUserId = dto.CreatedBy,
                    DepartmentId = dto.DepartmentId,
                    Status = (int)InventoryTaskStatus.Pending,
                    CheckDate = dto.EndDate
                });
            }

            _context.InventorySessions.Add(session);
            await _context.SaveChangesAsync();

            // Notification.Content column is VARCHAR(100) — keep the string concise
            _context.Notifications.Add(new Notification
            {
                Title = $"Lịch kiểm kê: {session.Code}",
                Content = $"Kiểm kê {session.Code} bắt đầu {session.StartDate:dd/MM/yyyy}.",
                RefId = session.SessionId,
                SentDate = session.StartDate,
                IsSend = false
            });

            await _context.SaveChangesAsync();
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
        var session = await _context.InventorySessions.FindAsync(id);
        if (session == null)
            return NotFound(new { message = "Phiên kiểm kê không tồn tại." });

        if (session.Status != (int)InventorySessionStatus.InProgress)
            return BadRequest(new { message = "Chỉ có thể ghi nhận kết quả khi phiên kiểm kê đang thực hiện." });

        var task = await _context.InventoryTasks
            .Include(t => t.Asset)
                .ThenInclude(a => a.AssetLocations)
            .Include(t => t.InventoryRecords)
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.SessionId == id);

        if (task == null)
            return NotFound(new { message = "Nhiệm vụ kiểm kê không tồn tại." });

        if (task.Status == (int)InventoryTaskStatus.Checked)
            return BadRequest(new { message = "Nhiệm vụ này đã được kiểm kê rồi." });

        var asset = task.Asset;
        var bookLocation = asset.AssetLocations.FirstOrDefault(al => al.IsCurrent);
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
                    al.AssetId == task.AssetId &&
                    al.DepartmentId == dto.ActualDepartmentId &&
                    !al.IsCurrent)
                ?? new AssetLocation();

            if (actualLocation.LocationId == 0)
            {
                actualLocation = new AssetLocation
                {
                    AssetId = task.AssetId,
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
            .Where(u => u.AssetId == task.AssetId && u.IsCurrent)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync();
        var bookUserId = bookUsage?.Employee?.UserId;
        var bookCondition = ((AssetStatus)asset.Status).ToString();
        var bookValue = asset.CurrentValue;

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

        // Recalculate session progress
        var totalTasks = await _context.InventoryTasks.CountAsync(t => t.SessionId == id);
        var checkedTasks = await _context.InventoryTasks
            .CountAsync(t => t.SessionId == id && t.Status == (int)InventoryTaskStatus.Checked);
        session.ProgressPercent = totalTasks > 0
            ? (int)Math.Round((double)checkedTasks / totalTasks * 100)
            : 0;
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
        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        if (session.Status == (int)InventorySessionStatus.Completed)
            return BadRequest(new { message = "Phiên kiểm kê này đã hoàn thành." });

        var totalTasks = session.InventoryTasks.Count;
        var checkedTasks = session.InventoryTasks.Count(t => t.Status == (int)InventoryTaskStatus.Checked);

        session.Status = (int)InventorySessionStatus.Completed;
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
    /// GET /api/inventory/sessions/{id}/review-summary — Báo cáo chênh lệch cho giám đốc / kế toán
    /// </summary>
    [HttpGet("sessions/{id:int}/review-summary")]
    public async Task<ActionResult<InventoryReviewSummaryDTO>> GetReviewSummary(int id)
    {
        var session = await _context.InventorySessions
            .Include(s => s.Department)
            .Include(s => s.AssetCategory)
            .Include(s => s.AssetType)
            .Include(s => s.InventoryTasks)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        var discrepancies = await _context.InventoryDiscrepancies
            .Where(d => d.Task.SessionId == id)
            .Include(d => d.Task).ThenInclude(t => t.Asset)
            .Include(d => d.Task).ThenInclude(t => t.InventoryRecords)
            .Include(d => d.BookLocation).ThenInclude(bl => bl.Department)
            .Include(d => d.ActualLocation).ThenInclude(al => al.Department)
            .Include(d => d.BookUser)
            .Include(d => d.ActualUser)
            .AsNoTracking()
            .ToListAsync();

        var userNameMap = await GetUserDisplayNamesAsync(
            discrepancies.SelectMany(d => new[] { d.BookUserId, d.ActualUserId }));

        var detailList = discrepancies.Select(d =>
        {
            var record = d.Task.InventoryRecords
                .OrderByDescending(r => r.RecordId)
                .FirstOrDefault();
            return new InventoryDiscrepancyDetailDTO
            {
                DiscrepancyId = d.DiscrepancyId,
                TaskId = d.Task.TaskId,
                AssetId = d.Task.AssetId,
                AssetCode = d.Task.Asset.Code,
                AssetName = d.Task.Asset.Name,
                DiscrepancyType = d.DiscrepancyType,
                DiscrepancyTypeName = BuildDiscrepancyTypeName(d.DiscrepancyType),
                BookValue = d.BookValue,
                BookQuantity = d.Task.Asset.Quantity,
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
                ActualCondition = record?.ActualCondition ?? d.ActualCondition
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
            TotalTasks = session.InventoryTasks.Count,
            CompletedTasks = session.InventoryTasks.Count(t => t.Status == (int)InventoryTaskStatus.Checked),
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
    /// POST /api/inventory/sessions/{id}/director-approve — Chờ xác nhận → Chờ xử lý (có lệch SL/người phụ trách) hoặc Đã xử lý (không lệch)
    /// </summary>
    [HttpPost("sessions/{id:int}/director-approve")]
    public async Task<ActionResult> DirectorApproveSession(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.Asset)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryRecords)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.Completed)
            return BadRequest(new { message = "Chỉ có thể xác nhận khi phiên đang ở trạng thái Chờ xác nhận." });

        var assetIds = session.InventoryTasks.Select(t => t.AssetId).Distinct().ToList();
        var bookUsages = await _context.AssetUsages
            .Where(u => assetIds.Contains(u.AssetId) && u.IsCurrent)
            .Include(u => u.Employee)
            .AsNoTracking()
            .ToDictionaryAsync(u => u.AssetId, u => u.Employee?.UserId);

        var hasMismatch = false;
        foreach (var task in session.InventoryTasks)
        {
            var asset = task.Asset;
            bookUsages.TryGetValue(task.AssetId, out var bookUserId);

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

            var actualQty = record.ActualQuantity ?? (record.IsFound == true ? asset.Quantity : 0);
            if (actualQty != asset.Quantity)
            {
                hasMismatch = true;
                break;
            }

            var actualUserId = record.ActualUserId;
            if (actualUserId != bookUserId && (actualUserId.HasValue || bookUserId.HasValue))
            {
                hasMismatch = true;
                break;
            }
        }

        session.Status = hasMismatch
            ? (int)InventorySessionStatus.PendingAccountant
            : (int)InventorySessionStatus.Confirmed;

        await _context.SaveChangesAsync();

        var displayStatus = GetDisplayStatus(session, DateTime.UtcNow);

        return Ok(new
        {
            message = hasMismatch
                ? "Đã xác nhận. Phiên chuyển sang Chờ xử lý (kế toán)."
                : "Đã xác nhận. Không có chênh lệch số lượng hoặc người phụ trách so với sổ — phiên đã xử lý.",
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

        return Ok(new { message = "Đã gửi yêu cầu kiểm kê lại. Phiên chuyển sang Đang thực hiện.", sessionId = id });
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/confirm — Kế toán: Chờ xử lý → Đã xử lý
    /// </summary>
    [HttpPost("sessions/{id:int}/confirm")]
    public async Task<ActionResult> AccountantConfirmSession(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        var session = await _context.InventorySessions.FindAsync(id);
        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.PendingAccountant)
            return BadRequest(new { message = "Chỉ có thể hoàn tất khi phiên đang ở trạng thái Chờ xử lý." });

        if (dto.ApplyCorrections)
        {
            // Áp dụng chỉnh sửa sổ từ chênh lệch — có thể mở rộng sau.
        }

        session.Status = (int)InventorySessionStatus.Confirmed;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã hoàn tất xử lý chênh lệch trên sổ sách.", sessionId = id });
    }

    /// <summary>
    /// PUT /api/inventory/sessions/{id} - Update purpose and dates of a scheduled session
    /// </summary>
    [HttpPut("sessions/{id:int}")]
    public async Task<ActionResult> UpdateSession(int id, [FromBody] UpdateInventorySessionDTO dto)
    {
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
        var session = await _context.InventorySessions.FindAsync(id);
        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.Scheduled)
            return BadRequest(new { message = "Chỉ có thể kích hoạt phiên kiểm kê ở trạng thái Đã lên lịch." });

        session.Status = (int)InventorySessionStatus.InProgress;

        // Mark the scheduled notification as sent
        var scheduledNotification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.RefId == id && !n.IsSend);
        if (scheduledNotification != null)
        {
            scheduledNotification.IsSend = true;
            scheduledNotification.SentDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Phiên kiểm kê đã được kích hoạt. Thông báo kiểm kê đã được gửi." });
    }

    /// <summary>
    /// POST /api/inventory/sessions/{id}/cancel
    /// Cancels a scheduled (status = Đã lên lịch) inventory session.
    /// </summary>
    [HttpPost("sessions/{id:int}/cancel")]
    public async Task<ActionResult> CancelSession(int id, [FromBody] ReviewInventorySessionDTO dto)
    {
        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound();

        if (session.Status != (int)InventorySessionStatus.Scheduled)
            return BadRequest(new { message = "Chỉ có thể hủy phiên kiểm kê đang ở trạng thái 'Đã lên lịch'." });

        session.Status = (int)InventorySessionStatus.Cancelled;

        _context.Notifications.Add(new Notification
        {
            Title = $"Lịch kiểm kê bị hủy: {session.Code}",
            Content = TruncateNotificationContent($"Phiên {session.Code} đã bị hủy. Lý do: {dto.ReviewNotes ?? "Không có ghi chú."}"),
            RefId = id,
            SentDate = DateTime.UtcNow,
            IsSend = true
        });

        // If this is a periodic session, also cancel all future scheduled periodic sessions
        // for the same department so the recurrence chain is stopped.
        int cancelledChainCount = 0;
        if (session.IsPeriodic)
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
        var sessionExists = await _context.InventorySessions.AnyAsync(s => s.SessionId == id);
        if (!sessionExists) return NotFound();

        var discrepancies = await _context.InventoryDiscrepancies
            .Where(d => d.Task.SessionId == id)
            .Include(d => d.Task).ThenInclude(t => t.Asset)
            .Include(d => d.Task).ThenInclude(t => t.InventoryRecords)
            .Include(d => d.BookLocation).ThenInclude(bl => bl.Department)
            .Include(d => d.ActualLocation).ThenInclude(al => al.Department)
            .Include(d => d.BookUser)
            .Include(d => d.ActualUser)
            .AsNoTracking()
            .ToListAsync();

        var userNameMap = await GetUserDisplayNamesAsync(
            discrepancies.SelectMany(d => new[] { d.BookUserId, d.ActualUserId }));

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
                BookQuantity = d.Task.Asset.Quantity,
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
                ActualCondition = record?.ActualCondition ?? d.ActualCondition
            };
        }).ToList();

        return Ok(result);
    }

    // ── Metadata endpoints for dropdowns ──────────────────────────────────────

    [HttpGet("meta/departments")]
    public async Task<ActionResult<IEnumerable<DropdownItemDTO>>> GetDepartments()
    {
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
        var items = await _context.Employees
            .AsNoTracking()
            .Select(e => new DropdownItemDTO { Id = e.UserId, Name = e.Name })
            .ToListAsync();
        return Ok(items);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Notification.Content is VARCHAR(100) — hard-truncate to prevent DB exceptions.</summary>
    private static string? TruncateNotificationContent(string content) =>
        content.Length > 100 ? content[..97] + "..." : content;

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
            .Include(s => s.InventoryTasks).ThenInclude(t => t.Asset)
                .ThenInclude(a => a.AssetLocations).ThenInclude(al => al.Department)
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
            TotalTasks = session.InventoryTasks.Count,
            CompletedTasks = 0,
            CreateDate = session.CreateDate,
            IsPeriodic = session.IsPeriodic,
            PeriodDays = session.PeriodDays,
            Tasks = session.InventoryTasks.Select(t => new InventoryTaskDTO
            {
                TaskId = t.TaskId,
                AssetId = t.AssetId,
                AssetCode = t.Asset?.Code ?? string.Empty,
                AssetName = t.Asset?.Name ?? string.Empty,
                BookCondition = t.Asset != null ? ((AssetStatus)t.Asset.Status).ToString() : string.Empty,
                BookDepartmentId = t.Asset?.AssetLocations.FirstOrDefault(al => al.IsCurrent)?.DepartmentId,
                BookDepartmentName = t.Asset?.AssetLocations.FirstOrDefault(al => al.IsCurrent)?.Department?.Name,
                BookValue = t.Asset?.CurrentValue ?? 0,
                Status = t.Status,
                StatusName = "Chưa kiểm kê",
                CheckDate = t.CheckDate,
                Note = t.Note,
                Discrepancies = new List<InventoryDiscrepancyDTO>()
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
            .Where(e => ids.Contains(e.UserId))
            .AsNoTracking()
            .ToDictionaryAsync(e => e.UserId, e => e.Name);

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
