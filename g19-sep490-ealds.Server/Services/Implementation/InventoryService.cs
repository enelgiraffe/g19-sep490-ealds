using g19_sep490_ealds.Server.DTOs.Inventory;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class InventoryService : IInventoryService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<InventoryService> _logger;
    private readonly IInventoryNotificationService _inventoryNotifications;

    public InventoryService(
        EaldsDbContext context,
        ILogger<InventoryService> logger,
        IInventoryNotificationService inventoryNotifications)
    {
        _context = context;
        _logger = logger;
        _inventoryNotifications = inventoryNotifications;
    }

    public async Task<IEnumerable<InventorySessionListItemDTO>> GetSessionsAsync(
        int userId, int? departmentId, int? status, string? keyword, bool directorInventoryReport = false)
    {
        var isDirOrAdmin = await UserIsDirectorOrAdminAsync(userId);

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

        if (!isDirOrAdmin)
            query = query.Where(s => s.CreatedBy == userId);
        if (departmentId.HasValue)
            query = query.Where(s => s.DepartmentId == departmentId.Value);

        var startOfCurrentUtcDay = new DateTime(
            DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc);

        if (directorInventoryReport && isDirOrAdmin)
        {
            query = query.Where(s =>
                s.Status == (int)InventorySessionStatus.Completed ||
                s.Status == (int)InventorySessionStatus.Confirmed ||
                s.Status == (int)InventorySessionStatus.PendingAccountant ||
                ((s.Status == (int)InventorySessionStatus.Scheduled || s.Status == (int)InventorySessionStatus.InProgress) &&
                 s.EndDate < startOfCurrentUtcDay));
        }

        if (status.HasValue)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);
            if (status.Value == 5)
            {
                query = query.Where(s =>
                    s.Status == 0 &&
                    s.StartDate < tomorrowUtc &&
                    s.EndDate >= todayUtc);
            }
            else if (status.Value == (int)InventorySessionStatus.PendingAccountant
                     || status.Value == (int)InventorySessionStatus.Completed)
            {
                query = query.Where(s =>
                    s.Status == (int)InventorySessionStatus.PendingAccountant ||
                    s.Status == (int)InventorySessionStatus.Completed);
            }
            else if (status.Value == 7)
            {
                query = query.Where(s =>
                    (s.Status == (int)InventorySessionStatus.Scheduled || s.Status == (int)InventorySessionStatus.InProgress) &&
                    s.EndDate < startOfCurrentUtcDay);
            }
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
        return sessions.Select(s =>
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
    }

    public async Task<InventorySessionDetailDTO> GetSessionByIdAsync(int userId, int sessionId)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

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
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        var instanceIds = session.InventoryTasks.Select(t => t.AssetInstanceId).ToList();
        var bookUserMap = await _context.AssetUsages
            .Where(u => instanceIds.Contains(u.AssetInstanceId) && u.IsCurrent)
            .Include(u => u.Employee)
            .AsNoTracking()
            .ToDictionaryAsync(
                u => u.AssetInstanceId,
                u => new { UserId = u.Employee.UserId, UserName = u.Employee.Name });

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
        return new InventorySessionDetailDTO
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
    }

    public Task<IEnumerable<SessionAssetCheckItemDTO>> GetSessionAssetsAsync(
        int userId, int sessionId, string? keyword, int? checkStatus)
        => GetSessionAssetCheckItemsAsync(userId, sessionId, catalogAssetId: null, keyword, checkStatus);

    public Task<IEnumerable<SessionAssetCheckItemDTO>> GetSessionAssetsForCatalogAssetAsync(
        int userId, int sessionId, int assetId, string? keyword, int? checkStatus)
        => GetSessionAssetCheckItemsAsync(userId, sessionId, catalogAssetId: assetId, keyword, checkStatus);

    private async Task<IEnumerable<SessionAssetCheckItemDTO>> GetSessionAssetCheckItemsAsync(
        int userId, int sessionId, int? catalogAssetId, string? keyword, int? checkStatus)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

        var sessionExists = await _context.InventorySessions.AnyAsync(s => s.SessionId == sessionId);
        if (!sessionExists) throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        var query = _context.InventoryTasks
            .Where(t => t.SessionId == sessionId)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
                    .ThenInclude(al => al.Department)
            .Include(t => t.InventoryRecords)
            .Include(t => t.InventoryDiscrepancies)
            .Include(t => t.Department)
            .AsNoTracking()
            .AsQueryable();

        if (catalogAssetId.HasValue)
            query = query.Where(t => t.AssetInstance.AssetId == catalogAssetId.Value);

        query = query.Where(t => t.AssetInstance.Status == (int)AssetStatus.InUse);

        var tasks = await query.ToListAsync();

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
                CheckStatus = cs,
                HasDiscrepancy = t.InventoryDiscrepancies.Count > 0
            };
        }).ToList();

        if (checkStatus.HasValue)
            result = result.Where(r => r.CheckStatus == checkStatus.Value).ToList();

        return result;
    }

    public async Task<AssetInventoryDetailDTO> GetAssetInventoryDetailAsync(int userId, int sessionId, int assetInstanceId)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

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

        if (task == null) throw new KeyNotFoundException("Nhiệm vụ kiểm kê không tồn tại.");

        var inst = task.AssetInstance;
        var asset = inst.Asset;
        if (IsExcludedFromInventoryExecution(inst.Status))
            throw new KeyNotFoundException("Chỉ có thể xem cá thể ở trạng thái Đang sử dụng trong kiểm kê.");

        var currentLoc = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent);
        var record = task.InventoryRecords.FirstOrDefault();
        int? actualLocationId = record?.ActualLocation?.DepartmentId;
        var actualStatusResolved = ResolveRecordedActualStatus(record, inst.Status);

        var departments = await _context.Departments
            .AsNoTracking()
            .Select(d => new DropdownItemDTO { Id = d.DepartmentId, Name = d.Name })
            .ToListAsync();

        return new AssetInventoryDetailDTO
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
    }

    public async Task SaveAssetInventoryAsync(int userId, int sessionId, int assetInstanceId, SaveAssetInventoryDTO dto)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

        if (dto.AssetInstanceId > 0 && dto.AssetInstanceId != assetInstanceId)
            throw new InvalidOperationException("AssetInstanceId trong body không khớp với đường dẫn.");

        var session = await _context.InventorySessions.FindAsync(sessionId);
        if (session == null) throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.InProgress)
            throw new InvalidOperationException("Chỉ có thể lưu kết quả khi phiên đang thực hiện.");

        var task = await _context.InventoryTasks
            .Where(t => t.SessionId == sessionId && t.AssetInstanceId == assetInstanceId)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(t => t.InventoryRecords)
            .Include(t => t.InventoryDiscrepancies)
            .FirstOrDefaultAsync();

        if (task == null) throw new KeyNotFoundException("Nhiệm vụ kiểm kê không tồn tại.");

        var inst = task.AssetInstance;
        var bookLocation = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent);
        if (IsExcludedFromInventoryExecution(inst.Status))
            throw new InvalidOperationException("Chỉ có thể kiểm kê cá thể ở trạng thái Đang sử dụng.");

        if (!Enum.IsDefined(typeof(AssetStatus), dto.ActualStatus))
            throw new InvalidOperationException("Trạng thái tài sản không hợp lệ.");

        var reported = (AssetStatus)dto.ActualStatus;
        var storedCondition = reported.ToString();
        bool actualInUseBucket = BookImpliesInUse(dto.ActualStatus);
        int bookStatusInt = inst.Status;
        bool bookInUseBucket = BookImpliesInUse(bookStatusInt);

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
            throw new InvalidOperationException("Tài sản không có thông tin vị trí trong hệ thống.");

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

        await UpdateSessionProgressPercentAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task<CreateSessionResultDTO> CreateSessionAsync(int userId, CreateInventorySessionDTO dto)
    {
        dto.CreatedBy = userId;

        var created = await CreateInventorySessionCoreAsync(
            dto.Purpose ?? string.Empty,
            dto.StartDate,
            dto.EndDate,
            dto.DepartmentId,
            dto.CreatedBy,
            dto.IsPeriodic,
            dto.IsPeriodic ? dto.PeriodDays : null,
            assetCategoryId: null,
            assetTypeId: null);

        if (!created.Success)
            throw new InvalidOperationException(created.ErrorMessage);

        return new CreateSessionResultDTO
        {
            Message = "Đã lên lịch kiểm kê thành công.",
            SessionIds = new[] { created.SessionId!.Value },
            Count = 1
        };
    }

    public async Task<SubmitTaskRecordResultDTO> SubmitTaskRecordAsync(int userId, int sessionId, int taskId, SubmitInventoryTaskDTO dto)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions.FindAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.InProgress)
            throw new InvalidOperationException("Chỉ có thể ghi nhận kết quả khi phiên kiểm kê đang thực hiện.");

        var task = await _context.InventoryTasks
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.AssetLocations)
            .Include(t => t.AssetInstance)
                .ThenInclude(ai => ai.Asset)
            .Include(t => t.InventoryRecords)
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.SessionId == sessionId);

        if (task == null)
            throw new KeyNotFoundException("Nhiệm vụ kiểm kê không tồn tại.");

        if (task.Status == (int)InventoryTaskStatus.Checked)
            throw new InvalidOperationException("Nhiệm vụ này đã được kiểm kê rồi.");

        var inst = task.AssetInstance;
        if (IsExcludedFromInventoryExecution(inst.Status))
            throw new InvalidOperationException("Chỉ có thể kiểm kê cá thể ở trạng thái Đang sử dụng.");
        var bookLocation = inst.AssetLocations.FirstOrDefault(al => al.IsCurrent);
        if (bookLocation == null)
            throw new InvalidOperationException("Tài sản không có thông tin vị trí trong hệ thống.");

        AssetLocation actualLocation;
        if (!dto.IsFound || bookLocation.DepartmentId == dto.ActualDepartmentId)
        {
            actualLocation = bookLocation;
        }
        else
        {
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

        task.Status = (int)InventoryTaskStatus.Checked;
        if (dto.Note != null) task.Note = dto.Note;

        await _context.SaveChangesAsync();

        await UpdateSessionProgressPercentAsync(session);
        await _context.SaveChangesAsync();

        return new SubmitTaskRecordResultDTO
        {
            Message = "Đã ghi nhận kết quả kiểm kê.",
            DiscrepancyDetected = discrepancyFlags != 0,
            DiscrepancyType = discrepancyFlags,
            DiscrepancyTypeName = BuildDiscrepancyTypeName(discrepancyFlags),
            ProgressPercent = session.ProgressPercent
        };
    }

    public async Task<CompleteSessionResultDTO> CompleteSessionAsync(int userId, int sessionId)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.InProgress)
            throw new InvalidOperationException("Chỉ có thể hoàn thành kiểm kê khi phiên đang ở trạng thái Đang thực hiện.");

        var eligibleTasks = session.InventoryTasks
            .Where(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status))
            .ToList();
        var totalTasks = eligibleTasks.Count;
        var checkedTasks = eligibleTasks.Count(t => t.Status == (int)InventoryTaskStatus.Checked);

        if (totalTasks == 0)
            throw new InvalidOperationException("Phiên không có cá thể ở trạng thái Đang sử dụng để kiểm kê. Hãy tạo phiên mới sau khi cập nhật trạng thái tài sản hợp lệ.");
        if (checkedTasks < totalTasks)
            throw new InvalidOperationException("Cần hoàn tất kiểm kê 100% tài sản trước khi kết thúc phiên.");

        session.ProgressPercent = totalTasks > 0
            ? (int)Math.Round((double)checkedTasks / totalTasks * 100)
            : 0;

        var taskIds = session.InventoryTasks.Select(t => t.TaskId).ToList();
        var discrepancies = await _context.InventoryDiscrepancies
            .Where(d => taskIds.Contains(d.TaskId))
            .AsNoTracking()
            .ToListAsync();

        var hasDiscrepancies = discrepancies.Count > 0;
        var abandonPeriodicChain = session.IsPeriodic &&
            InventoryScheduleWindow.UtcCalendarDayIsAfterEndWindow(session.EndDate, DateTime.UtcNow);

        session.Status = hasDiscrepancies
            ? (int)InventorySessionStatus.PendingAccountant
            : (int)InventorySessionStatus.Confirmed;

        await _context.SaveChangesAsync();

        if (!hasDiscrepancies)
            await TryCreateNextPeriodicSessionIfApplicableAsync(session, abandonPeriodicChain);

        var displayStatus = GetDisplayStatus(session, DateTime.UtcNow);

        return new CompleteSessionResultDTO
        {
            Message = hasDiscrepancies
                ? "Phiên kiểm kê đã được hoàn thành."
                : "Phiên kiểm kê đã được hoàn thành. Không có chênh lệch — phiên đã xử lý.",
            ProgressPercent = session.ProgressPercent,
            CheckedTasks = checkedTasks,
            TotalTasks = totalTasks,
            NewStatus = displayStatus,
            StatusName = GetSessionStatusName(displayStatus),
            HasDiscrepancies = hasDiscrepancies,
            QuantityDiffCount = discrepancies.Count(d =>
                (d.DiscrepancyType & (int)DiscrepancyType.AssetNotFound) != 0 ||
                (d.DiscrepancyType & (int)DiscrepancyType.QuantityMismatch) != 0),
            LocationChangeCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.LocationMismatch) != 0),
            DepartmentChangeCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.LocationMismatch) != 0),
            ConditionChangeCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.ConditionMismatch) != 0)
        };
    }

    public async Task<InventoryReviewSummaryDTO> GetReviewSummaryAsync(int userId, int sessionId)
    {
        await EnsureCreatorOrDirectorReviewAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions
            .Include(s => s.Department)
            .Include(s => s.AssetCategory)
            .Include(s => s.AssetType)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        var discrepancies = await _context.InventoryDiscrepancies
            .Where(d => d.Task.SessionId == sessionId)
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

        var displayStatus = GetDisplayStatus(session, DateTime.UtcNow);

        return new InventoryReviewSummaryDTO
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
            IsPeriodic = session.IsPeriodic,
            PeriodDays = session.PeriodDays,
            TotalDiscrepancies = detailList.Count,
            AssetNotFoundCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.AssetNotFound) != 0),
            QuantityMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.QuantityMismatch) != 0),
            LocationMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.LocationMismatch) != 0),
            UserMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.UserMismatch) != 0),
            ValueMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.ValueMismatch) != 0),
            ConditionMismatchCount = discrepancies.Count(d => (d.DiscrepancyType & (int)DiscrepancyType.ConditionMismatch) != 0),
            Discrepancies = detailList
        };
    }

    public async Task<DirectorApproveResultDTO> DirectorApproveSessionAsync(int userId, int sessionId, ReviewInventorySessionDTO dto)
    {
        await EnsureCreatorOrDirectorModerateAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.AssetInstance)
                    .ThenInclude(ai => ai.Asset)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryRecords)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryDiscrepancies)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.Completed &&
            session.Status != (int)InventorySessionStatus.PendingAccountant)
            throw new InvalidOperationException("Chỉ có thể xác nhận khi phiên đang chờ xử lý sau kiểm kê.");

        const int bookQtyPerInstance = 1;
        var hasMismatch = false;
        foreach (var task in session.InventoryTasks.Where(t =>
                     !IsExcludedFromInventoryExecution(t.AssetInstance.Status)))
        {
            var record = task.InventoryRecords.FirstOrDefault();
            if (record == null) { hasMismatch = true; break; }
            if (record.IsFound == false) { hasMismatch = true; break; }

            var actualQty = record.ActualQuantity ?? (record.IsFound == true ? bookQtyPerInstance : 0);
            if (actualQty != bookQtyPerInstance) { hasMismatch = true; break; }
        }

        if (!hasMismatch)
        {
            hasMismatch = session.InventoryTasks
                .Where(t => !IsExcludedFromInventoryExecution(t.AssetInstance.Status))
                .Any(t => t.InventoryDiscrepancies.Any(d => d.ResolvedAt == null));
        }

        var utcNowApprove = DateTime.UtcNow;
        var abandonPeriodicChain = session.IsPeriodic &&
            (session.Status == (int)InventorySessionStatus.PendingAccountant ||
             session.Status == (int)InventorySessionStatus.Completed) &&
            InventoryScheduleWindow.UtcCalendarDayIsAfterEndWindow(session.EndDate, utcNowApprove);

        session.Status = hasMismatch
            ? (int)InventorySessionStatus.PendingAccountant
            : (int)InventorySessionStatus.Confirmed;

        await _context.SaveChangesAsync();

        if (!hasMismatch)
            await TryCreateNextPeriodicSessionIfApplicableAsync(session, abandonPeriodicChain);

        await SafeNotifyAsync(
            () => _inventoryNotifications.NotifyAfterDirectorApprovalAsync(session, hasMismatch),
            "director-approve → heads/accountants");

        var displayStatus = GetDisplayStatus(session, DateTime.UtcNow);

        return new DirectorApproveResultDTO
        {
            Message = hasMismatch
                ? "Đã xác nhận. Có chênh lệch so với sổ — phiên chuyển sang Chờ xử lý (trưởng phòng xử lý trên sổ)."
                : "Đã xác nhận. Không có chênh lệch so với sổ — phiên đã xử lý.",
            NewStatus = displayStatus,
            StatusName = GetSessionStatusName(displayStatus),
            HasQuantityOrUserDiscrepancy = hasMismatch
        };
    }

    public async Task RequestInventoryRecheckAsync(int userId, int sessionId, ReviewInventorySessionDTO dto)
    {
        await EnsureCreatorOrDirectorModerateAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryRecords)
            .Include(s => s.InventoryTasks)
                .ThenInclude(t => t.InventoryDiscrepancies)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.Completed &&
            session.Status != (int)InventorySessionStatus.PendingAccountant)
            throw new InvalidOperationException("Chỉ có thể yêu cầu kiểm kê lại khi phiên đang chờ xử lý sau kiểm kê.");

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

        await SafeNotifyAsync(
            () => _inventoryNotifications.NotifyDepartmentHeadsRecheckRequestedAsync(session),
            "reject-recheck → heads");
    }

    public async Task DepartmentHeadFinishInventoryResolutionAsync(int userId, int sessionId, ReviewInventorySessionDTO dto)
    {
        await EnsureDepartmentHeadOrAdminAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions.FindAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.PendingAccountant)
            throw new InvalidOperationException("Chỉ có thể hoàn tất khi phiên đang ở trạng thái Chờ xử lý.");

        var unresolved = await _context.InventoryDiscrepancies
            .CountAsync(d => d.Task.SessionId == sessionId && d.ResolvedAt == null);
        if (unresolved > 0)
            throw new InvalidOperationException("Còn chênh lệch chưa cập nhật lên sổ. Vui lòng xử lý hết trước khi hoàn tất.");

        var utcNowFinish = DateTime.UtcNow;
        var abandonPeriodicChain = session.IsPeriodic &&
            InventoryScheduleWindow.UtcCalendarDayIsAfterEndWindow(session.EndDate, utcNowFinish);

        session.Status = (int)InventorySessionStatus.Confirmed;

        await _context.SaveChangesAsync();

        await TryCreateNextPeriodicSessionIfApplicableAsync(session, abandonPeriodicChain);
    }

    public async Task AccountantApplyDiscrepancyActualAsync(int userId, int sessionId, int discrepancyId)
    {
        await EnsureDepartmentHeadOrAdminAccessAsync(userId, sessionId);

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
            throw new KeyNotFoundException("Không tìm thấy chênh lệch trong phiên này.");

        var session = discrepancy.Task.Session;
        if (session.Status != (int)InventorySessionStatus.PendingAccountant)
            throw new InvalidOperationException("Chỉ có thể cập nhật sổ khi phiên đang ở trạng thái Chờ xử lý.");

        if (discrepancy.ResolvedAt.HasValue)
            throw new InvalidOperationException("Chênh lệch này đã được cập nhật lên sổ trước đó.");

        var record = discrepancy.Task.InventoryRecords
            .OrderByDescending(r => r.RecordId)
            .FirstOrDefault();
        if (record == null)
            throw new InvalidOperationException("Không có bản ghi kiểm kê cho nhiệm vụ này.");

        var inst = discrepancy.Task.AssetInstance;
        var effective = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!Enum.TryParse<AssetStatus>(record.ActualCondition, true, out var newStatus))
        {
            if (record.IsFound == false)
                newStatus = AssetStatus.Lost;
            else
                throw new InvalidOperationException("Không đọc được tình trạng thực tế (ActualCondition) từ bản ghi kiểm kê.");
        }

        inst.Status = (int)newStatus;

        if ((discrepancy.DiscrepancyType & (int)DiscrepancyType.ValueMismatch) != 0)
            inst.CurrentValue = discrepancy.ActualValue;

        var targetLocation = await _context.AssetLocations
            .FirstOrDefaultAsync(al => al.LocationId == record.ActualLocationId);
        if (targetLocation == null || targetLocation.AssetInstanceId != inst.AssetInstanceId)
            throw new InvalidOperationException("Vị trí thực tế không hợp lệ cho thể hiện tài sản này.");

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
                throw new InvalidOperationException("Không tìm thấy nhân viên gắn với người dùng được ghi nhận khi kiểm kê.");

            if (employee.DepartmentId != targetLocation.DepartmentId)
                throw new InvalidOperationException("Phòng ban của nhân viên phụ trách phải trùng với phòng ban vị trí thực tế.");

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
    }

    public async Task UpdateSessionAsync(int userId, int sessionId, UpdateInventorySessionDTO dto)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions.FindAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.Scheduled)
            throw new InvalidOperationException("Chỉ có thể chỉnh sửa phiên kiểm kê ở trạng thái 'Đã lên lịch'.");

        if (dto.EndDate <= dto.StartDate)
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");

        var effectivePeriodDays = session.IsPeriodic
            ? (dto.PeriodDays is int pd && pd > 0 ? pd : session.PeriodDays)
            : null;
        var targetDepartmentId = dto.DepartmentId ?? session.DepartmentId;
        var scheduleError = await ValidateInventoryScheduleAgainstDepartmentAsync(
            targetDepartmentId,
            dto.StartDate,
            dto.EndDate,
            session.IsPeriodic,
            effectivePeriodDays,
            excludeSessionId: sessionId);
        if (scheduleError != null)
            throw new InvalidOperationException(scheduleError);

        if (targetDepartmentId != session.DepartmentId)
        {
            var department = await _context.Departments.FindAsync(targetDepartmentId);
            if (department == null)
                throw new InvalidOperationException("Phòng ban không tồn tại.");

            var existingTasks = await _context.InventoryTasks
                .Include(t => t.InventoryRecords)
                .Include(t => t.InventoryDiscrepancies)
                .Where(t => t.SessionId == sessionId)
                .ToListAsync();

            foreach (var t in existingTasks)
            {
                if (t.Status != (int)InventoryTaskStatus.Pending
                    || t.InventoryRecords.Count > 0
                    || t.InventoryDiscrepancies.Count > 0)
                    throw new InvalidOperationException("Không thể đổi phòng ban khi phiên đã có kết quả kiểm kê trên một số nhiệm vụ.");
            }

            var instances = await _context.AssetInstances
                .Where(ai =>
                    ai.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == targetDepartmentId) &&
                    ai.Status == (int)AssetStatus.InUse)
                .AsNoTracking()
                .ToListAsync();

            if (!instances.Any())
                throw new InvalidOperationException("Không có tài sản hợp lệ nào trong phòng ban này.");

            _context.InventoryTasks.RemoveRange(existingTasks);

            foreach (var inst in instances)
            {
                _context.InventoryTasks.Add(new InventoryTask
                {
                    SessionId = sessionId,
                    AssetInstanceId = inst.AssetInstanceId,
                    AssignedUserId = session.CreatedBy,
                    DepartmentId = targetDepartmentId,
                    Status = (int)InventoryTaskStatus.Pending,
                    CheckDate = dto.EndDate
                });
            }

            session.DepartmentId = targetDepartmentId;
            session.ProgressPercent = 0;
        }

        session.Purpose = dto.Purpose ?? string.Empty;
        session.StartDate = dto.StartDate;
        session.EndDate = dto.EndDate;

        if (session.IsPeriodic && dto.PeriodDays.HasValue && dto.PeriodDays.Value > 0)
            session.PeriodDays = dto.PeriodDays.Value;

        await _context.SaveChangesAsync();
    }

    public async Task ActivateSessionAsync(int userId, int sessionId)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions.FindAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.Scheduled)
            throw new InvalidOperationException("Chỉ có thể kích hoạt phiên kiểm kê ở trạng thái Đã lên lịch.");

        var now = DateTime.UtcNow;
        if (!InventoryScheduleWindow.UtcCalendarDayInInclusiveRange(session.StartDate, session.EndDate, now))
            throw new InvalidOperationException("Chỉ có thể bắt đầu khi đã đến khung lịch (trạng thái hiển thị \"Đến lịch\").");

        session.Status = (int)InventorySessionStatus.InProgress;

        await _context.SaveChangesAsync();
    }

    public async Task<CancelSessionResultDTO> CancelSessionAsync(int userId, int sessionId, ReviewInventorySessionDTO dto)
    {
        await EnsureCreatorAccessAsync(userId, sessionId);

        var session = await _context.InventorySessions
            .Include(s => s.InventoryTasks)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (session.Status != (int)InventorySessionStatus.Scheduled &&
            session.Status != (int)InventorySessionStatus.InProgress)
            throw new InvalidOperationException("Chỉ có thể hủy phiên ở trạng thái Đã lên lịch hoặc Đang thực hiện.");

        var wasScheduled = session.Status == (int)InventorySessionStatus.Scheduled;
        var overdueOpen = session.IsPeriodic &&
            session.Status == (int)InventorySessionStatus.InProgress &&
            InventoryScheduleWindow.UtcCalendarDayIsAfterEndWindow(session.EndDate, DateTime.UtcNow);

        session.Status = (int)InventorySessionStatus.Cancelled;

        int? notifyUserId = dto.ReviewedBy > 0 ? dto.ReviewedBy : userId;
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

        int cancelledChainCount = 0;
        if (session.IsPeriodic && (wasScheduled || overdueOpen))
        {
            var futurePeriodicSessions = await _context.InventorySessions
                .Where(s =>
                    s.SessionId != sessionId &&
                    s.DepartmentId == session.DepartmentId &&
                    s.IsPeriodic &&
                    s.Status == (int)InventorySessionStatus.Scheduled &&
                    s.CreatedBy == session.CreatedBy &&
                    s.PeriodDays == session.PeriodDays &&
                    s.Purpose == session.Purpose &&
                    s.AssetCategoryId == session.AssetCategoryId &&
                    s.AssetTypeId == session.AssetTypeId &&
                    s.StartDate > session.StartDate)
                .ToListAsync();

            foreach (var future in futurePeriodicSessions)
                future.Status = (int)InventorySessionStatus.Cancelled;

            cancelledChainCount = futurePeriodicSessions.Count;
        }

        await _context.SaveChangesAsync();

        return new CancelSessionResultDTO
        {
            Message = cancelledChainCount > 0
                ? $"Phiên kiểm kê đã được hủy. Đã dừng {cancelledChainCount} lịch định kỳ tiếp theo."
                : "Phiên kiểm kê đã được hủy.",
            SessionId = sessionId,
            ReviewNotes = dto.ReviewNotes,
            CancelledChainCount = cancelledChainCount
        };
    }

    public async Task<IEnumerable<InventoryDiscrepancyDTO>> GetDiscrepanciesAsync(int userId, int sessionId)
    {
        await EnsureCreatorOrDirectorReviewAccessAsync(userId, sessionId);

        var sessionExists = await _context.InventorySessions.AnyAsync(s => s.SessionId == sessionId);
        if (!sessionExists) throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        var discrepancies = await _context.InventoryDiscrepancies
            .Where(d => d.Task.SessionId == sessionId)
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
        return discrepancies.Select(d =>
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
    }

    public async Task<IEnumerable<DropdownItemDTO>> GetDepartmentsAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DropdownItemDTO { Id = d.DepartmentId, Name = d.Name })
            .ToListAsync();
    }

    public async Task<IEnumerable<DropdownItemDTO>> GetAssetCategoriesAsync()
    {
        return await _context.AssetCategories
            .AsNoTracking()
            .Select(c => new DropdownItemDTO { Id = c.CategoryId, Name = c.Name })
            .ToListAsync();
    }

    public async Task<IEnumerable<DropdownItemDTO>> GetAssetTypesAsync(int? categoryId)
    {
        var query = _context.AssetTypes.AsNoTracking().AsQueryable();
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        return await query
            .Select(t => new DropdownItemDTO { Id = t.AssetTypeId, Name = t.Name })
            .ToListAsync();
    }

    public async Task<IEnumerable<DropdownItemDTO>> GetUsersAsync()
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.UserId != null)
            .Select(e => new DropdownItemDTO { Id = e.UserId!.Value, Name = e.Name })
            .ToListAsync();
    }

    // ── Access control ────────────────────────────────────────────────────────

    private async Task EnsureCreatorAccessAsync(int userId, int sessionId)
    {
        var createdBy = await _context.InventorySessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .Select(s => (int?)s.CreatedBy)
            .FirstOrDefaultAsync();

        if (!createdBy.HasValue)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (createdBy.Value != userId)
            throw new UnauthorizedAccessException();
    }

    private async Task EnsureCreatorOrDirectorReviewAccessAsync(int userId, int sessionId)
    {
        var row = await _context.InventorySessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .Select(s => new { s.CreatedBy, s.Status })
            .FirstOrDefaultAsync();

        if (row == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (row.CreatedBy == userId) return;

        if (await UserIsAccountantAsync(userId))
        {
            if (row.Status != (int)InventorySessionStatus.Completed
                && row.Status != (int)InventorySessionStatus.Confirmed
                && row.Status != (int)InventorySessionStatus.PendingAccountant)
                throw new UnauthorizedAccessException();
            return;
        }

        if (!await UserIsDirectorAsync(userId))
            throw new UnauthorizedAccessException();

        if (row.Status != (int)InventorySessionStatus.Completed
            && row.Status != (int)InventorySessionStatus.Confirmed
            && row.Status != (int)InventorySessionStatus.PendingAccountant)
            throw new UnauthorizedAccessException();
    }

    private async Task EnsureCreatorOrDirectorModerateAccessAsync(int userId, int sessionId)
    {
        var createdBy = await _context.InventorySessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .Select(s => (int?)s.CreatedBy)
            .FirstOrDefaultAsync();

        if (!createdBy.HasValue)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (createdBy.Value == userId) return;

        if (!await UserIsDirectorAsync(userId))
            throw new UnauthorizedAccessException();
    }

    private async Task EnsureDepartmentHeadOrAdminAccessAsync(int userId, int sessionId)
    {
        var roleCodes = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();

        if (roleCodes.Any(IsAdminRoleCode) || roleCodes.Any(IsAccountantRoleCode))
            return;

        var sessionRow = await _context.InventorySessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .Select(s => new { s.DepartmentId, s.CreatedBy })
            .FirstOrDefaultAsync();

        if (sessionRow == null)
            throw new KeyNotFoundException("Phiên kiểm kê không tồn tại.");

        if (sessionRow.CreatedBy == userId)
            return;

        if (!roleCodes.Any(IsDepartmentHeadRole))
            throw new UnauthorizedAccessException();

        var userDeptId = await _context.Employees
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync();

        if (!userDeptId.HasValue || userDeptId.Value != sessionRow.DepartmentId)
            throw new UnauthorizedAccessException();
    }

    private async Task<bool> UserIsDirectorOrAdminAsync(int userId)
    {
        var codes = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();
        return codes.Any(c => IsDirectorRoleCode(c) || IsAdminRoleCode(c));
    }

    private async Task<bool> UserIsDirectorAsync(int userId)
    {
        var codes = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();
        return codes.Any(IsDirectorRoleCode);
    }

    private async Task<bool> UserIsAccountantAsync(int userId)
    {
        var codes = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Code)
            .ToListAsync();
        return codes.Any(IsAccountantRoleCode);
    }

    // ── Core session creation ─────────────────────────────────────────────────

    private sealed record CreateInventorySessionResult(bool Success, string? ErrorMessage, int? SessionId);

    private async Task<CreateInventorySessionResult> CreateInventorySessionCoreAsync(
        string purpose,
        DateTime startDate,
        DateTime endDate,
        int departmentId,
        int createdBy,
        bool isPeriodic,
        int? periodDays,
        int? assetCategoryId,
        int? assetTypeId)
    {
        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
            return new CreateInventorySessionResult(false, "Phòng ban không tồn tại.", null);

        var scheduleError = await ValidateInventoryScheduleAgainstDepartmentAsync(
            departmentId, startDate, endDate, isPeriodic,
            isPeriodic ? periodDays : null, excludeSessionId: null);
        if (scheduleError != null)
            return new CreateInventorySessionResult(false, scheduleError, null);

        var instances = await _context.AssetInstances
            .Where(ai =>
                ai.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == departmentId) &&
                ai.Status == (int)AssetStatus.InUse)
            .AsNoTracking()
            .ToListAsync();

        if (!instances.Any())
            return new CreateInventorySessionResult(
                false,
                "Không có tài sản nào đang được sử dụng trong phòng ban này.",
                null);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var code = await GenerateSessionCode();
            var session = new InventorySession
            {
                Code = code,
                Purpose = purpose,
                StartDate = startDate,
                EndDate = endDate,
                DepartmentId = departmentId,
                AssetCategoryId = assetCategoryId,
                AssetTypeId = assetTypeId,
                Status = (int)InventorySessionStatus.Scheduled,
                ProgressPercent = 0,
                CreatedBy = createdBy,
                CreateDate = DateTime.UtcNow,
                IsPeriodic = isPeriodic,
                PeriodDays = isPeriodic ? periodDays : null
            };

            foreach (var inst in instances)
            {
                session.InventoryTasks.Add(new InventoryTask
                {
                    AssetInstanceId = inst.AssetInstanceId,
                    AssignedUserId = createdBy,
                    DepartmentId = departmentId,
                    Status = (int)InventoryTaskStatus.Pending,
                    CheckDate = endDate
                });
            }

            _context.InventorySessions.Add(session);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CreateInventorySessionResult(true, null, session.SessionId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task TryCreateNextPeriodicSessionIfApplicableAsync(
        InventorySession closedSession,
        bool abandonPeriodicChain)
    {
        if (abandonPeriodicChain)
            return;

        if (!closedSession.IsPeriodic || closedSession.PeriodDays is not int periodDays || periodDays <= 0)
            return;

        var utcNow = DateTime.UtcNow;
        var executionLength = closedSession.EndDate - closedSession.StartDate;
        if (executionLength <= TimeSpan.Zero)
            executionLength = TimeSpan.FromDays(1);

        var nextStart = closedSession.StartDate.AddDays(periodDays);
        while (nextStart < utcNow)
            nextStart = nextStart.AddDays(periodDays);

        var nextEnd = nextStart + executionLength;

        var created = await CreateInventorySessionCoreAsync(
            closedSession.Purpose ?? string.Empty,
            nextStart,
            nextEnd,
            closedSession.DepartmentId,
            closedSession.CreatedBy,
            isPeriodic: true,
            periodDays,
            closedSession.AssetCategoryId,
            closedSession.AssetTypeId);

        if (!created.Success)
        {
            _logger.LogWarning(
                "Could not auto-schedule next periodic inventory after session {SessionId}: {Reason}",
                closedSession.SessionId,
                created.ErrorMessage);
            return;
        }

        _logger.LogInformation(
            "Auto-scheduled next periodic inventory session {NewSessionId} after confirmed session {ClosedSessionId}.",
            created.SessionId,
            closedSession.SessionId);
    }

    private async Task<string> GenerateSessionCode()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"KK-{today}-";
        var count = await _context.InventorySessions
            .CountAsync(s => s.Code.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }

    // ── Schedule validation ───────────────────────────────────────────────────

    private async Task<string?> ValidateInventoryScheduleAgainstDepartmentAsync(
        int departmentId,
        DateTime startDate,
        DateTime endDate,
        bool isPeriodic,
        int? periodDays,
        int? excludeSessionId,
        CancellationToken cancellationToken = default)
    {
        var recurrenceDays = 0;
        if (isPeriodic)
        {
            if (periodDays is not int p || p <= 0)
                return "Vui lòng nhập chu kỳ kiểm kê (số ngày giữa các lần, tính từ ngày bắt đầu) lớn hơn 0 cho lịch định kỳ.";
            recurrenceDays = p;
        }

        var span = InventorySessionExecutionSpan(startDate, endDate);

        if (isPeriodic)
        {
            var firstRepeatStart = startDate.AddDays(recurrenceDays);
            var firstRepeatEnd = firstRepeatStart + span;
            if (InventoryScheduleWindow.CalendarRangesOverlap(startDate, endDate, firstRepeatStart, firstRepeatEnd))
                return "Chu kỳ kiểm kê quá ngắn so với thời gian thực hiện: các lần lặp theo chu kỳ sẽ trùng hoặc gối nhau. Vui lòng tăng chu kỳ hoặc rút ngắn thời gian thực hiện.";
        }

        var utcNow = DateTime.UtcNow;
        var horizonEndUtc = utcNow.AddYears(6);
        var fromStartHorizon = startDate.AddYears(6);
        if (fromStartHorizon > horizonEndUtc)
            horizonEndUtc = fromStartHorizon;

        var blockingWindows = await BuildDepartmentOpenInventoryBlockingWindowsAsync(
            departmentId, excludeSessionId, horizonEndUtc, cancellationToken);

        var maxK = !isPeriodic ? 0 : Math.Min(2000, Math.Max(1, (int)Math.Ceiling(5.0 * 365 / recurrenceDays)) + 10);

        for (var k = 0; k <= maxK; k++)
        {
            var ws = startDate.AddDays(isPeriodic ? k * recurrenceDays : 0);
            if (ws > horizonEndUtc) break;

            var we = ws + span;
            foreach (var block in blockingWindows)
            {
                if (!InventoryScheduleWindow.CalendarRangesOverlap(ws, we, block.Start, block.End))
                    continue;

                if (!isPeriodic || k == 0)
                    return "Phòng ban này đã có phiên kiểm kê (chưa hủy, chưa hoàn tất xử lý) trùng hoặc gối khoảng thời gian được chọn — kể cả khi so với các lần định kỳ dự kiến của phiên khác. Vui lòng điều chỉnh ngày, thời gian thực hiện hoặc chu kỳ.";

                return $"Lịch định kỳ không khả thi: lần thứ {k + 1} trong chu kỳ (dự kiến) trùng hoặc gối một phiên kiểm kê hiện có hoặc một lần lặp định kỳ khác trong phòng ban. Vui lòng điều chỉnh ngày bắt đầu, thời gian thực hiện hoặc chu kỳ.";
            }
        }

        return null;
    }

    private async Task<List<(DateTime Start, DateTime End)>> BuildDepartmentOpenInventoryBlockingWindowsAsync(
        int departmentId,
        int? excludeSessionId,
        DateTime horizonEndUtc,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var rows = await _context.InventorySessions
            .AsNoTracking()
            .Where(s =>
                s.DepartmentId == departmentId &&
                s.Status != (int)InventorySessionStatus.Cancelled &&
                s.Status != (int)InventorySessionStatus.Confirmed)
            .Select(s => new { s.SessionId, s.Status, s.StartDate, s.EndDate, s.IsPeriodic, s.PeriodDays })
            .ToListAsync(cancellationToken);

        var windows = new List<(DateTime Start, DateTime End)>(rows.Count * 4);

        foreach (var s in rows)
        {
            if (excludeSessionId.HasValue && s.SessionId == excludeSessionId.Value)
                continue;

            // Periodic + same "Quá hạn" rule as list UI: do not block scheduling (or future repeats).
            if (s.IsPeriodic &&
                (s.Status == (int)InventorySessionStatus.Scheduled || s.Status == (int)InventorySessionStatus.InProgress) &&
                InventoryScheduleWindow.UtcCalendarDayIsAfterEndWindow(s.EndDate, utcNow))
                continue;

            windows.Add((s.StartDate, s.EndDate));

            if (!s.IsPeriodic || s.PeriodDays is not int p || p <= 0)
                continue;

            var span = InventorySessionExecutionSpan(s.StartDate, s.EndDate);
            const int maxRepeats = 2000;
            for (var n = 1; n <= maxRepeats; n++)
            {
                var vs = s.StartDate.AddDays(n * p);
                if (vs > horizonEndUtc) break;
                windows.Add((vs, vs + span));
            }
        }

        return windows;
    }

    // ── Asset location / usage helpers ────────────────────────────────────────

    private async Task CloseCurrentAssetLocationsExceptAsync(int assetInstanceId, int exceptLocationId, DateOnly newStartDate)
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

    private async Task UpdateSessionProgressPercentAsync(InventorySession session)
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

    // ── User display name helpers ─────────────────────────────────────────────

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

    // ── Notification helper ───────────────────────────────────────────────────

    private async Task SafeNotifyAsync(Func<Task> notify, string step)
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

    // ── Static helpers ────────────────────────────────────────────────────────

    private static TimeSpan InventorySessionExecutionSpan(DateTime startDate, DateTime endDate)
    {
        var span = endDate - startDate;
        return span <= TimeSpan.Zero ? TimeSpan.FromDays(1) : span;
    }

    private static int GetDisplayStatus(InventorySession session, DateTime now)
    {
        if (session.Status == (int)InventorySessionStatus.Cancelled)
            return (int)InventorySessionStatus.Cancelled;

        if ((session.Status == (int)InventorySessionStatus.Scheduled || session.Status == (int)InventorySessionStatus.InProgress)
            && InventoryScheduleWindow.UtcCalendarDayIsAfterEndWindow(session.EndDate, now))
            return 7;

        if (session.Status == (int)InventorySessionStatus.Scheduled
            && InventoryScheduleWindow.UtcCalendarDayInInclusiveRange(session.StartDate, session.EndDate, now))
            return 5;

        return session.Status;
    }

    private static string GetSessionStatusName(int status) => status switch
    {
        0 => "Đã lên lịch",
        1 => "Đang thực hiện",
        2 => "Chờ xử lý",
        3 => "Đã hủy",
        4 => "Đã xử lý",
        5 => "Đến lịch",
        6 => "Chờ xử lý",
        7 => "Quá hạn",
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

    private static bool BookImpliesInUse(int status) => (AssetStatus)status switch
    {
        AssetStatus.Available => false,
        AssetStatus.Disposed => false,
        AssetStatus.Lost => false,
        AssetStatus.Liquidated => false,
        _ => true
    };

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

    private static bool IsExcludedFromInventoryExecution(int instanceStatus) =>
        instanceStatus != (int)AssetStatus.InUse;

    private static string? ResolveDisplayName(IReadOnlyDictionary<int, string> map, int? userId) =>
        userId.HasValue && map.TryGetValue(userId.Value, out var name) ? name : null;

    private static string TruncateNotificationContent(string content) =>
        content.Length > 500 ? content[..497] + "..." : content;

    private static bool IsDirectorRoleCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim().ToLowerInvariant().Replace(' ', '_');
        return c is "director" or "giám_đốc" or "giam_doc";
    }

    private static bool IsAdminRoleCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return code.Trim().ToLowerInvariant().Replace(' ', '_') is "admin";
    }

    private static bool IsDepartmentHeadRole(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim().ToLowerInvariant().Replace(' ', '_');
        return c is "department_head" or "departmenthead" or "dept_head"
            or "trưởng_phòng" or "truong_phong";
    }

    private static bool IsAccountantRoleCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        return c is "accountant" or "kế_toán" or "ke_toan" or "ketoan"
            || c.Contains("accountant", StringComparison.Ordinal)
            || c.Contains("ketoan", StringComparison.Ordinal);
    }
}
