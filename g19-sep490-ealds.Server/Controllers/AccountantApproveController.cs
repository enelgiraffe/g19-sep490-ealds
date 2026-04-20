using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services;
using g19_sep490_ealds.Server.Services.Interface;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/accountant")]
public class AccountantApproveController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _transferRequestTypeId;
    private readonly int _allocationRequestTypeId;
    private readonly int _handoverRequestTypeId;

    public AccountantApproveController(
        EaldsDbContext db,
        IConfiguration configuration,
        IAssetRequestNotificationService requestNotifications)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _transferRequestTypeId = configuration.GetValue<int>("App:TransferRequestTypeId", 3);
        _allocationRequestTypeId = configuration.GetValue<int>("App:AllocationRequestTypeId", 6);
        _handoverRequestTypeId = configuration.GetValue<int>("App:HandoverRequestTypeId", 7);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovalActionDto dto)
    {
        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar==null) return NotFound();
        var fromStatus = ar.Status;
        var isTransfer =
            ar.RequestTypeId == _transferRequestTypeId
            || await _db.TransferRecords.AsNoTracking().AnyAsync(tr => tr.AssetRequestId == ar.AssetRequestId);
        var isAllocation = ar.RequestTypeId == _allocationRequestTypeId;
        var isHandover = ar.RequestTypeId == _handoverRequestTypeId;
        if (!(ar.Status == 0 || (isTransfer && ar.Status == 1)))
            return BadRequest("Only requests with status=0 (Sent) can be approved by accountant.");

        if (isAllocation || isHandover)
        {
            if (ar.Status != 0)
                return BadRequest(isHandover
                    ? "Yêu cầu hoàn trả không ở trạng thái chờ kế toán."
                    : "Yêu cầu cấp phát không ở trạng thái chờ kế toán.");
            var orderErr = isHandover
                ? await AllocationOrderWorkflow.TryCreateHandoverOrderOnAccountantApproveAsync(_db, ar)
                : await AllocationOrderWorkflow.TryCreateOrderOnAccountantApproveAsync(_db, ar);
            if (orderErr != null)
                return BadRequest(new { message = orderErr });

            var rtAlloc = await _db.RequestTypes.FindAsync(ar.RequestTypeId);
            var workflowIdAlloc = rtAlloc?.WorkflowId ?? 0;
            var stepsAlloc = await _db.WorkflowSteps.Where(s => s.WorkflowId == workflowIdAlloc).OrderBy(s => s.StepOrder).ToListAsync();
            var currentStepAlloc = ar.StepId == 0
                ? stepsAlloc.FirstOrDefault()
                : stepsAlloc.FirstOrDefault(s => s.StepId == ar.StepId) ?? stepsAlloc.FirstOrDefault();

            ar.Status = AllocationOrderWorkflow.RequestStatusAccountantApproved;
            ar.ApproveDate = DateTime.UtcNow;
            if (currentStepAlloc != null)
            {
                var userRoleAlloc = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);
                _db.Approvals.Add(new Approval
                {
                    StepId = currentStepAlloc.StepId,
                    AssetRequestId = ar.AssetRequestId,
                    Decision = 1,
                    DecisionDate = DateTime.UtcNow,
                    ApprovedUserId = dto.ApprovedBy,
                    ApprovedRoleId = userRoleAlloc?.RoleId ?? 0,
                    Comment = dto.Comment
                });
            }

            _db.AssetRequestRecords.Add(new AssetRequestRecord
            {
                AssetRequestId = ar.AssetRequestId,
                FromStatus = fromStatus,
                ToStatus = ar.Status,
                Action = 1,
                ActionByUserId = dto.ApprovedBy,
                ActionRoleId = (await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy))?.RoleId ?? 0,
                Comment = dto.Comment,
                OccurredAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await _requestNotifications.NotifySenderDecisionAsync(ar.AssetRequestId, true, dto.ApprovedBy, dto.Comment);
            return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status });
        }

        var rt = await _db.RequestTypes.FindAsync(ar.RequestTypeId);
        int workflowId = rt?.WorkflowId ?? 0;
        var steps = await _db.WorkflowSteps.Where(s=>s.WorkflowId==workflowId).OrderBy(s=>s.StepOrder).ToListAsync();

        WorkflowStep currentStep = null!;
        if (ar.StepId==0) currentStep = steps.FirstOrDefault();
        else currentStep = steps.FirstOrDefault(s=>s.StepId==ar.StepId) ?? steps.FirstOrDefault();

        // Accounting approval:
        // - Purchase/etc: forward to director (waiting director decision) => 0 -> 1
        // - Transfer: mark as valid for next step (as per transfer status map) => 1 -> 2
        ar.Status = isTransfer ? 2 : 1;
        ar.ApproveDate = null;
        if (currentStep != null)
        {
            var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.ApprovedBy);
            var approval = new Approval
            {
                StepId = currentStep.StepId,
                AssetRequestId = ar.AssetRequestId,
                Decision = 1,
                DecisionDate = DateTime.UtcNow,
                ApprovedUserId = dto.ApprovedBy,
                ApprovedRoleId = userRole?.RoleId ?? 0,
                Comment = dto.Comment
            };
            _db.Approvals.Add(approval);
        }

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = ar.Status,
            Action = 1,
            ActionByUserId = dto.ApprovedBy,
            ActionRoleId = (await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur=>ur.UserId==dto.ApprovedBy))?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };
        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        if (isTransfer && ar.Status == 2)
            await _requestNotifications.NotifyTransferDirectorsPendingApprovalAsync(ar.AssetRequestId);

        await _requestNotifications.NotifySenderDecisionAsync(ar.AssetRequestId, true, dto.ApprovedBy, dto.Comment);

        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status });
    }
}
