using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services;

/// <summary>Display names for allocation / request reporting (employee name, else email).</summary>
public static class AllocationReporting
{
    public static async Task<Dictionary<int, string>> ResolveUserDisplayNamesAsync(
        EaldsDbContext db,
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        var emps = await db.Employees.AsNoTracking()
            .Where(e => e.UserId != null && ids.Contains(e.UserId.Value))
            .Select(e => new { e.UserId, e.EmployeeId, e.Name })
            .ToListAsync(cancellationToken);

        var nameByUser = emps
            .Where(e => e.UserId.HasValue)
            .GroupBy(e => e.UserId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.EmployeeId).First().Name);

        var missing = ids.Where(id => !nameByUser.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var emails = await db.Users.AsNoTracking()
                .Where(u => missing.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Email, cancellationToken);
            foreach (var id in missing)
            {
                if (emails.TryGetValue(id, out var email))
                    nameByUser[id] = email;
                else
                    nameByUser[id] = $"User #{id}";
            }
        }

        return nameByUser;
    }

    /// <summary>Comment kế toán nhập khi duyệt (0 → accountant approved), lấy bản ghi mới nhất.</summary>
    public static async Task<string?> GetAccountantApprovalCommentAsync(
        EaldsDbContext db,
        int assetRequestId,
        CancellationToken cancellationToken = default)
    {
        return await db.AssetRequestRecords.AsNoTracking()
            .Where(rec =>
                rec.AssetRequestId == assetRequestId &&
                rec.ToStatus == AllocationOrderWorkflow.RequestStatusAccountantApproved &&
                rec.Action == 1)
            .OrderByDescending(rec => rec.OccurredAt)
            .Select(rec => rec.Comment)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
