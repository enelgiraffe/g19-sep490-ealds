using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.DTOs.Allocation;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services;

/// <summary>Parse proposed JSON, validate warehouse stock, create đơn cấp phát, and confirm assignments.</summary>
public static class AllocationOrderWorkflow
{
    public const int RequestStatusPendingAccountant = 0;
    public const int RequestStatusAccountantApproved = 2;
    public const int RequestStatusRejected = 3;
    public const int RequestStatusDeptConfirmed = 4;

    /// <summary>Allocation request created from an approved PR: waiting until linked PO is fully received (goods receipt).</summary>
    public const int RequestStatusAwaitingGoodsReceipt = 5;

    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal sealed class ProposedLineJson
    {
        public int AssetTypeId { get; set; }
        public int AssetId { get; set; }
        public int Quantity { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>JSON shape of <see cref="AssetRequest.ProposedData"/> for allocation requests.</summary>
    internal sealed class ProposedRootJson
    {
        public int DepartmentId { get; set; }
        public List<ProposedLineJson>? Lines { get; set; }
    }

    public static async Task<int> GetWarehouseAvailableCountAsync(
        EaldsDbContext db,
        int assetId,
        CancellationToken cancellationToken = default)
    {
        return await db.AssetInstances
            .AsNoTracking()
            .CountAsync(
                i => i.AssetId == assetId && !i.AssetLocations.Any(al => al.IsCurrent),
                cancellationToken);
    }

    /// <summary>Instances currently assigned to the department (book location) for a catalog asset.</summary>
    public static async Task<int> GetDepartmentAssignedCountAsync(
        EaldsDbContext db,
        int assetId,
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        return await db.AssetInstances
            .AsNoTracking()
            .CountAsync(
                i => i.AssetId == assetId &&
                     i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == departmentId),
                cancellationToken);
    }

    public static string BuildProposedDataJson(int departmentId, IReadOnlyList<AllocationLineInputDto> lines)
    {
        var payload = new
        {
            departmentId,
            lines = lines.Select(l => new
            {
                assetTypeId = l.AssetTypeId,
                assetId = l.AssetId,
                quantity = l.Quantity,
                reason = string.IsNullOrWhiteSpace(l.Reason) ? null : l.Reason.Trim()
            })
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    internal static bool TryParseProposedData(string? json, out ProposedRootJson? root, out string? error)
    {
        root = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Thiếu dữ liệu yêu cầu.";
            return false;
        }

        try
        {
            var doc = JsonSerializer.Deserialize<ProposedRootJson>(json, JsonRead);
            if (doc?.Lines == null || doc.Lines.Count == 0)
            {
                error = "Danh sách tài sản không được để trống.";
                return false;
            }

            root = doc;
            return true;
        }
        catch
        {
            error = "Dữ liệu yêu cầu không hợp lệ.";
            return false;
        }
    }

    internal static async Task<string?> ValidateLinesAgainstDatabaseAsync(
        EaldsDbContext db,
        ProposedRootJson root,
        CancellationToken cancellationToken = default,
        bool validateWarehouseAvailability = true)
    {
        if (root.DepartmentId <= 0)
            return "Phòng ban không hợp lệ.";

        if (!await db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == root.DepartmentId, cancellationToken))
            return $"Phòng ban {root.DepartmentId} không tồn tại.";

        var lines = root.Lines ?? new List<ProposedLineJson>();
        if (lines.Count == 0)
            return "Danh sách tài sản không được để trống.";

        foreach (var line in lines)
        {
            if (line.Quantity < 1)
                return "Số lượng phải lớn hơn 0.";

            var asset = await db.Assets
                .AsNoTracking()
                .Include(a => a.AssetType)
                .FirstOrDefaultAsync(a => a.AssetId == line.AssetId, cancellationToken);
            if (asset == null)
                return $"Tài sản #{line.AssetId} không tồn tại.";

            if (asset.AssetTypeId != line.AssetTypeId)
                return $"Tài sản không thuộc loại đã chọn (dòng mã {asset.Code}).";

            if (validateWarehouseAvailability)
            {
                var available = await GetWarehouseAvailableCountAsync(db, line.AssetId, cancellationToken);
                if (line.Quantity > available)
                    return $"Số lượng vượt quá tồn kho cho {asset.Name} (còn {available}).";
            }
        }

        return null;
    }

    internal static async Task<string?> ValidateReturnLinesAgainstDatabaseAsync(
        EaldsDbContext db,
        ProposedRootJson root,
        CancellationToken cancellationToken = default)
    {
        if (root.DepartmentId <= 0)
            return "Phòng ban không hợp lệ.";

        if (!await db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == root.DepartmentId, cancellationToken))
            return $"Phòng ban {root.DepartmentId} không tồn tại.";

        var lines = root.Lines ?? new List<ProposedLineJson>();
        if (lines.Count == 0)
            return "Danh sách tài sản không được để trống.";

        foreach (var line in lines)
        {
            if (line.Quantity < 1)
                return "Số lượng phải lớn hơn 0.";

            var asset = await db.Assets
                .AsNoTracking()
                .Include(a => a.AssetType)
                .FirstOrDefaultAsync(a => a.AssetId == line.AssetId, cancellationToken);
            if (asset == null)
                return $"Tài sản #{line.AssetId} không tồn tại.";

            if (asset.AssetTypeId != line.AssetTypeId)
                return $"Tài sản không thuộc loại đã chọn (dòng mã {asset.Code}).";

            var atDept = await GetDepartmentAssignedCountAsync(db, line.AssetId, root.DepartmentId, cancellationToken);
            if (line.Quantity > atDept)
                return $"Số lượng vượt quá tài sản đang ở phòng ban cho {asset.Name} (còn {atDept}).";
        }

        return null;
    }

