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

/// <summary>Supplier invoices per UC3 (reference PO or goods receipt).</summary>
[ApiController]
[Route("api/supplier-invoices")]
[Authorize(Roles = "ACCOUNTANT")]
public class SupplierInvoicesController : ControllerBase
{
    public const int StatusActive = 0;
    public const int StatusCancelled = 1;

    private readonly EaldsDbContext _db;

    public SupplierInvoicesController(EaldsDbContext db)
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

    private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    [HttpGet]
    public async Task<ActionResult<SupplierInvoiceListResponseDto>> GetList(
        [FromQuery] string? invoiceNumber,
        [FromQuery] int? supplierId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var q = _db.SupplierInvoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            var term = invoiceNumber.Trim();
            q = q.Where(i => i.InvoiceNumber.Contains(term));
        }

        if (supplierId.HasValue)
            q = q.Where(i => i.Procurement.SupplierId == supplierId.Value);

        if (dateFrom.HasValue)
        {
            var df = DateOnly.FromDateTime(dateFrom.Value.Date);
            q = q.Where(i => i.InvoiceDate >= df);
        }

        if (dateTo.HasValue)
        {
            var dt = DateOnly.FromDateTime(dateTo.Value.Date);
            q = q.Where(i => i.InvoiceDate <= dt);
        }

        var total = await q.CountAsync();

