using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/repair")]
public class RepairRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _repairRequestTypeId;

    public RepairRequestsController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _repairRequestTypeId = configuration.GetValue<int>("App:RepairRequestTypeId", 4);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRepairRequest([FromBody] RepairRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest("Reason is required.");

        var title = dto.Title ?? $"Repair request for asset {dto.AssetId}";

        var assetRequest = new AssetRequest
        {
            UserId = dto.CreatedBy,
            RequestTypeId = _repairRequestTypeId,
            AssetId = dto.AssetId,
            Title = title,
            Description = dto.Description ?? dto.Reason,
            ProposedData = null,
            // Align with director workflow: newly created repair requests are considered "submitted"
            // so they can be approved/rejected by director (DirectorApproveController expects status=1).
            Status = 1,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var repairTask = new RepairTask
        {
            AssetRequestId = assetRequest.AssetRequestId,
            AssetId = dto.AssetId,
            EstimatedCost = dto.EstimatedCost,
            Reason = dto.Reason,
            Status = 0
        };

        _db.RepairTasks.Add(repairTask);

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = assetRequest.Status,
            ToStatus = assetRequest.Status,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Repair requested",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);

        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = assetRequest.AssetRequestId, taskId = repairTask.TaskId });
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartRepair(int id, [FromBody] RepairStartDto dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");
        if (dto.StartedBy <= 0)
            return BadRequest("StartedBy is required.");

        var ar = await _db.AssetRequests.FindAsync(id);
        if (ar == null) return NotFound();

        var isFinalApproved = await IsFinalApprovedByWorkflowAsync(ar);
        if (!isFinalApproved)
            return BadRequest("Only requests approved at final workflow step can be started.");

        var userRole = await _db.UserRoles.Include(ur => ur.Role).AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.StartedBy);
        var role = userRole?.Role;
        var allowed = role != null && (
            (role.Code != null && (role.Code.Equals("DepartmentManager", StringComparison.OrdinalIgnoreCase) || role.Code.Equals("Accountant", StringComparison.OrdinalIgnoreCase)))
            || (role.Name != null && (role.Name.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0 || role.Name.IndexOf("Director", StringComparison.OrdinalIgnoreCase) >= 0 || role.Name.IndexOf("Accountant", StringComparison.OrdinalIgnoreCase) >= 0))
        );

        if (!allowed) return Forbid();

        var from = ar.Status;
        ar.Status = 4;

        var task = await _db.RepairTasks.FirstOrDefaultAsync(t => t.AssetRequestId == ar.AssetRequestId);
        if (task != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.DamageCondition))
                task.Reason = dto.DamageCondition;
            if (dto.EstimatedCost.HasValue)
                task.EstimatedCost = dto.EstimatedCost.Value;
            task.Status = 1; // in-progress
            _db.RepairTasks.Update(task);
        }

        Dictionary<string, object?> startData = new()
        {
            ["flowType"] = "repair-start",
            ["reportNumber"] = dto.ReportNumber,
            ["damageDate"] = dto.DamageDate,
            ["damageCondition"] = dto.DamageCondition,
            ["attachmentDocumentIds"] = dto.AttachmentDocumentIds,
            ["attachmentUrls"] = dto.AttachmentUrls,
            ["repairDate"] = dto.RepairDate,
            ["expectedCompletionDate"] = dto.ExpectedCompletionDate,
            ["expectedCompletionFrom"] = dto.ExpectedCompletionFrom,
            ["expectedCompletionTo"] = dto.ExpectedCompletionTo,
            ["estimatedCost"] = dto.EstimatedCost,
            ["repairProgressStatus"] = dto.RepairProgressStatus
        };
        if (!string.IsNullOrWhiteSpace(ar.ProposedData))
            startData["legacyProposedData"] = ar.ProposedData;
        ar.ProposedData = JsonSerializer.Serialize(startData);

        var record = new AssetRequestRecord
        {
            AssetRequestId = ar.AssetRequestId,
            FromStatus = from,
            ToStatus = ar.Status,
            Action = 2,
            ActionByUserId = dto.StartedBy,
            ActionRoleId = userRole?.RoleId ?? 0,
            Comment = dto.Comment,
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { assetRequestId = ar.AssetRequestId, status = ar.Status, taskId = task?.TaskId });
    }

    private async Task<bool> IsFinalApprovedByWorkflowAsync(AssetRequest ar)
    {
        var workflowId = await _db.RequestTypes.AsNoTracking()
            .Where(rt => rt.RequestTypeId == ar.RequestTypeId)
            .Select(rt => (int?)rt.WorkflowId)
            .FirstOrDefaultAsync();

        if (!workflowId.HasValue || workflowId.Value == 0)
            return ar.Status == 2 || ar.Status == 4;

        var finalStepId = await _db.WorkflowSteps.AsNoTracking()
            .Where(ws => ws.WorkflowId == workflowId.Value)
            .OrderBy(ws => ws.StepOrder)
            .Select(ws => (int?)ws.StepId)
            .LastOrDefaultAsync();

        if (!finalStepId.HasValue)
            return ar.Status == 2 || ar.Status == 4;

        return await _db.Approvals.AsNoTracking().AnyAsync(a =>
            a.AssetRequestId == ar.AssetRequestId
            && a.StepId == finalStepId.Value
            && a.Decision == 1);
    }

    [HttpPost("tasks/{taskId}/complete")]
    public async Task<IActionResult> CompleteRepair(int taskId, [FromBody] Models.DTOs.RepairCompleteDto dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");
        if (dto.CompletedBy <= 0)
            return BadRequest("CompletedBy is required.");

        var task = await _db.RepairTasks.FindAsync(taskId);
        if (task == null) return NotFound();

        if (task.Status != 1)
            return BadRequest("Repair can only be completed while the task is in progress.");

        var ar = await _db.AssetRequests.FindAsync(task.AssetRequestId);
        if (ar != null
            && ar.RequestTypeId == _repairRequestTypeId
            && ar.Status != 4)
            return BadRequest("Repair can only be completed for requests in the in-progress repair state (status 4).");

        var repairDate = dto.CompletionDate ?? dto.RepairDate ?? DateTime.UtcNow;

        var resultLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(dto.ReportNumber))
            resultLines.Add($"ReportNumber: {dto.ReportNumber}");
        if (dto.ReturnToUseDate.HasValue)
            resultLines.Add($"ReturnToUseDate: {dto.ReturnToUseDate.Value:O}");
        if (!string.IsNullOrWhiteSpace(dto.Result))
            resultLines.Add(dto.Result);
        if (!string.IsNullOrWhiteSpace(dto.DetailedDescription))
            resultLines.Add(dto.DetailedDescription);
        var resultText = resultLines.Count > 0 ? string.Join("\n", resultLines) : string.Empty;

        var rr = new RepairRecord
        {
            TaskId = task.TaskId,
            ActualCost = dto.ActualCost,
            RepairDate = repairDate,
            Result = resultText,
            SupplierId = dto.SupplierId
        };

        _db.RepairRecords.Add(rr);

        task.Status = 2; // completed
        _db.RepairTasks.Update(task);

        if (ar != null)
        {
            var completionNode = new JsonObject
            {
                ["flowType"] = "repair-complete",
                ["reportNumber"] = dto.ReportNumber,
                ["completionDate"] = JsonSerializer.SerializeToNode(repairDate),
                ["returnToUseDate"] = dto.ReturnToUseDate.HasValue
                    ? JsonSerializer.SerializeToNode(dto.ReturnToUseDate.Value)
                    : null,
                ["actualCost"] = JsonSerializer.SerializeToNode(dto.ActualCost),
                ["attachmentDocumentIds"] = dto.AttachmentDocumentIds != null
                    ? JsonSerializer.SerializeToNode(dto.AttachmentDocumentIds)
                    : null,
                ["attachmentUrls"] = dto.AttachmentUrls != null
                    ? JsonSerializer.SerializeToNode(dto.AttachmentUrls)
                    : null,
                ["completedAt"] = JsonSerializer.SerializeToNode(DateTime.UtcNow)
            };

            JsonObject root;
            if (string.IsNullOrWhiteSpace(ar.ProposedData))
                root = new JsonObject();
            else
            {
                try
                {
                    var parsed = JsonNode.Parse(ar.ProposedData);
                    root = parsed as JsonObject ?? new JsonObject { ["legacy"] = parsed };
                }
                catch
                {
                    root = new JsonObject { ["legacyProposedDataRaw"] = ar.ProposedData };
                }
            }

            root["repairCompletion"] = completionNode;
            ar.ProposedData = root.ToJsonString();

            var rec = new AssetRequestRecord
            {
                AssetRequestId = ar.AssetRequestId,
                FromStatus = ar.Status,
                ToStatus = ar.Status,
                Action = 3,
                ActionByUserId = dto.CompletedBy,
                ActionRoleId = 0,
                Comment = "Repair completed",
                OccurredAt = DateTime.UtcNow
            };

            _db.AssetRequestRecords.Add(rec);
        }

        await _db.SaveChangesAsync();

        return Ok(new { recordId = rr.RecordId, taskId = task.TaskId });
    }
}