    /// <summary>Creates allocation order + lines; caller saves changes.</summary>
    public static async Task<string?> TryCreateOrderOnAccountantApproveAsync(
        EaldsDbContext db,
        AssetRequest ar,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseProposedData(ar.ProposedData, out var root, out var parseErr) || root == null)
            return parseErr;

        var validation = await ValidateLinesAgainstDatabaseAsync(db, root, cancellationToken, validateWarehouseAvailability: true);
        if (validation != null)
            return validation;

        if (await db.AssetAllocationOrders.AnyAsync(o => o.AssetRequestId == ar.AssetRequestId, cancellationToken))
            return "Đơn cấp phát đã được tạo cho yêu cầu này.";

        var order = new AssetAllocationOrder
        {
            AssetRequestId = ar.AssetRequestId,
            DepartmentId = root.DepartmentId,
            RequestedByUserId = ar.UserId,
            RequestSubmittedAt = ar.CreateDate,
            Status = AssetAllocationOrderStatus.AwaitingDepartmentConfirm,
            CreatedAt = DateTime.UtcNow,
            Kind = AssetAllocationOrderKind.Allocation
        };

        foreach (var line in root.Lines!)
        {
            order.Lines.Add(new AssetAllocationOrderLine
            {
                AssetTypeId = line.AssetTypeId,
                AssetId = line.AssetId,
                Quantity = line.Quantity,
                Reason = string.IsNullOrWhiteSpace(line.Reason) ? null : line.Reason.Trim()
            });
        }

