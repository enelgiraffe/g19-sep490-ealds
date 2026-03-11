using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public InventoryController(EaldsDbContext context)
    {
        _context = context;
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
            query = query.Where(s => s.Status == status.Value);

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

        var result = sessions.Select(s => new InventorySessionListItemDTO
        {
            SessionId = s.SessionId,
            Code = s.Code,
            Purpose = s.Purpose,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            DepartmentId = s.DepartmentId,
            DepartmentName = s.Department.Name,
            AssetCategoryName = s.AssetCategory.Name,
            AssetTypeName = s.AssetType.Name,
            Status = s.Status,
            StatusName = GetSessionStatusName(s.Status),
            ProgressPercent = s.ProgressPercent,
            TotalTasks = s.InventoryTasks.Count,
            CompletedTasks = s.InventoryTasks.Count(t => t.Status == (int)InventoryTaskStatus.Checked),
            CreateDate = s.CreateDate
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
                BookDepartmentName = d.BookLocation?.Department?.Name,
                BookUserId = d.BookUserId,
                BookCondition = d.BookCondition,
                ActualValue = d.ActualValue,
                ActualDepartmentName = d.ActualLocation?.Department?.Name,
                ActualUserId = d.ActualUserId,
                ActualCondition = d.ActualCondition
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

        var dto = new InventorySessionDetailDTO
        {
            SessionId = session.SessionId,
            Code = session.Code,
            Purpose = session.Purpose,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            DepartmentId = session.DepartmentId,
            DepartmentName = session.Department.Name,
            AssetCategoryName = session.AssetCategory.Name,
            AssetTypeName = session.AssetType.Name,
            Status = session.Status,
            StatusName = GetSessionStatusName(session.Status),
            ProgressPercent = session.ProgressPercent,
            TotalTasks = session.InventoryTasks.Count,
            CompletedTasks = session.InventoryTasks.Count(t => t.Status == (int)InventoryTaskStatus.Checked),
            CreateDate = session.CreateDate,
            Tasks = taskDTOs
        };

        return Ok(dto);
    }

    /// <summary>
    /// POST /api/inventory/sessions - Create a new inventory session and auto-generate tasks for matching assets
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult<InventorySessionDetailDTO>> CreateSession([FromBody] CreateInventorySessionDTO dto)
    {
        var department = await _context.Departments.FindAsync(dto.DepartmentId);
        if (department == null)
            return BadRequest(new { message = "Phòng ban không tồn tại." });

        var category = await _context.AssetCategories.FindAsync(dto.AssetCategoryId);
        if (category == null)
            return BadRequest(new { message = "Danh mục tài sản không tồn tại." });

        var assetType = await _context.AssetTypes.FindAsync(dto.AssetTypeId);
        if (assetType == null)
            return BadRequest(new { message = "Loại tài sản không tồn tại." });

        // Find all active assets in the department matching the selected type
        var assets = await _context.Assets
            .Where(a =>
                a.AssetTypeId == dto.AssetTypeId &&
                a.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == dto.DepartmentId) &&
                a.Status != (int)AssetStatus.Disposed &&
                a.Status != (int)AssetStatus.Lost &&
                a.Status != (int)AssetStatus.Liquidated)
            .AsNoTracking()
            .ToListAsync();

        if (!assets.Any())
            return BadRequest(new { message = "Không có tài sản nào trong phòng ban này thuộc loại tài sản đã chọn." });

        var code = await GenerateSessionCode();

        var session = new InventorySession
        {
            Code = code,
            Purpose = dto.Purpose,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DepartmentId = dto.DepartmentId,
            AssetCategoryId = dto.AssetCategoryId,
            AssetTypeId = dto.AssetTypeId,
            Status = (int)InventorySessionStatus.InProgress,
            ProgressPercent = 0,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow
        };

        _context.InventorySessions.Add(session);
        await _context.SaveChangesAsync();

        foreach (var asset in assets)
        {
            _context.InventoryTasks.Add(new InventoryTask
            {
                AssetId = asset.AssetId,
                SessionId = session.SessionId,
                AssignedUserId = dto.CreatedBy,
                DepartmentId = dto.DepartmentId,
                Status = (int)InventoryTaskStatus.Pending,
                CheckDate = dto.EndDate
            });
        }

        await _context.SaveChangesAsync();

        var detail = await BuildSessionDetailDTO(session.SessionId);
        return CreatedAtAction(nameof(GetSessionById), new { id = session.SessionId }, detail);
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

        if (session.Status == (int)InventorySessionStatus.Completed)
            return BadRequest(new { message = "Phiên kiểm kê đã hoàn thành, không thể cập nhật." });

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

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Phiên kiểm kê đã được hoàn thành.",
            progressPercent = session.ProgressPercent,
            checkedTasks,
            totalTasks
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
            .Include(d => d.BookLocation).ThenInclude(bl => bl.Department)
            .Include(d => d.ActualLocation).ThenInclude(al => al.Department)
            .Include(d => d.BookUser)
            .Include(d => d.ActualUser)
            .AsNoTracking()
            .ToListAsync();

        var result = discrepancies.Select(d => new InventoryDiscrepancyDTO
        {
            DiscrepancyId = d.DiscrepancyId,
            DiscrepancyType = d.DiscrepancyType,
            DiscrepancyTypeName = BuildDiscrepancyTypeName(d.DiscrepancyType),
            BookValue = d.BookValue,
            BookDepartmentName = d.BookLocation?.Department?.Name,
            BookUserId = d.BookUserId,
            BookUserName = d.BookUser?.Email,
            BookCondition = d.BookCondition,
            ActualValue = d.ActualValue,
            ActualDepartmentName = d.ActualLocation?.Department?.Name,
            ActualUserId = d.ActualUserId,
            ActualUserName = d.ActualUser?.Email,
            ActualCondition = d.ActualCondition
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
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        var dto = new InventorySessionDetailDTO
        {
            SessionId = session!.SessionId,
            Code = session.Code,
            Purpose = session.Purpose,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            DepartmentId = session.DepartmentId,
            DepartmentName = session.Department.Name,
            AssetCategoryName = session.AssetCategory.Name,
            AssetTypeName = session.AssetType.Name,
            Status = session.Status,
            StatusName = GetSessionStatusName(session.Status),
            ProgressPercent = session.ProgressPercent,
            TotalTasks = session.InventoryTasks.Count,
            CompletedTasks = 0,
            CreateDate = session.CreateDate,
            Tasks = session.InventoryTasks.Select(t => new InventoryTaskDTO
            {
                TaskId = t.TaskId,
                AssetId = t.AssetId,
                AssetCode = t.Asset.Code,
                AssetName = t.Asset.Name,
                BookCondition = ((AssetStatus)t.Asset.Status).ToString(),
                BookDepartmentId = t.Asset.AssetLocations.FirstOrDefault(al => al.IsCurrent)?.DepartmentId,
                BookDepartmentName = t.Asset.AssetLocations.FirstOrDefault(al => al.IsCurrent)?.Department?.Name,
                BookValue = t.Asset.CurrentValue,
                Status = t.Status,
                StatusName = "Chưa kiểm kê",
                CheckDate = t.CheckDate,
                Note = t.Note,
                Discrepancies = new List<InventoryDiscrepancyDTO>()
            }).ToList()
        };

        return dto;
    }

    private static string GetSessionStatusName(int status) => status switch
    {
        0 => "Nháp",
        1 => "Đang thực hiện",
        2 => "Hoàn thành",
        3 => "Đã hủy",
        _ => status.ToString()
    };

    private static string BuildDiscrepancyTypeName(int flags)
    {
        var parts = new List<string>();
        if ((flags & (int)DiscrepancyType.AssetNotFound) != 0) parts.Add("Không tìm thấy tài sản");
        if ((flags & (int)DiscrepancyType.LocationMismatch) != 0) parts.Add("Sai vị trí");
        if ((flags & (int)DiscrepancyType.UserMismatch) != 0) parts.Add("Sai người sử dụng");
        if ((flags & (int)DiscrepancyType.ValueMismatch) != 0) parts.Add("Sai giá trị");
        if ((flags & (int)DiscrepancyType.ConditionMismatch) != 0) parts.Add("Sai tình trạng");
        return parts.Count > 0 ? string.Join(", ", parts) : "Không có lệch";
    }
}
