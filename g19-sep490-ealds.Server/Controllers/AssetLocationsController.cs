using g19_sep490_ealds.Server.DTOs.AssetLocation;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetLocationsController : ControllerBase
{
    private readonly EaldsDbContext _db;

    public AssetLocationsController(EaldsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/AssetLocations/departments - List departments as location dropdown options.
    /// Preserved for transfer dropdowns and other frontend consumers.
    /// </summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var list = await _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.DepartmentId)
            .Select(d => new
            {
                locationId = d.DepartmentId,
                displayName = d.Name
            })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>
    /// GET /api/AssetLocations/departments/{departmentId}/employees — Staff in a department (custodian dropdown).
    /// </summary>
    [HttpGet("departments/{departmentId:int}/employees")]
    [Authorize]
    public async Task<IActionResult> GetEmployeesForDepartment(int departmentId)
    {
        if (!await _db.Departments.AnyAsync(d => d.DepartmentId == departmentId))
            return NotFound(new { message = $"Department {departmentId} not found." });

        var list = await _db.Employees
            .AsNoTracking()
            .Where(e => e.DepartmentId == departmentId)
            .OrderBy(e => e.UserId == null)
            .ThenBy(e => e.UserId)
            .ThenBy(e => e.EmployeeId)
            .Select(e => new
            {
                employeeId = e.EmployeeId,
                name = e.Name,
                code = e.Code,
                userId = e.UserId
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// GET /api/AssetLocations - List all asset location records.
    /// Optional filters: assetInstanceId (physical row), assetId (catalog), departmentId, isCurrent.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetLocationResponseDto>>> GetAll(
        [FromQuery] int? assetInstanceId,
        [FromQuery] int? assetId,
        [FromQuery] int? departmentId,
        [FromQuery] bool? isCurrent)
    {
        var query = _db.AssetLocations
            .AsNoTracking()
            .AsQueryable();

        if (assetInstanceId.HasValue)
            query = query.Where(l => l.AssetInstanceId == assetInstanceId.Value);

        if (assetId.HasValue)
            query = query.Where(l => l.AssetInstance.AssetId == assetId.Value);

        if (departmentId.HasValue)
            query = query.Where(l => l.DepartmentId == departmentId.Value);

        if (isCurrent.HasValue)
            query = query.Where(l => l.IsCurrent == isCurrent.Value);

        var result = await query
            .OrderByDescending(l => l.IsCurrent)
            .ThenByDescending(l => l.StartDate)
            .Select(l => new AssetLocationResponseDto
            {
                LocationId = l.LocationId,
                AssetInstanceId = l.AssetInstanceId,
                AssetId = l.AssetInstance.AssetId,
                InstanceCode = l.AssetInstance.InstanceCode,
                AssetName = l.AssetInstance.Asset.Name,
                AssetCode = l.AssetInstance.Asset.Code,
                DepartmentId = l.DepartmentId,
                DepartmentName = l.Department.Name,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                IsCurrent = l.IsCurrent,
                Note = l.Note
            })
            .ToListAsync();

        return Ok(result);
    }

    /// <summary>
    /// GET /api/AssetLocations/{id} - Get a single asset location record by LocationId.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetLocationResponseDto>> GetById(int id)
    {
        var location = await _db.AssetLocations
            .AsNoTracking()
            .Where(l => l.LocationId == id)
            .Select(l => new AssetLocationResponseDto
            {
                LocationId = l.LocationId,
                AssetInstanceId = l.AssetInstanceId,
                AssetId = l.AssetInstance.AssetId,
                InstanceCode = l.AssetInstance.InstanceCode,
                AssetName = l.AssetInstance.Asset.Name,
                AssetCode = l.AssetInstance.Asset.Code,
                DepartmentId = l.DepartmentId,
                DepartmentName = l.Department.Name,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                IsCurrent = l.IsCurrent,
                Note = l.Note
            })
            .FirstOrDefaultAsync();

        if (location == null)
            return NotFound(new { message = $"Asset location with id {id} not found." });

        return Ok(location);
    }

    /// <summary>
    /// POST /api/AssetLocations - Create a new asset location record for an <see cref="AssetInstance"/>.
    /// If IsCurrent is true, the previous current record for the same instance is
    /// automatically closed (IsCurrent=false, EndDate set to StartDate - 1 day).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssetLocationResponseDto>> Create([FromBody] CreateAssetLocationDto dto)
    {
        var instanceExists = await _db.AssetInstances.AnyAsync(i => i.AssetInstanceId == dto.AssetInstanceId);
        if (!instanceExists)
            return NotFound(new { message = $"Asset instance with id {dto.AssetInstanceId} not found." });

        var departmentExists = await _db.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId);
        if (!departmentExists)
            return NotFound(new { message = $"Department with id {dto.DepartmentId} not found." });

        if (dto.EndDate.HasValue && dto.EndDate.Value <= dto.StartDate)
            return BadRequest(new { message = "EndDate must be after StartDate." });

        if (dto.IsCurrent)
            await CloseCurrentLocationAsync(dto.AssetInstanceId, excludeLocationId: null, newStartDate: dto.StartDate);

        var location = new AssetLocation
        {
            AssetInstanceId = dto.AssetInstanceId,
            DepartmentId = dto.DepartmentId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsCurrent = dto.IsCurrent,
            Note = dto.Note?.Trim()
        };

        _db.AssetLocations.Add(location);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = location.LocationId },
            await BuildResponseDtoAsync(location.LocationId));
    }

    /// <summary>
    /// PUT /api/AssetLocations/{id} - Update an existing asset location record.
    /// If IsCurrent is being set to true, the previous current record for the same
    /// instance is automatically closed.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetLocationResponseDto>> Update(int id, [FromBody] UpdateAssetLocationDto dto)
    {
        var location = await _db.AssetLocations.FindAsync(id);
        if (location == null)
            return NotFound(new { message = $"Asset location with id {id} not found." });

        var departmentExists = await _db.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId);
        if (!departmentExists)
            return NotFound(new { message = $"Department with id {dto.DepartmentId} not found." });

        if (dto.EndDate.HasValue && dto.EndDate.Value <= dto.StartDate)
            return BadRequest(new { message = "EndDate must be after StartDate." });

        if (dto.IsCurrent && !location.IsCurrent)
            await CloseCurrentLocationAsync(location.AssetInstanceId, excludeLocationId: id, newStartDate: dto.StartDate);

        location.DepartmentId = dto.DepartmentId;
        location.StartDate = dto.StartDate;
        location.EndDate = dto.EndDate;
        location.IsCurrent = dto.IsCurrent;
        location.Note = dto.Note?.Trim();

        await _db.SaveChangesAsync();

        return Ok(await BuildResponseDtoAsync(id));
    }

    /// <summary>
    /// DELETE /api/AssetLocations/{id} - Delete an asset location record.
    /// Blocked if the record is referenced by InventoryRecord, InventoryDiscrepancy,
    /// or TransferRecord.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var location = await _db.AssetLocations
            .Include(l => l.InventoryRecords)
            .Include(l => l.InventoryDiscrepancyActualLocations)
            .Include(l => l.InventoryDiscrepancyBookLocations)
            .Include(l => l.TransferRecordFromLocations)
            .Include(l => l.TransferRecordToLocations)
            .FirstOrDefaultAsync(l => l.LocationId == id);

        if (location == null)
            return NotFound(new { message = $"Asset location with id {id} not found." });

        var linkedCount =
            location.InventoryRecords.Count +
            location.InventoryDiscrepancyActualLocations.Count +
            location.InventoryDiscrepancyBookLocations.Count +
            location.TransferRecordFromLocations.Count +
            location.TransferRecordToLocations.Count;

        if (linkedCount > 0)
            return Conflict(new
            {
                message = $"Cannot delete this location record because it is referenced by {linkedCount} related record(s) " +
                          "(inventory records, inventory discrepancies, or transfer records). Remove those references first."
            });

        _db.AssetLocations.Remove(location);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task CloseCurrentLocationAsync(int assetInstanceId, int? excludeLocationId, DateOnly newStartDate)
    {
        var current = await _db.AssetLocations
            .Where(l => l.AssetInstanceId == assetInstanceId && l.IsCurrent &&
                        (excludeLocationId == null || l.LocationId != excludeLocationId))
            .FirstOrDefaultAsync();

        if (current != null)
        {
            current.IsCurrent = false;
            current.EndDate = newStartDate.AddDays(-1);
        }
    }

    private async Task<AssetLocationResponseDto> BuildResponseDtoAsync(int locationId)
    {
        return await _db.AssetLocations
            .AsNoTracking()
            .Where(l => l.LocationId == locationId)
            .Select(l => new AssetLocationResponseDto
            {
                LocationId = l.LocationId,
                AssetInstanceId = l.AssetInstanceId,
                AssetId = l.AssetInstance.AssetId,
                InstanceCode = l.AssetInstance.InstanceCode,
                AssetName = l.AssetInstance.Asset.Name,
                AssetCode = l.AssetInstance.Asset.Code,
                DepartmentId = l.DepartmentId,
                DepartmentName = l.Department.Name,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                IsCurrent = l.IsCurrent,
                Note = l.Note
            })
            .FirstAsync();
    }
}
