using System.Security.Claims;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Utils;

/// <summary>
/// Resolves whether catalog / instance APIs should be limited to one department (department head).
/// </summary>
public readonly record struct AssetDepartmentScope(bool IsRestricted, int? DepartmentId)
{
    public static AssetDepartmentScope Unrestricted => new(false, null);
}

public static class DepartmentAssetScope
{
    /// <summary>
    /// Instance is counted as belonging to <paramref name="departmentId"/> when its current location is that department
    /// and it is not disposed, lost, or liquidated (same rules as GET /api/assets/department/{id}).
    /// </summary>
    public static bool InstanceBelongsToDepartment(AssetInstance i, int departmentId)
    {
        if (i.Status == (int)AssetStatus.Disposed ||
            i.Status == (int)AssetStatus.Lost ||
            i.Status == (int)AssetStatus.Liquidated)
            return false;

        return i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == departmentId);
    }

    public static async Task<AssetDepartmentScope> ResolveForUserAsync(
        ClaimsPrincipal user,
        EaldsDbContext db,
        int departmentHeadRoleId,
        CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true)
            return AssetDepartmentScope.Unrestricted;

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            return AssetDepartmentScope.Unrestricted;

        var roleRows = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => new { ur.RoleId, Code = ur.Role != null ? ur.Role.Code : null })
            .ToListAsync(cancellationToken);

        // Seeded ids: ADMIN=1, DIRECTOR=2, ACCOUNTANT=3, DEPARTMENT_HEAD=4
        if (roleRows.Any(r => r.RoleId is 1 or 2 or 3))
            return AssetDepartmentScope.Unrestricted;

        if (user.IsInRole("ACCOUNTANT") || user.IsInRole("DIRECTOR") || user.IsInRole("ADMIN"))
            return AssetDepartmentScope.Unrestricted;

        var isDeptHead = roleRows.Any(r =>
            r.RoleId == departmentHeadRoleId ||
            (r.Code != null &&
             (string.Equals(r.Code, "DEPARTMENT_HEAD", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(r.Code, "DEPARTMENTHEAD", StringComparison.OrdinalIgnoreCase))));

        if (!isDeptHead)
            return AssetDepartmentScope.Unrestricted;

        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        if (employee == null || employee.DepartmentId <= 0)
            return new AssetDepartmentScope(true, null);

        return new AssetDepartmentScope(true, employee.DepartmentId);
    }
}
