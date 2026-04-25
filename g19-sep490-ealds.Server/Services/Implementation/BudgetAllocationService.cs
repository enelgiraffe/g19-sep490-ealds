using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.BudgetAllocation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class BudgetAllocationService : IBudgetAllocationService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<BudgetAllocationService> _logger;

    public BudgetAllocationService(EaldsDbContext context, ILogger<BudgetAllocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<BudgetAllocationListItemDto>> GetListAsync(int? departmentId, string? status)
    {
        var query = _context.BudgetAllocations.AsNoTracking().AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(x => x.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            var st = ParseStatusFilter(status);
            if (st == null)
                throw new InvalidOperationException("status must be pending, allocated, recalled, or all.");
            query = query.Where(x => x.Status == st.Value);
        }

        return await query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.BudgetAllocationId)
            .Select(x => new BudgetAllocationListItemDto
            {
                Id = x.BudgetAllocationId,
                AssetInstanceId = x.AssetInstanceId,
                Name = x.AssetInstance.Asset.Name + " — " + x.AssetInstance.InstanceCode,
                Category = x.AssetCategory.Name,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department.Name,
                Date = x.TransactionDate.ToString("yyyy-MM-dd"),
                Status = StatusToApiString(x.Status),
                SubmittedBy = x.SubmittedByDisplayName,
                Note = x.Note
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<AssetInstanceOptionDto>> GetAssetInstanceOptionsAsync(
        int categoryId, int departmentId, string mode, string? search)
    {
        if (!await _context.AssetCategories.AsNoTracking().AnyAsync(c => c.CategoryId == categoryId))
            throw new InvalidOperationException($"Asset category {categoryId} was not found.");

        if (!await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == departmentId))
            throw new InvalidOperationException($"Department {departmentId} was not found.");

        var m = (mode ?? "").Trim().ToLowerInvariant();
        if (m is not ("assign" or "recall"))
            throw new InvalidOperationException("mode must be assign or recall.");

        var query = _context.AssetInstances
            .AsNoTracking()
            .Where(i => i.Asset != null && i.Asset.AssetType.CategoryId == categoryId);

        if (m == "assign")
            query = query.Where(i => !i.AssetLocations.Any(al => al.IsCurrent));
        else
            query = query.Where(i =>
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == departmentId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(i =>
                i.InstanceCode.ToLower().Contains(kw) ||
                (i.Asset != null && i.Asset.Name.ToLower().Contains(kw)) ||
                (i.Asset != null && i.Asset.Code.ToLower().Contains(kw)));
        }

        return await query
            .OrderBy(i => i.Asset!.Name)
            .ThenBy(i => i.InstanceCode)
            .Select(i => new AssetInstanceOptionDto
            {
                AssetInstanceId = i.AssetInstanceId,
                Label = i.Asset!.Name + " — " + i.InstanceCode + " (" + i.Asset.Code + ")"
            })
            .Take(100)
            .ToListAsync();
    }

    public async Task<BudgetAllocationListItemDto> CreateAsync(int userId, CreateBudgetAllocationDto dto)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            throw new UnauthorizedAccessException();

        var submitterDisplay = await _context.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.EmployeeId)
            .Select(e => e.Name)
            .FirstOrDefaultAsync() ?? user.Email;

        if (!await _context.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId))
            throw new InvalidOperationException($"Department {dto.DepartmentId} was not found.");

        if (!await _context.AssetCategories.AnyAsync(c => c.CategoryId == dto.AssetCategoryId))
            throw new InvalidOperationException($"Asset category {dto.AssetCategoryId} was not found.");

        var instance = await _context.AssetInstances
            .Include(i => i.Asset!).ThenInclude(a => a.AssetType)
            .FirstOrDefaultAsync(i => i.AssetInstanceId == dto.AssetInstanceId);

        if (instance?.Asset == null)
            throw new InvalidOperationException($"Asset instance {dto.AssetInstanceId} was not found.");

        if (instance.Asset.AssetType.CategoryId != dto.AssetCategoryId)
            throw new InvalidOperationException("Selected asset does not belong to the chosen category.");

        var effective = DateOnly.FromDateTime(dto.TransactionDate?.Date ?? DateTime.UtcNow.Date);

        var hasCurrentAssignment = await _context.AssetLocations.AnyAsync(al =>
            al.AssetInstanceId == dto.AssetInstanceId && al.IsCurrent);

        var inTargetDept = await _context.AssetLocations.AnyAsync(al =>
            al.AssetInstanceId == dto.AssetInstanceId &&
            al.IsCurrent &&
            al.DepartmentId == dto.DepartmentId);

        if (dto.IsRecall)
        {
            if (!inTargetDept)
                throw new InvalidOperationException(
                    "This asset is not currently assigned to the selected department.");
            await CloseCurrentLocationAsync(instance.AssetInstanceId, effective);
        }
        else
        {
            if (hasCurrentAssignment)
                throw new InvalidOperationException(
                    "This asset is already assigned to a department. Recall it from that department first before assigning it again.");
            await CloseCurrentLocationAsync(instance.AssetInstanceId, effective);
            _context.AssetLocations.Add(new AssetLocation
            {
                AssetInstanceId = instance.AssetInstanceId,
                DepartmentId = dto.DepartmentId,
                StartDate = effective,
                EndDate = null,
                IsCurrent = true
            });
        }

        var entity = new BudgetAllocation
        {
            DepartmentId = dto.DepartmentId,
            AssetInstanceId = instance.AssetInstanceId,
            AssetCategoryId = dto.AssetCategoryId,
            SubmittedByUserId = userId,
            SubmittedByDisplayName = submitterDisplay,
            TransactionDate = dto.TransactionDate?.Date ?? DateTime.UtcNow.Date,
            Status = dto.IsRecall ? BudgetAllocationStatus.Recalled : BudgetAllocationStatus.Allocated,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
        };

        _context.BudgetAllocations.Add(entity);
        await _context.SaveChangesAsync();

        return await _context.BudgetAllocations.AsNoTracking()
            .Where(x => x.BudgetAllocationId == entity.BudgetAllocationId)
            .Select(x => new BudgetAllocationListItemDto
            {
                Id = x.BudgetAllocationId,
                AssetInstanceId = x.AssetInstanceId,
                Name = x.AssetInstance.Asset.Name + " — " + x.AssetInstance.InstanceCode,
                Category = x.AssetCategory.Name,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department.Name,
                Date = x.TransactionDate.ToString("yyyy-MM-dd"),
                Status = StatusToApiString(x.Status),
                SubmittedBy = x.SubmittedByDisplayName,
                Note = x.Note
            })
            .FirstAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.BudgetAllocations.FindAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Allocation {id} was not found.");

        _context.BudgetAllocations.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private async Task CloseCurrentLocationAsync(int assetInstanceId, DateOnly newStartDate)
    {
        var current = await _context.AssetLocations
            .Where(l => l.AssetInstanceId == assetInstanceId && l.IsCurrent)
            .FirstOrDefaultAsync();
        if (current != null)
        {
            current.IsCurrent = false;
            current.EndDate = newStartDate.AddDays(-1);
        }
    }

    private static BudgetAllocationStatus? ParseStatusFilter(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "pending" => BudgetAllocationStatus.Pending,
            "allocated" => BudgetAllocationStatus.Allocated,
            "recalled" => BudgetAllocationStatus.Recalled,
            _ => null
        };

    private static string StatusToApiString(BudgetAllocationStatus s) => s switch
    {
        BudgetAllocationStatus.Pending => "pending",
        BudgetAllocationStatus.Allocated => "allocated",
        BudgetAllocationStatus.Recalled => "recalled",
        _ => "pending"
    };
}