        db.AssetAllocationOrders.Add(order);
        return null;
    }

    /// <summary>Creates handover (return-to-warehouse) order + lines; caller saves changes.</summary>
    public static async Task<string?> TryCreateHandoverOrderOnAccountantApproveAsync(
        EaldsDbContext db,
        AssetRequest ar,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseProposedData(ar.ProposedData, out var root, out var parseErr) || root == null)
            return parseErr;

        var validation = await ValidateReturnLinesAgainstDatabaseAsync(db, root, cancellationToken);
        if (validation != null)
            return validation;

        if (await db.AssetAllocationOrders.AnyAsync(o => o.AssetRequestId == ar.AssetRequestId, cancellationToken))
            return "Đơn hoàn trả đã được tạo cho yêu cầu này.";

        var order = new AssetAllocationOrder
        {
            AssetRequestId = ar.AssetRequestId,
            DepartmentId = root.DepartmentId,
            RequestedByUserId = ar.UserId,
            RequestSubmittedAt = ar.CreateDate,
            Status = AssetAllocationOrderStatus.AwaitingDepartmentConfirm,
            CreatedAt = DateTime.UtcNow,
            Kind = AssetAllocationOrderKind.ReturnToWarehouse
        };

        foreach (var line in root.Lines!)
        {
            order.Lines.Add(new AssetAllocationOrderLine
            {
                AssetTypeId = line.AssetTypeId,
                AssetId = line.AssetId,
                Quantity = line.Quantity,
                Reason = string.IsNullOrWhiteSpace(line.Reason) ? null : line.Reason.Trim()
            });
        }

        db.AssetAllocationOrders.Add(order);
        return null;
    }

    public static async Task<string?> ConfirmOrderAsync(
        EaldsDbContext db,
        int orderId,
        int actorUserId,
        int departmentHeadRoleId,
        CancellationToken cancellationToken = default)
    {
        var inHeadRole = await db.UserRoles.AsNoTracking()
            .AnyAsync(ur => ur.UserId == actorUserId && ur.RoleId == departmentHeadRoleId, cancellationToken);
        if (!inHeadRole)
            return "Chỉ trưởng phòng ban mới xác nhận cấp phát.";

        var employeeDeptId = await db.Employees.AsNoTracking()
            .Where(e => e.UserId == actorUserId)
            .OrderBy(e => e.EmployeeId)
            .Select(e => (int?)e.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!employeeDeptId.HasValue)
            return "Không tìm thấy phòng ban của người dùng.";

        var order = await db.AssetAllocationOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.AssetAllocationOrderId == orderId, cancellationToken);
        if (order == null)
            return "Không tìm thấy đơn cấp phát.";

        if (order.DepartmentId != employeeDeptId.Value)
            return "Bạn không thuộc phòng ban của đơn này.";

        if (await DepartmentAssetScope.DepartmentHasInventoryInProgressAsync(
                db,
                order.DepartmentId,
                cancellationToken))
            return DepartmentAssetScope.InventoryInProgressBlockingMessage;

        if (order.Status != AssetAllocationOrderStatus.AwaitingDepartmentConfirm)
            return "Đơn đã được xác nhận hoặc không hợp lệ.";

        var ar = await db.AssetRequests.FirstOrDefaultAsync(r => r.AssetRequestId == order.AssetRequestId, cancellationToken);
        if (ar == null)
            return "Không tìm thấy yêu cầu gốc.";

        var effective = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        if (order.Kind == AssetAllocationOrderKind.ReturnToWarehouse)
        {
            foreach (var line in order.Lines)
            {
                var instances = await db.AssetInstances
                    .Where(i =>
                        i.AssetId == line.AssetId &&
                        i.AssetLocations.Any(al => al.IsCurrent && al.DepartmentId == order.DepartmentId))
                    .OrderBy(i => i.AssetInstanceId)
                    .Take(line.Quantity)
                    .ToListAsync(cancellationToken);

                if (instances.Count < line.Quantity)
                {
                    var assetName = await db.Assets.AsNoTracking()
                        .Where(a => a.AssetId == line.AssetId)
                        .Select(a => a.Name)
                        .FirstOrDefaultAsync(cancellationToken) ?? $"#{line.AssetId}";
                    return $"Không đủ tài sản tại phòng ban để hoàn trả: {assetName} (thiếu {line.Quantity - instances.Count}).";
                }

                foreach (var inst in instances)
                    await CloseCurrentLocationAsync(db, inst.AssetInstanceId, effective, cancellationToken);
            }
        }
        else
        {
            foreach (var line in order.Lines)
            {
                var instances = await db.AssetInstances
                    .Where(i => i.AssetId == line.AssetId && !i.AssetLocations.Any(al => al.IsCurrent))
                    .OrderBy(i => i.AssetInstanceId)
                    .Take(line.Quantity)
                    .ToListAsync(cancellationToken);

                if (instances.Count < line.Quantity)
                {
                    var assetName = await db.Assets.AsNoTracking()
                        .Where(a => a.AssetId == line.AssetId)
                        .Select(a => a.Name)
                        .FirstOrDefaultAsync(cancellationToken) ?? $"#{line.AssetId}";
                    return $"Không đủ tài sản trong kho để cấp: {assetName} (thiếu {line.Quantity - instances.Count}).";
                }

                foreach (var inst in instances)
                {
                    await CloseCurrentLocationAsync(db, inst.AssetInstanceId, effective, cancellationToken);
                    db.AssetLocations.Add(new AssetLocation
                    {
                        AssetInstanceId = inst.AssetInstanceId,
                        DepartmentId = order.DepartmentId,
                        StartDate = effective,
                        EndDate = null,
                        IsCurrent = true
                    });
                }
            }
        }

        order.Status = AssetAllocationOrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.UtcNow;
        order.ConfirmedByUserId = actorUserId;
        ar.Status = RequestStatusDeptConfirmed;
        ar.ApproveDate ??= DateTime.UtcNow;

        return null;
    }

    private static async Task CloseCurrentLocationAsync(
        EaldsDbContext db,
        int assetInstanceId,
        DateOnly newStartDate,
        CancellationToken cancellationToken)
    {
        var current = await db.AssetLocations
            .Where(l => l.AssetInstanceId == assetInstanceId && l.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);

        if (current != null)
        {
            current.IsCurrent = false;
            current.EndDate = newStartDate.AddDays(-1);
        }
    }
}
