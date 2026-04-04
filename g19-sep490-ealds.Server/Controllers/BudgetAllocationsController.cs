using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.BudgetAllocation;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ACCOUNTANT")]
public class BudgetAllocationsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public BudgetAllocationsController(EaldsDbContext db) => _db = db;

    /// <summary>
    /// GET /api/BudgetAllocations — assignment / recall audit rows.
    /// Optional filters: departmentId, status (allocated | recalled | pending | all).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BudgetAllocationListItemDto>>> GetList(
        [FromQuery] int? departmentId,
        [FromQuery] string? status)
    {
        var query = _db.BudgetAllocations.AsNoTracking().AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(x => x.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            var st = ParseStatusFilter(status);
            if (st == null)
                return BadRequest(new { message = "status must be pending, allocated, recalled, or all." });
            query = query.Where(x => x.Status == st.Value);
        }

        var list = await query
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

        return Ok(list);
    }

    /// <summary>
    /// GET /api/BudgetAllocations/asset-instance-options — searchable instances for the modal (by category + department + mode).
    /// mode assign: instances in category with no current department assignment.
    /// mode recall: instances in category currently assigned to the given department.
    /// </summary>
    [HttpGet("asset-instance-options")]
    public async Task<ActionResult<IEnumerable<AssetInstanceOptionDto>>> GetAssetInstanceOptions(
        [FromQuery] int categoryId,
        [FromQuery] int departmentId,
        [FromQuery] string mode,
        [FromQuery] string? search)
    {
        if (!await _db.AssetCategories.AsNoTracking().AnyAsync(c => c.CategoryId == categoryId))
            return BadRequest(new { message = $"Asset category {categoryId} was not found." });

        if (!await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == departmentId))
            return BadRequest(new { message = $"Department {departmentId} was not found." });

        var m = (mode ?? "").Trim().ToLowerInvariant();
        if (m is not ("assign" or "recall"))
            return BadRequest(new { message = "mode must be assign or recall." });

        var query = _db.AssetInstances
            .AsNoTracking()
            .Where(i => i.Asset != null && i.Asset.AssetType.CategoryId == categoryId);

        if (m == "assign")
        {
            // Only instances not currently assigned to any department.
            query = query.Where(i => !i.AssetLocations.Any(al => al.IsCurrent));
        }
        else
        {
            // Only instances currently assigned to the selected department (can be recalled from it).
            query = query.Where(i =>
                i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == departmentId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(i =>
                i.InstanceCode.ToLower().Contains(kw) ||
                (i.Asset != null && i.Asset.Name.ToLower().Contains(kw)) ||
                (i.Asset != null && i.Asset.Code.ToLower().Contains(kw)));
        }

        var list = await query
            .OrderBy(i => i.Asset!.Name)
            .ThenBy(i => i.InstanceCode)
            .Select(i => new AssetInstanceOptionDto
            {
                AssetInstanceId = i.AssetInstanceId,
                Label = i.Asset!.Name + " — " + i.InstanceCode + " (" + i.Asset.Code + ")"
            })
            .Take(100)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>POST /api/BudgetAllocations — assign instance to department or remove it from department.</summary>
    [HttpPost]
    public async Task<ActionResult<BudgetAllocationListItemDto>> Create([FromBody] CreateBudgetAllocationDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return Unauthorized();

        var submitterDisplay = await _db.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.EmployeeId)
            .Select(e => e.Name)
            .FirstOrDefaultAsync() ?? user.Email;

        if (!await _db.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId))
            return BadRequest(new { message = $"Department {dto.DepartmentId} was not found." });

        if (!await _db.AssetCategories.AnyAsync(c => c.CategoryId == dto.AssetCategoryId))
            return BadRequest(new { message = $"Asset category {dto.AssetCategoryId} was not found." });

        var instance = await _db.AssetInstances
            .Include(i => i.Asset!).ThenInclude(a => a.AssetType)
            .FirstOrDefaultAsync(i => i.AssetInstanceId == dto.AssetInstanceId);

        if (instance?.Asset == null)
            return BadRequest(new { message = $"Asset instance {dto.AssetInstanceId} was not found." });

        if (instance.Asset.AssetType.CategoryId != dto.AssetCategoryId)
            return BadRequest(new { message = "Selected asset does not belong to the chosen category." });

        var effective = DateOnly.FromDateTime(dto.TransactionDate?.Date ?? DateTime.UtcNow.Date);

        var hasCurrentDepartmentAssignment = await _db.AssetLocations.AnyAsync(al =>
            al.AssetInstanceId == dto.AssetInstanceId && al.IsCurrent);

        var inTargetDept = await _db.AssetLocations.AnyAsync(al =>
            al.AssetInstanceId == dto.AssetInstanceId &&
            al.IsCurrent &&
            al.DepartmentId == dto.DepartmentId);

        if (dto.IsRecall)
        {
            if (!inTargetDept)
                return BadRequest(new
                {
                    message = "This asset is not currently assigned to the selected department."
                });

            await CloseCurrentLocationAsync(instance.AssetInstanceId, effective);
        }
        else
        {
            if (hasCurrentDepartmentAssignment)
                return BadRequest(new
                {
                    message =
                        "This asset is already assigned to a department. Recall it from that department first before assigning it again."
                });

            await CloseCurrentLocationAsync(instance.AssetInstanceId, effective);
            _db.AssetLocations.Add(new AssetLocation
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

        _db.BudgetAllocations.Add(entity);
        await _db.SaveChangesAsync();

        var row = await _db.BudgetAllocations.AsNoTracking()
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

        return Created($"/api/BudgetAllocations/{row.Id}", row);
    }

    /// <summary>DELETE /api/BudgetAllocations/{id} — removes audit row only (does not change asset location).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.BudgetAllocations.FindAsync(id);
        if (entity == null)
            return NotFound(new { message = $"Allocation {id} was not found." });

        _db.BudgetAllocations.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task CloseCurrentLocationAsync(int assetInstanceId, DateOnly newStartDate)
    {
        var current = await _db.AssetLocations
            .Where(l => l.AssetInstanceId == assetInstanceId && l.IsCurrent)
            .FirstOrDefaultAsync();

        if (current != null)
        {
            current.IsCurrent = false;
            current.EndDate = newStartDate.AddDays(-1);
        }
    }

    private static BudgetAllocationStatus? ParseStatusFilter(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "pending" => BudgetAllocationStatus.Pending,
            "allocated" => BudgetAllocationStatus.Allocated,
            "recalled" => BudgetAllocationStatus.Recalled,
            _ => null
        };
    }

    private static string StatusToApiString(BudgetAllocationStatus s) => s switch
    {
        BudgetAllocationStatus.Pending => "pending",
        BudgetAllocationStatus.Allocated => "allocated",
        BudgetAllocationStatus.Recalled => "recalled",
        _ => "pending"
    };
}