        var pageRows = await q
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.SupplierInvoiceId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.SupplierInvoiceId,
                i.InvoiceNumber,
                SupplierId = i.Procurement.SupplierId ?? 0,
                SupplierName = i.Procurement.Supplier != null ? i.Procurement.Supplier.Name : null,
                i.TotalAmount,
                i.InvoiceDate,
                i.Status,
                i.ProcurementId,
                i.GoodsReceiptId,
            })
            .ToListAsync();

        var items = pageRows.Select(i => new SupplierInvoiceListItemDto
        {
            SupplierInvoiceId = i.SupplierInvoiceId,
            InvoiceNumber = i.InvoiceNumber,
            SupplierId = i.SupplierId,
            SupplierName = i.SupplierName,
            TotalAmount = i.TotalAmount,
            InvoiceDate = i.InvoiceDate,
            Status = i.Status,
            ProcurementId = i.ProcurementId,
            GoodsReceiptId = i.GoodsReceiptId,
        }).ToList();

        return Ok(new SupplierInvoiceListResponseDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierInvoiceDetailDto>> GetById(int id)
    {
        var header = await _db.SupplierInvoices.AsNoTracking()
            .Include(i => i.Procurement).ThenInclude(p => p.Supplier!)
            .FirstOrDefaultAsync(i => i.SupplierInvoiceId == id);
        if (header == null)
            return NotFound();

        var lines = await _db.SupplierInvoiceLines.AsNoTracking()
            .Include(l => l.ProcurementLine).ThenInclude(pl => pl.Asset!)
            .Include(l => l.GoodsReceiptLine).ThenInclude(gl => gl!.Asset!)
            .Where(l => l.SupplierInvoiceId == id)
            .OrderBy(l => l.SupplierInvoiceLineId)
            .ToListAsync();

        var lineDtos = lines.Select(l =>
        {
            var pl = l.ProcurementLine;
            var assetId = l.GoodsReceiptLine?.AssetId ?? pl.AssetId;
            var code = l.GoodsReceiptLine?.Asset?.Code ?? pl.Asset?.Code;
            var name = l.GoodsReceiptLine?.Asset?.Name ?? pl.Asset?.Name;
            return new SupplierInvoiceDetailLineDto
            {
                SupplierInvoiceLineId = l.SupplierInvoiceLineId,
                ProcurementLineId = l.ProcurementLineId,
                GoodsReceiptLineId = l.GoodsReceiptLineId,
                AssetId = assetId,
                AssetCode = code,
                AssetName = name,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
            };
        }).ToList();

        return Ok(new SupplierInvoiceDetailDto
        {
            SupplierInvoiceId = header.SupplierInvoiceId,
            InvoiceNumber = header.InvoiceNumber,
            SupplierId = header.Procurement.SupplierId ?? 0,
            SupplierName = header.Procurement.Supplier?.Name,
            InvoiceDate = header.InvoiceDate,
            Currency = header.Currency,
            TotalAmount = header.TotalAmount,
            Note = header.Note,
            Status = header.Status,
            ProcurementId = header.ProcurementId,
            GoodsReceiptId = header.GoodsReceiptId,
            CreatedDate = header.CreatedDate,
            Lines = lineDtos,
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] SupplierInvoiceCreateDto dto)
    {
        if (dto == null)
            return BadRequest("Body required.");
        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
            return BadRequest("Invoice number is required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            return BadRequest("At least one line is required.");

        var userId = GetActorUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var invNo = dto.InvoiceNumber.Trim();
        if (invNo.Length > 100)
            return BadRequest("Invoice number is too long.");

        var procurement = await _db.Procurements
            .Include(p => p.Supplier)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.ProcurementId == dto.ProcurementId);
        if (procurement == null)
            return NotFound("Purchase order not found.");
        if (procurement.Status == PurchaseOrdersController.StatusCancelled)
            return BadRequest("Cannot create an invoice for a cancelled purchase order.");
        if (procurement.SupplierId is null or <= 0)
            return BadRequest("Purchase order has no supplier.");

        var dup = await _db.SupplierInvoices.AsNoTracking()
            .AnyAsync(i =>
                i.Status == StatusActive
                && i.Procurement.SupplierId == procurement.SupplierId
                && i.InvoiceNumber == invNo);
        if (dup)
            return BadRequest("An active invoice with this number already exists for this supplier.");

        GoodsReceipt? goodsReceipt = null;
        IReadOnlyDictionary<int, GoodsReceiptLine>? grLineById = null;
        if (dto.GoodsReceiptId is > 0)
        {
            var grId = dto.GoodsReceiptId.Value;
            goodsReceipt = await _db.GoodsReceipts
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.GoodsReceiptId == grId);
            if (goodsReceipt == null)
                return NotFound("Goods receipt not found.");
            if (goodsReceipt.ProcurementId != dto.ProcurementId)
                return BadRequest("Goods receipt does not belong to the selected purchase order.");
            grLineById = goodsReceipt.Lines.ToDictionary(l => l.GoodsReceiptLineId);
        }

        var plById = procurement.Lines.ToDictionary(l => l.LineId);

        var normalized = new List<(SupplierInvoiceCreateLineDto Row, decimal LineTotal)>();
        foreach (var row in dto.Lines)
        {
            if (!plById.TryGetValue(row.ProcurementLineId, out var pl))
                return BadRequest($"Procurement line {row.ProcurementLineId} is not on this order.");
            if (pl.ProcurementId != dto.ProcurementId)
                return BadRequest($"Procurement line {row.ProcurementLineId} does not match order.");

            if (row.Quantity <= 0)
                return BadRequest($"Line {row.ProcurementLineId}: quantity must be positive.");
            if (row.UnitPrice < 0)
                return BadRequest($"Line {row.ProcurementLineId}: unit price cannot be negative.");

            if (dto.GoodsReceiptId is > 0)
            {
                if (row.GoodsReceiptLineId is null or <= 0)
                    return BadRequest("Each line must reference a goods receipt line when a goods receipt is selected.");
                var grlId = row.GoodsReceiptLineId.Value;
                if (grLineById == null || !grLineById.TryGetValue(grlId, out var grl))
                    return BadRequest($"Goods receipt line {grlId} not found.");
                if (grl.ProcurementLineId != row.ProcurementLineId)
                    return BadRequest($"Goods receipt line {grlId} does not match procurement line {row.ProcurementLineId}.");
                if (row.Quantity > grl.QuantityReceived)
                    return BadRequest($"Line {row.ProcurementLineId}: quantity cannot exceed quantity on the goods receipt ({grl.QuantityReceived}).");
            }
            else
            {
                if (row.GoodsReceiptLineId.HasValue)
                    return BadRequest("Goods receipt line must be empty when no goods receipt is selected.");
                if (row.Quantity > pl.Quantity)
                    return BadRequest($"Line {row.ProcurementLineId}: quantity cannot exceed ordered quantity ({pl.Quantity}).");
            }

            var lineTotal = RoundMoney(row.Quantity * row.UnitPrice);
            normalized.Add((row, lineTotal));
        }

        var totalAmount = normalized.Sum(x => x.LineTotal);

        var entity = new SupplierInvoice
        {
            ProcurementId = procurement.ProcurementId,
            GoodsReceiptId = dto.GoodsReceiptId is > 0 ? dto.GoodsReceiptId.Value : null,
            InvoiceNumber = invNo,
            InvoiceDate = dto.InvoiceDate,
            Currency = string.IsNullOrWhiteSpace(procurement.Currency) ? "VND" : procurement.Currency.Trim(),
            TotalAmount = totalAmount,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            Status = StatusActive,
            CreatedBy = userId.Value,
            CreatedDate = DateTime.UtcNow,
        };

        _db.SupplierInvoices.Add(entity);
        await _db.SaveChangesAsync();

        foreach (var (row, lineTotal) in normalized)
        {
            _db.SupplierInvoiceLines.Add(new SupplierInvoiceLine
            {
                SupplierInvoiceId = entity.SupplierInvoiceId,
                ProcurementLineId = row.ProcurementLineId,
                GoodsReceiptLineId = dto.GoodsReceiptId is > 0 ? row.GoodsReceiptLineId!.Value : null,
                Quantity = row.Quantity,
                UnitPrice = row.UnitPrice,
                LineTotal = lineTotal,
            });
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.SupplierInvoiceId }, new { supplierInvoiceId = entity.SupplierInvoiceId });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id)
    {
        var userId = GetActorUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var inv = await _db.SupplierInvoices.FirstOrDefaultAsync(i => i.SupplierInvoiceId == id);
        if (inv == null)
            return NotFound();
        if (inv.Status == StatusCancelled)
            return BadRequest("Invoice is already cancelled.");

        inv.Status = StatusCancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
