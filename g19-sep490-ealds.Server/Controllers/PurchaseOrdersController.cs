using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

/// <summary>Purchase orders (<see cref="Procurement"/> + <see cref="ProcurementLine"/>). Standalone from requisitions (UC).</summary>
[ApiController]
[Route("api/purchase-orders")]
[Authorize(Roles = "ACCOUNTANT")]
public class PurchaseOrdersController : ControllerBase
{
    public const int StatusDraft = -1;

    public const int StatusCreated = 0;

    /// <summary>At least one line has received quantity but PO not fully received.</summary>
    public const int StatusPartiallyReceived = 1;

    public const int StatusCancelled = 2;

    /// <summary>All lines fully received.</summary>
    public const int StatusCompleted = 3;

    private readonly EaldsDbContext _db;

    public PurchaseOrdersController(EaldsDbContext db)
    {
        _db = db;
    }

    private int? GetActorUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claim, out var id) && id > 0)
            return id;
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<PurchaseOrderListResponseDto>> GetList(
        [FromQuery] int? procurementId,
        [FromQuery] int? supplierId,
        [FromQuery] int? status,
        [FromQuery] bool receivingEligible = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var q = _db.Procurements.AsNoTracking().AsQueryable();

        if (procurementId.HasValue)
            q = q.Where(p => p.ProcurementId == procurementId.Value);

        if (supplierId.HasValue)
            q = q.Where(p => p.SupplierId == supplierId.Value);

        if (status.HasValue)
            q = q.Where(p => p.Status == status.Value);

        if (receivingEligible)
            q = q.Where(p => p.Status != StatusCancelled && p.Status != StatusCompleted);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(p => p.CreateDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseOrderListItemDto
            {
                ProcurementId = p.ProcurementId,
                AssetRequestId = p.AssetRequestId,
                SupplierId = p.SupplierId ?? 0,
                SupplierName = p.Supplier != null ? p.Supplier.Name : null,
                ContractNo = p.ContractNo,
                Title = p.Title,
                Currency = p.Currency,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                CreateDate = p.CreateDate,
            })
            .ToListAsync();

        return Ok(new PurchaseOrderListResponseDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(int id)
    {
        var header = await _db.Procurements
            .AsNoTracking()
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProcurementId == id);
        if (header == null)
            return NotFound();

        var lines = await _db.ProcurementLines
            .AsNoTracking()
            .Include(l => l.Asset)
            .Where(l => l.ProcurementId == id)
            .OrderBy(l => l.LineIndex)
            .Select(l => new PurchaseOrderLineItemDto
            {
                LineId = l.LineId,
                LineIndex = l.LineIndex,
                Description = l.Description,
                AssetId = l.AssetId,
                AssetCode = l.Asset != null ? l.Asset.Code : null,
                AssetName = l.Asset != null ? l.Asset.Name : null,
                Quantity = l.Quantity,
                Unit = l.Unit,
                UnitPrice = l.UnitPrice,
                ExpectedDeliveryDate = l.ExpectedDeliveryDate,
                LineTotal = l.Quantity * l.UnitPrice,
                ReceivedQuantity = l.ReceivedQuantity,
                OpenQuantity = l.Quantity - l.ReceivedQuantity,
            })
            .ToListAsync();

        var dto = new PurchaseOrderDetailDto
        {
            ProcurementId = header.ProcurementId,
            AssetRequestId = header.AssetRequestId,
            SupplierId = header.SupplierId ?? 0,
            SupplierName = header.Supplier?.Name,
            ContractNo = header.ContractNo,
            Title = header.Title,
            Currency = header.Currency,
            TotalAmount = header.TotalAmount,
            Status = header.Status,
            CreateDate = header.CreateDate,
            Lines = lines,
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] PurchaseOrderCreateDto dto)
    {
        if (dto == null)
            return BadRequest("Body required.");
        if (dto.SupplierId <= 0)
            return BadRequest("Supplier is required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            return BadRequest("At least one line item is required.");

        var supplierExists = await _db.Suppliers.AsNoTracking().AnyAsync(s => s.SupplierId == dto.SupplierId);
        if (!supplierExists)
            return BadRequest("Supplier not found.");

        if (dto.AssetRequestId.HasValue)
        {
            var arOk = await _db.AssetRequests.AsNoTracking()
                .AnyAsync(a => a.AssetRequestId == dto.AssetRequestId.Value);
            if (!arOk)
                return BadRequest("Linked requisition (AssetRequest) not found.");
        }

        foreach (var line in dto.Lines)
        {
            if (line.Quantity <= 0)
                return BadRequest("Each line must have quantity greater than zero.");
            if (line.AssetId.HasValue && line.AssetId.Value > 0)
            {
                var assetOk = await _db.Assets.AsNoTracking().AnyAsync(a => a.AssetId == line.AssetId.Value);
                if (!assetOk)
                    return BadRequest($"AssetId {line.AssetId} not found.");
            }
        }

        var userId = GetActorUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var currency = string.IsNullOrWhiteSpace(dto.Currency) ? "VND" : dto.Currency.Trim().ToUpperInvariant();
        if (currency.Length > 10)
            return BadRequest("Currency code is too long.");

        var title = dto.Lines
            .Select(l => l.Description?.Trim())
            .FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "Purchase order";

        var total = dto.Lines.Sum(l => l.Quantity * l.UnitPrice);

        var contractNo = string.IsNullOrWhiteSpace(dto.ContractNo)
            ? string.Empty
            : dto.ContractNo.Trim();

        var entity = new Procurement
        {
            AssetRequestId = dto.AssetRequestId,
            SupplierId = dto.SupplierId,
            ContractNo = contractNo,
            ContractDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Title = title.Length > 255 ? title[..255] : title,
            Currency = currency,
            TotalAmount = total,
            AdvanceAmount = 0,
            RemainingAmount = total,
            Status = dto.IsDraft ? StatusDraft : StatusCreated,
            CreatedBy = userId.Value,
            CreateDate = DateTime.UtcNow,
        };

        _db.Procurements.Add(entity);
        await _db.SaveChangesAsync();

        if (string.IsNullOrEmpty(entity.ContractNo))
        {
            entity.ContractNo = $"PO-{entity.ProcurementId}";
        }
        var idx = 0;
        foreach (var line in dto.Lines)
        {
            _db.ProcurementLines.Add(new ProcurementLine
            {
                ProcurementId = entity.ProcurementId,
                LineIndex = idx++,
                Description = string.IsNullOrWhiteSpace(line.Description) ? null : line.Description.Trim(),
                AssetId = line.AssetId is > 0 ? line.AssetId : null,
                Quantity = line.Quantity,
                Unit = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit.Trim(),
                UnitPrice = line.UnitPrice,
                ExpectedDeliveryDate = line.ExpectedDeliveryDate,
            });
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.ProcurementId }, new { procurementId = entity.ProcurementId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PurchaseOrderUpdateDto dto)
    {
        if (dto == null)
            return BadRequest("Body required.");

        var entity = await _db.Procurements
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.ProcurementId == id);
        if (entity == null)
            return NotFound();

        if (entity.Status == StatusCancelled)
            return BadRequest("Cannot edit a cancelled purchase order.");

        if (entity.Lines.Any(l => l.ReceivedQuantity > 0))
            return BadRequest("Cannot edit a purchase order after goods have been received.");

        if (dto.SupplierId > 0)
        {
            var supplierExists = await _db.Suppliers.AsNoTracking().AnyAsync(s => s.SupplierId == dto.SupplierId);
            if (!supplierExists)
                return BadRequest("Supplier not found.");
            entity.SupplierId = dto.SupplierId;
        }

        if (dto.AssetRequestId.HasValue)
        {
            var arOk = await _db.AssetRequests.AsNoTracking()
                .AnyAsync(a => a.AssetRequestId == dto.AssetRequestId.Value);
            if (!arOk)
                return BadRequest("Linked requisition (AssetRequest) not found.");
        }

        var currency = string.IsNullOrWhiteSpace(dto.Currency) ? "VND" : dto.Currency.Trim().ToUpperInvariant();
        if (currency.Length > 10)
            return BadRequest("Currency code is too long.");

        entity.AssetRequestId = dto.AssetRequestId;
        entity.Currency = currency;

        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            await _db.SaveChangesAsync();
            return Ok(new { procurementId = id });
        }

        foreach (var line in dto.Lines)
        {
            if (line.Quantity <= 0)
                return BadRequest("Each line must have quantity greater than zero.");
            if (line.AssetId.HasValue && line.AssetId.Value > 0)
            {
                var assetOk = await _db.Assets.AsNoTracking().AnyAsync(a => a.AssetId == line.AssetId.Value);
                if (!assetOk)
                    return BadRequest($"AssetId {line.AssetId} not found.");
            }
        }

        var title = dto.Lines
            .Select(l => l.Description?.Trim())
            .FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? entity.Title;

        var total = dto.Lines.Sum(l => l.Quantity * l.UnitPrice);

        entity.Title = title.Length > 255 ? title[..255] : title;
        entity.TotalAmount = total;
        entity.RemainingAmount = total;

        _db.ProcurementLines.RemoveRange(entity.Lines);
        var idx = 0;
        foreach (var line in dto.Lines)
        {
            _db.ProcurementLines.Add(new ProcurementLine
            {
                ProcurementId = entity.ProcurementId,
                LineIndex = idx++,
                Description = string.IsNullOrWhiteSpace(line.Description) ? null : line.Description.Trim(),
                AssetId = line.AssetId is > 0 ? line.AssetId : null,
                Quantity = line.Quantity,
                Unit = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit.Trim(),
                UnitPrice = line.UnitPrice,
                ExpectedDeliveryDate = line.ExpectedDeliveryDate,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { procurementId = id });
    }

    public class CancelPurchaseOrderDto
    {
        public string? Comment { get; set; }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelPurchaseOrderDto? _)
    {
        var entity = await _db.Procurements.FirstOrDefaultAsync(p => p.ProcurementId == id);
        if (entity == null)
            return NotFound();
        if (entity.Status == StatusCancelled)
            return BadRequest("Already cancelled.");

        var hasReceipts = await _db.ProcurementLines.AsNoTracking()
            .AnyAsync(l => l.ProcurementId == id && l.ReceivedQuantity > 0);
        if (hasReceipts)
            return BadRequest("Cannot cancel a purchase order that has goods receipts.");

        entity.Status = StatusCancelled;
        await _db.SaveChangesAsync();
        return Ok(new { procurementId = id, status = entity.Status });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Procurements.FirstOrDefaultAsync(p => p.ProcurementId == id);
        if (entity == null)
            return NotFound();

        if (entity.Status != StatusCreated)
            return BadRequest("Only purchase orders in Created status can be deleted.");

        var hasReceipts = await _db.ProcurementLines.AsNoTracking()
            .AnyAsync(l => l.ProcurementId == id && l.ReceivedQuantity > 0);
        if (hasReceipts)
            return BadRequest("Cannot delete a purchase order that has goods receipts.");

        var lines = await _db.ProcurementLines.Where(l => l.ProcurementId == id).ToListAsync();
        if (lines.Count > 0)
            _db.ProcurementLines.RemoveRange(lines);

        _db.Procurements.Remove(entity);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
