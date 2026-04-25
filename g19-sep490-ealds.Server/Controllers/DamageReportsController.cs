using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/report-damage")]
public class DamageReportsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private const int DamageRequestTypeId = 4;
    private const string DamageReportTitlePrefix = "Damage report -";

    public DamageReportsController(EaldsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Đánh dấu cá thể hỏng và lưu ghi chú (ngày/tình trạng). Không tạo đơn / không gửi phê duyệt —
    /// trưởng phòng tạo đề nghị sửa chữa tại màn hình Sửa chữa.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Report([FromBody] ReportDamageDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (dto.AssetInstanceId <= 0 || dto.ReportedBy <= 0)
            return BadRequest("AssetInstanceId and ReportedBy are required.");

        if (dto.ReportDate == default)
            return BadRequest("ReportDate is required.");

        var instance = await _db.AssetInstances.FindAsync(dto.AssetInstanceId);
        if (instance == null)
            return NotFound("Asset instance not found.");

        if (dto.DocumentId.HasValue)
        {
            var doc = await _db.Documents.FindAsync(dto.DocumentId.Value);
            if (doc == null)
                return BadRequest("Document not found.");
        }

        instance.Status = (int)AssetStatus.Damaged;
        if (!string.IsNullOrWhiteSpace(dto.Description))
            instance.Note = dto.Description.Trim();

        await _db.SaveChangesAsync();

        return Ok(new { assetInstanceId = dto.AssetInstanceId });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ar = await _db.AssetRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetRequestId == id);

        if (ar == null)
            return NotFound();

        // Accept both legacy title-based rows and request-type-based rows.
        var isDamageByTitle = !string.IsNullOrWhiteSpace(ar.Title) && ar.Title.StartsWith(DamageReportTitlePrefix, StringComparison.OrdinalIgnoreCase);
        var isDamageByType = ar.RequestTypeId == DamageRequestTypeId;
        if (!isDamageByTitle && !isDamageByType)
            return BadRequest("Only damage report requests can be deleted via this endpoint.");

        // Remove children first because FK delete behavior is ClientSetNull (no cascade)
        var approvals = await _db.Approvals.Where(x => x.AssetRequestId == id).ToListAsync();
        var records = await _db.AssetRequestRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var repairTasks = await _db.RepairTasks.Where(x => x.AssetRequestId == id).ToListAsync();
        var maintenanceTasks = await _db.MaintenanceTasks.Where(x => x.AssetRequestId == id).ToListAsync();
        var disposalRecords = await _db.DisposalRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var transferRecords = await _db.TransferRecords.Where(x => x.AssetRequestId == id).ToListAsync();
        var procurements = await _db.Procurements.Where(x => x.AssetRequestId == id).ToListAsync();

        if (approvals.Count > 0) _db.Approvals.RemoveRange(approvals);
        if (records.Count > 0) _db.AssetRequestRecords.RemoveRange(records);
        if (repairTasks.Count > 0) _db.RepairTasks.RemoveRange(repairTasks);
        if (maintenanceTasks.Count > 0) _db.MaintenanceTasks.RemoveRange(maintenanceTasks);
        if (disposalRecords.Count > 0) _db.DisposalRecords.RemoveRange(disposalRecords);
        if (transferRecords.Count > 0) _db.TransferRecords.RemoveRange(transferRecords);
        if (procurements.Count > 0) _db.Procurements.RemoveRange(procurements);

        var tracked = new AssetRequest { AssetRequestId = id };
        _db.AssetRequests.Attach(tracked);
        _db.AssetRequests.Remove(tracked);

        await _db.SaveChangesAsync();
        return Ok(new { assetRequestId = id, deleted = true });
    }
}
