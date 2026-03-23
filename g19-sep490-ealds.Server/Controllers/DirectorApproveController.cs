using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/director")]
public class DirectorApproveController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _transferRequestTypeId;
    private readonly int _purchaseRequestTypeId;
    public DirectorApproveController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
        _purchaseRequestTypeId = configuration.GetValue<int>("App:PurchaseRequestTypeId", 1);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar==null) return NotFound();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);
        var approval = new Approval
        {
            StepId = ar.StepId,
            AssetRequestId = ar.AssetRequestId,
            Decision = 1,
            DecisionDate = DateTime.UtcNow,
            ApprovedUserId = dto.ApprovedBy,
            ApprovedRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment
        };
        _db.Approvals.Add(approval);

        ar.Status = (int)AssetRequestStatus.Approved; ar.ApproveDate = DateTime.UtcNow;

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = (int)AssetRequestStatus.Draft,
            ToStatus = ar.Status,
            Action = 1,
            ActionByUserId = dto.ApprovedBy,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };
        _db.AssetRequestRecords.Add(record);

        // Bug 3 fix: create a Procurement record when a purchase request is approved by director
        var isPurchase = ar.RequestTypeId == _purchaseRequestTypeId;
        if (isPurchase && ar.Status == 2)
        {
            var alreadyHasProcurement = await _db.Procurements.AsNoTracking()
                .AnyAsync(p => p.AssetRequestId == ar.AssetRequestId);
            if (!alreadyHasProcurement)
            {
                var procurement = new Procurement
                {
                    AssetRequestId = ar.AssetRequestId,
                    ContractNo = string.Empty,      // To be filled by accountant later
                    ContractDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    TotalAmount = 0,
                    AdvanceAmount = 0,
                    RemainingAmount = 0,
                    Status = 0,                     // 0 = pending/initial
                    CreatedBy = dto.ApprovedBy,
                    CreateDate = DateTime.UtcNow
                };
                _db.Procurements.Add(procurement);
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status });
    }
}

