using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class AllocationsService : IAllocationsService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<AllocationsService> _logger;
    private const int AllocationTypeId = 100;
    private const int RevocationTypeId = 101;

    public AllocationsService(EaldsDbContext context, ILogger<AllocationsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AllocationSummaryDTO> GetSummaryAsync()
    {
        var allocationRequests = await _context.AssetRequests
            .AsNoTracking()
            .Where(r => r.RequestTypeId == AllocationTypeId || r.RequestTypeId == RevocationTypeId)
            .ToListAsync();

        decimal allocatedAmount = 0, revokedAmount = 0, pendingAmount = 0;
        int revokedCount = 0, pendingCount = 0;

        foreach (var req in allocationRequests)
        {
            if (string.IsNullOrEmpty(req.ProposedData)) continue;
            try
            {
                var doc = JsonDocument.Parse(req.ProposedData);
                decimal amount = 0;
                if (doc.RootElement.TryGetProperty("Amount", out var amtProp))
                    amount = amtProp.GetDecimal();

                if (req.Status < 4)
                {
                    pendingAmount += amount;
                    pendingCount++;
                }
                else
                {
                    if (req.RequestTypeId == AllocationTypeId)
                        allocatedAmount += amount;
                    else if (req.RequestTypeId == RevocationTypeId)
                    {
                        revokedAmount += amount;
                        revokedCount++;
                    }
                }
            }
            catch { }
        }

        return new AllocationSummaryDTO
        {
            TotalBudget = 0,
            AllocatedAmount = allocatedAmount,
            AllocatedPercentage = 0,
            RevokedAmount = revokedAmount,
            RevokedCount = revokedCount,
            PendingAmount = pendingAmount,
            PendingCount = pendingCount
        };
    }

    public async Task<IEnumerable<AllocationTransactionDTO>> GetTransactionsAsync()
    {
        var allocationRequests = await _context.AssetRequests
            .AsNoTracking()
            .Where(r => r.RequestTypeId == AllocationTypeId || r.RequestTypeId == RevocationTypeId)
            .OrderByDescending(r => r.CreateDate)
            .ToListAsync();

        var departments = await _context.Departments.AsNoTracking()
            .ToDictionaryAsync(d => d.DepartmentId, d => d.Code);

        var results = new List<AllocationTransactionDTO>();
        foreach (var req in allocationRequests)
        {
            var dto = new AllocationTransactionDTO
            {
                Id = "TX" + req.AssetRequestId.ToString().PadLeft(3, '0'),
                Name = req.Title,
                Date = req.CreateDate.ToString("yyyy-MM-dd")
            };

            if (req.Status < 4) dto.Status = "pending";
            else if (req.RequestTypeId == AllocationTypeId) dto.Status = "allocated";
            else if (req.RequestTypeId == RevocationTypeId) dto.Status = "recalled";

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
                        dto.Dept = departments.ContainsKey(dId) ? departments[dId].ToLower() : dId.ToString();
                    }
                }
                catch { }
            }

            results.Add(dto);
        }

        return results;
    }

    public async Task<int> AllocateAsync(CreateAllocationRequestDTO dto)
    {
        return await CreateAllocationRequestAsync(dto, AllocationTypeId);
    }

    public async Task<int> RecallAsync(CreateAllocationRequestDTO dto)
    {
        return await CreateAllocationRequestAsync(dto, RevocationTypeId);
    }

    public async Task ApproveTransactionAsync(int id)
    {
        var req = await _context.AssetRequests.FindAsync(id);
        if (req == null) throw new KeyNotFoundException("Transaction not found.");
        if (req.Status >= 4) throw new InvalidOperationException("Đã được phê duyệt.");

        req.Status = 4;
        req.ApproveDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTransactionAsync(int id)
    {
        var req = await _context.AssetRequests.FindAsync(id);
        if (req == null) throw new KeyNotFoundException("Transaction not found.");

        _context.AssetRequests.Remove(req);
        await _context.SaveChangesAsync();
    }

    private async Task<int> CreateAllocationRequestAsync(CreateAllocationRequestDTO dto, int typeId)
    {
        if (dto.Amount <= 0) throw new InvalidOperationException("Số tiền không hợp lệ.");

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
            UserId = 1,
            CreatedBy = 1,
            RequestTypeId = typeId,
            Title = dto.Name,
            Description = "Budget allocation auto-generated request",
            ProposedData = JsonSerializer.Serialize(proposedObj),
            Status = 1,
            CreateDate = dto.Date == default ? DateTime.UtcNow : dto.Date,
            StepId = 0
        };

        _context.AssetRequests.Add(assetRequest);
        await _context.SaveChangesAsync();
        return assetRequest.AssetRequestId;
    }
}
