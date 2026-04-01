using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Allocations")]
public class AllocationsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private const int AllocationTypeId = 100;
    private const int RevocationTypeId = 101;

    public AllocationsController(EaldsDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AllocationSummaryDTO>> GetSummary()
    {
        var departments = await _db.Departments.AsNoTracking().ToListAsync();
        decimal totalBudget = 0; // Bỏ qua budget cứng từ database theo yêu cầu

        var allocationRequests = await _db.AssetRequests
            .AsNoTracking()
            .Where(r => r.RequestTypeId == AllocationTypeId || r.RequestTypeId == RevocationTypeId)
            .ToListAsync();

        decimal allocatedAmount = 0;
        decimal revokedAmount = 0;
        int revokedCount = 0;
        decimal pendingAmount = 0;
        int pendingCount = 0;

        foreach (var req in allocationRequests)
        {
            if (string.IsNullOrEmpty(req.ProposedData)) continue;
            
            try 
            {
                var doc = JsonDocument.Parse(req.ProposedData);
                decimal amount = 0;
                if (doc.RootElement.TryGetProperty("Amount", out var amtProp))
                {
                    amount = amtProp.GetDecimal();
                }

                if (req.Status < 4) // Pending
                {
                    pendingAmount += amount;
                    pendingCount++;
                }
                else // Status 4 or higher indicates approved
                {
                    if (req.RequestTypeId == AllocationTypeId)
                    {
                        allocatedAmount += amount;
                    }
                    else if (req.RequestTypeId == RevocationTypeId)
                    {
                        revokedAmount += amount;
                        revokedCount++;
                    }
                }
            }
            catch {}
        }

        double pct = 0;
        if (totalBudget > 0)
        {
            pct = (double)(allocatedAmount / totalBudget) * 100;
        }

        return Ok(new AllocationSummaryDTO
        {
            TotalBudget = totalBudget,
            AllocatedAmount = allocatedAmount,
            AllocatedPercentage = Math.Round(pct, 1),
            RevokedAmount = revokedAmount,
            RevokedCount = revokedCount,
            PendingAmount = pendingAmount,
            PendingCount = pendingCount
        });
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IEnumerable<AllocationTransactionDTO>>> GetTransactions()
    {
        var allocationRequests = await _db.AssetRequests
            .AsNoTracking()
            .Where(r => r.RequestTypeId == AllocationTypeId || r.RequestTypeId == RevocationTypeId)
            .OrderByDescending(r => r.CreateDate)
            .ToListAsync();

        var departments = await _db.Departments.AsNoTracking().ToDictionaryAsync(d => d.DepartmentId, d => d.Code);

        var results = new List<AllocationTransactionDTO>();
        foreach (var req in allocationRequests)
        {
            var dto = new AllocationTransactionDTO
            {
                Id = "TX" + req.AssetRequestId.ToString().PadLeft(3, '0'),
                Name = req.Title,
                Date = req.CreateDate.ToString("yyyy-MM-dd")
            };

            if (req.Status < 4)
                dto.Status = "pending";
            else if (req.RequestTypeId == AllocationTypeId)
                dto.Status = "allocated";
            else if (req.RequestTypeId == RevocationTypeId)
                dto.Status = "recalled";

            if (!string.IsNullOrEmpty(req.ProposedData))
            {
                try
                {
                    var doc = JsonDocument.Parse(req.ProposedData);
                    if (doc.RootElement.TryGetProperty("Category", out var catProp)) dto.Cat = catProp.GetString() ?? "";
                    if (doc.RootElement.TryGetProperty("Amount", out var amtProp)) dto.Amount = amtProp.GetDecimal();
                    if (doc.RootElement.TryGetProperty("ApproverName", out var appProp)) dto.Approver = appProp.GetString() ?? "";
                    
                    if (doc.RootElement.TryGetProperty("DepartmentId", out var deptProp))
                    {
                        int dId = deptProp.GetInt32();
                        if (departments.ContainsKey(dId)) dto.Dept = departments[dId].ToLower();
                        else dto.Dept = dId.ToString();
                    }
                }
                catch {}
            }

            results.Add(dto);
        }

        return Ok(results);
    }

    [HttpPost("allocate")]
    // [Authorize] -> enable later if needed
    public async Task<IActionResult> Allocate([FromBody] CreateAllocationRequestDTO dto)
    {
        return await CreateAllocationRequest(dto, AllocationTypeId);
    }

    [HttpPost("recall")]
    public async Task<IActionResult> Recall([FromBody] CreateAllocationRequestDTO dto)
    {
        return await CreateAllocationRequest(dto, RevocationTypeId);
    }

    private async Task<IActionResult> CreateAllocationRequest(CreateAllocationRequestDTO dto, int typeId)
    {
        if (dto.Amount <= 0) return BadRequest("Số tiền không hợp lệ.");

        var proposedObj = new
        {
            Amount = dto.Amount,
            Category = dto.Category,
            DepartmentId = dto.DepartmentId,
            ApproverName = dto.Approver,
            Note = dto.Note
        };

        var assetRequest = new AssetRequest
        {
            // Just use a generic or systemic UserId=1 if the endpoint is called unauthenticated in tests
            UserId = 1,
            CreatedBy = 1,
            RequestTypeId = typeId,
            Title = dto.Name,
            Description = "Budget allocation auto-generated request",
            ProposedData = JsonSerializer.Serialize(proposedObj),
            Status = 1, // Pending
            CreateDate = dto.Date == default ? DateTime.UtcNow : dto.Date,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        return Ok(new { id = assetRequest.AssetRequestId });
    }

    [HttpPut("transactions/{id}/approve")]
    public async Task<IActionResult> ApproveTransaction(int id)
    {
        var req = await _db.AssetRequests.FindAsync(id);
        if (req == null) return NotFound();

        if (req.Status >= 4) return BadRequest("Đã được phê duyệt.");
        
        req.Status = 4;
        req.ApproveDate = DateTime.UtcNow;

        // In a real flow, this would add an Approval record. We simulate the final state.
        await _db.SaveChangesAsync();
        return Ok(new { status = "success" });
    }

    [HttpDelete("transactions/{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var req = await _db.AssetRequests.FindAsync(id);
        if (req == null) return NotFound();

        _db.AssetRequests.Remove(req);
        await _db.SaveChangesAsync();
        return Ok(new { status = "success" });
    }
}

