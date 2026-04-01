using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Controllers;

/// <summary>Aggregated metrics and previews for the director home dashboard.</summary>
[ApiController]
[Route("api/dashboard/director")]
public class DirectorDashboardController : ControllerBase
{
    private const decimal BillionVnd = 1_000_000_000m;
    private const int PendingPreviewLimit = 6;

    private readonly EaldsDbContext _db;
    private readonly int _transferRequestTypeId;

    public DirectorDashboardController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var now = DateTime.UtcNow;

        var totalAssets = await _db.AssetInstances.AsNoTracking().CountAsync();
        var totalValueVnd = await _db.AssetInstances.AsNoTracking()
            .SumAsync(i => (decimal?)i.CurrentValue) ?? 0m;
        var totalAssetValueBillions = Math.Round(totalValueVnd / BillionVnd, 2, MidpointRounding.AwayFromZero);

        var pendingApprovals = await CountDirectorPendingRequestsAsync();

        var maintenanceDueSoon = await _db.MaintenanceSchedules.AsNoTracking()
            .Where(s => s.IsActive && s.NextDueDate != null && s.NextDueDate <= now.AddDays(60))
            .CountAsync();
        var inventoryAwaitingDirector = await _db.InventorySessions.AsNoTracking()
            .CountAsync(s => s.Status == (int)InventorySessionStatus.Completed);
        var assetsDueMaintenance = maintenanceDueSoon + inventoryAwaitingDirector;

        var assetStatusBreakdown = await BuildAssetStatusBreakdownAsync();

        var pendingPreview = await BuildPendingPreviewAsync(PendingPreviewLimit);

        return Ok(new DirectorDashboardSummaryDto
        {
            Kpi = new DirectorDashboardKpiDto
            {
                TotalAssets = totalAssets,
                TotalAssetValue = totalAssetValueBillions,
                PendingApprovals = pendingApprovals,
                AssetsDueMaintenance = assetsDueMaintenance
            },
            PendingPreview = pendingPreview,
            AssetStatusBreakdown = assetStatusBreakdown
        });
    }

    private async Task<int> CountDirectorPendingRequestsAsync()
    {
        return await DirectorPendingQuery().CountAsync();
    }

    private IQueryable<AssetRequest> DirectorPendingQuery()
    {
        return _db.AssetRequests.AsNoTracking()
            .Where(ar =>
                (ar.Status == 2
                 && (ar.RequestTypeId == _transferRequestTypeId
                     || _db.TransferRecords.Any(tr => tr.AssetRequestId == ar.AssetRequestId)))
                || (ar.Status == 1
                    && !(ar.RequestTypeId == _transferRequestTypeId
                         || _db.TransferRecords.Any(tr => tr.AssetRequestId == ar.AssetRequestId))));
    }

    private async Task<List<DirectorDashboardPendingRowDto>> BuildPendingPreviewAsync(int take)
    {
        var rows = await DirectorPendingQuery()
            .OrderByDescending(ar => ar.CreateDate)
            .Take(take)
            .Select(ar => new
            {
                ar.AssetRequestId,
                ar.RequestTypeId,
                ar.CreateDate,
                DepartmentName = ar.Asset != null
                    ? ar.Asset.AssetInstances
                        .SelectMany(ai => ai.AssetLocations)
                        .Where(al => al.IsCurrent)
                        .Select(al => al.Department != null ? al.Department.Name : null)
                        .FirstOrDefault()
                    : null
            })
            .ToListAsync();

        return rows.Select(r => new DirectorDashboardPendingRowDto
        {
            Id = r.AssetRequestId.ToString(),
            RequestType = MapRequestTypeName(r.RequestTypeId),
            Department = r.DepartmentName ?? "—",
            CreateDate = r.CreateDate,
            Status = "Chờ phê duyệt"
        }).ToList();
    }

    private static string MapRequestTypeName(int requestTypeId) => requestTypeId switch
    {
        1 => "Mua sắm",
        2 => "Bảo dưỡng",
        3 => "Điều chuyển",
        4 => "Sửa chữa",
        5 => "Thanh lý",
        _ => $"Loại #{requestTypeId}"
    };

    private async Task<List<DirectorDashboardAssetStatusSliceDto>> BuildAssetStatusBreakdownAsync()
    {
        var groups = await _db.AssetInstances.AsNoTracking()
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        long inUse = 0, available = 0, repairMaint = 0, disposed = 0, other = 0;
        foreach (var g in groups)
        {
            var s = g.Status;
            // InUse and Active share value 1 in AssetStatus
            if (s == (int)AssetStatus.InUse || s == (int)AssetStatus.Available)
            {
                if (s == (int)AssetStatus.Available)
                    available += g.Count;
                else
                    inUse += g.Count;
            }
            else if (s == (int)AssetStatus.InMaintenance || s == (int)AssetStatus.InRepair)
                repairMaint += g.Count;
            else if (s == (int)AssetStatus.Disposed || s == (int)AssetStatus.Lost || s == (int)AssetStatus.Liquidated)
                disposed += g.Count;
            else
                other += g.Count;
        }

        var slices = new List<DirectorDashboardAssetStatusSliceDto>();
        void Add(string name, long value, string color)
        {
            if (value > 0)
                slices.Add(new DirectorDashboardAssetStatusSliceDto { Name = name, Value = value, Color = color });
        }

        Add("Đang sử dụng", inUse, "#1677ff");
        Add("Nhàn rỗi", available, "#52c41a");
        Add("Đang sửa chữa / bảo trì", repairMaint, "#faad14");
        Add("Thanh lý / loại bỏ", disposed, "#ff4d4f");
        Add("Khác", other, "#722ed1");

        return slices;
    }
}

public sealed class DirectorDashboardSummaryDto
{
    public DirectorDashboardKpiDto Kpi { get; set; } = null!;
    public List<DirectorDashboardPendingRowDto> PendingPreview { get; set; } = new();
    public List<DirectorDashboardAssetStatusSliceDto> AssetStatusBreakdown { get; set; } = new();
}

public sealed class DirectorDashboardKpiDto
{
    public int TotalAssets { get; set; }
    public decimal TotalAssetValue { get; set; }
    public int PendingApprovals { get; set; }
    public int AssetsDueMaintenance { get; set; }
}

public sealed class DirectorDashboardPendingRowDto
{
    public string Id { get; set; } = "";
    public string RequestType { get; set; } = "";
    public string Department { get; set; } = "";
    public DateTime CreateDate { get; set; }
    public string Status { get; set; } = "";
}

public sealed class DirectorDashboardAssetStatusSliceDto
{
    public string Name { get; set; } = "";
    public long Value { get; set; }
    public string Color { get; set; } = "#1677ff";
}
