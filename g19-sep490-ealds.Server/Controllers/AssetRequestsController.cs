using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/Assets/Requests/purchase")]
public class AssetRequestsController : ControllerBase
{
    private readonly EaldsDbContext _db;
    private readonly int _purchaseRequestTypeId;

    public AssetRequestsController(EaldsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _purchaseRequestTypeId = configuration.GetValue<int>("App:PurchaseRequestTypeId", 1);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int? requestTypeId)
    {
        var typeId = requestTypeId ?? _purchaseRequestTypeId;
        var list = await _db.AssetRequests
            .AsNoTracking()
            .Where(r => r.RequestTypeId == typeId)
            .OrderByDescending(r => r.CreateDate)
            .Select(r => new AssetRequestListItemDTO
            {
                AssetRequestId = r.AssetRequestId,
                Title = r.Title,
                Description = r.Description,
                ProposedData = r.ProposedData,
                Status = r.Status,
                CreateDate = r.CreateDate,
                CreatedBy = r.CreatedBy,
                CreatorName = r.User != null ? r.User.Email : null
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _db.AssetRequests
            .AsNoTracking()
            .Where(r => r.AssetRequestId == id && r.RequestTypeId == _purchaseRequestTypeId)
            .Select(r => new
            {
                r.AssetRequestId,
                r.Title,
                r.Description,
                r.ProposedData,
                r.Status,
                r.CreateDate,
                r.CreatedBy,
                CreatorName = r.User != null ? r.User.Email : null
            })
            .FirstOrDefaultAsync();
        if (request == null)
            return NotFound();
        return Ok(request);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AssetRequestDTO dto)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        var requestTypeExists = await _db.RequestTypes
            .AsNoTracking()
            .AnyAsync(rt => rt.RequestTypeId == _purchaseRequestTypeId);
        if (!requestTypeExists)
        {
            return BadRequest($"Configured purchase RequestTypeId '{_purchaseRequestTypeId}' does not exist in RequestType table.");
        }

        var assetRequest = new AssetRequest
        {
            UserId = dto.UserId,
            RequestTypeId = _purchaseRequestTypeId,
            AssetId = dto.AssetId,
            Title = dto.Title,
            Description = dto.Description,
            ProposedData = dto.ProposedData,
            Status = 0,
            CreatedBy = dto.CreatedBy,
            CreateDate = DateTime.UtcNow,
            StepId = 0
        };

        _db.AssetRequests.Add(assetRequest);
        await _db.SaveChangesAsync();

        var userRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == dto.CreatedBy);
        var actionRoleId = userRole?.RoleId ?? 1;

        var record = new AssetRequestRecord
        {
            AssetRequestId = assetRequest.AssetRequestId,
            FromStatus = 0,
            ToStatus = 0,
            Action = 0,
            ActionByUserId = dto.CreatedBy,
            ActionRoleId = actionRoleId,
            Comment = "Created request",
            OccurredAt = DateTime.UtcNow
        };

        _db.AssetRequestRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { assetRequest.AssetRequestId });
    }
}
