using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class SupplierInvoiceService : ISupplierInvoiceService
{
    public const int StatusActive = 0;
    public const int StatusCancelled = 1;
    private const int DocumentTypeSupplierInvoiceAttachment = 51;

    private readonly EaldsDbContext _context;
    private readonly ILogger<SupplierInvoiceService> _logger;

    public SupplierInvoiceService(EaldsDbContext context, ILogger<SupplierInvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SupplierInvoiceListResponseDto> GetListAsync(
        string? invoiceNumber, int? supplierId,
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var q = _context.SupplierInvoices.AsNoTracking().AsQueryable();

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

        return new SupplierInvoiceListResponseDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        };
    }

    public async Task<SupplierInvoiceDetailDto> GetByIdAsync(int id)
    {
        var header = await _context.SupplierInvoices.AsNoTracking()
            .Include(i => i.Procurement).ThenInclude(p => p.Supplier!)
            .FirstOrDefaultAsync(i => i.SupplierInvoiceId == id);

        if (header == null)
            throw new KeyNotFoundException($"Supplier invoice {id} not found.");

        var lines = await _context.SupplierInvoiceLines.AsNoTracking()
            .Include(l => l.ProcurementLine).ThenInclude(pl => pl!.Asset!)
            .Include(l => l.GoodsReceiptLine).ThenInclude(gl => gl!.Asset!)
            .Where(l => l.SupplierInvoiceId == id)
            .OrderBy(l => l.SupplierInvoiceLineId)
            .ToListAsync();

        var lineDtos = lines.Select(l =>
        {
            var pl = l.ProcurementLine;
            var assetId = l.GoodsReceiptLine?.AssetId ?? pl?.AssetId;
            var code = l.GoodsReceiptLine?.Asset?.Code ?? pl?.Asset?.Code;
            var name = l.GoodsReceiptLine?.Asset?.Name ?? pl?.Asset?.Name;
            return new SupplierInvoiceDetailLineDto
            {
                SupplierInvoiceLineId = l.SupplierInvoiceLineId,
                ProcurementLineId = l.ProcurementLineId,
                GoodsReceiptLineId = l.GoodsReceiptLineId,
                ChargeDescription = l.ChargeDescription,
                AssetId = assetId,
                AssetCode = code,
                AssetName = name,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
            };
        }).ToList();

        var attachments = await _context.Documents.AsNoTracking()
            .Where(d => d.SupplierInvoiceId == id)
            .OrderBy(d => d.DocumentId)
            .Select(d => new DocumentAttachmentDto { DocumentId = d.DocumentId, FileUrl = d.FileUrl })
            .ToListAsync();

        return new SupplierInvoiceDetailDto
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
            Attachments = attachments,
            Lines = lineDtos,
        };
    }

    public async Task<int> CreateAsync(int userId, SupplierInvoiceCreateDto dto)
    {
        if (dto == null)
            throw new InvalidOperationException("Body required.");
        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
            throw new InvalidOperationException("Invoice number is required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new InvalidOperationException("At least one line is required.");

        var invNo = dto.InvoiceNumber.Trim();
        if (invNo.Length > 100)
            throw new InvalidOperationException("Invoice number is too long.");

        var procurement = await _context.Procurements
            .Include(p => p.Supplier)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.ProcurementId == dto.ProcurementId);
        if (procurement == null)
            throw new KeyNotFoundException("Purchase order not found.");
        if (procurement.Status == PurchaseOrderService.StatusCancelled)
            throw new InvalidOperationException("Cannot create an invoice for a cancelled purchase order.");
        if (procurement.SupplierId is null or <= 0)
            throw new InvalidOperationException("Purchase order has no supplier.");

        var dup = await _context.SupplierInvoices.AsNoTracking()
            .AnyAsync(i =>
                i.Status == StatusActive
                && i.Procurement.SupplierId == procurement.SupplierId
                && i.InvoiceNumber == invNo);
        if (dup)
            throw new InvalidOperationException("An active invoice with this number already exists for this supplier.");

        GoodsReceipt? goodsReceipt = null;
        IReadOnlyDictionary<int, GoodsReceiptLine>? grLineById = null;
        if (dto.GoodsReceiptId is > 0)
        {
            var grId = dto.GoodsReceiptId.Value;
            goodsReceipt = await _context.GoodsReceipts
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.GoodsReceiptId == grId);
            if (goodsReceipt == null)
                throw new KeyNotFoundException("Goods receipt not found.");
            if (goodsReceipt.ProcurementId != dto.ProcurementId)
                throw new InvalidOperationException("Goods receipt does not belong to the selected purchase order.");
            grLineById = goodsReceipt.Lines.ToDictionary(l => l.GoodsReceiptLineId);
        }

        var plById = procurement.Lines.ToDictionary(l => l.LineId);

        var normalized = new List<(int? ProcurementLineId, string? ChargeDescription, int? GoodsReceiptLineId, decimal Quantity, decimal UnitPrice, decimal LineTotal)>();
        var hasPositivePoLine = false;

        foreach (var row in dto.Lines)
        {
            var plId = row.ProcurementLineId;
            var isMisc = plId is null or <= 0;

            if (isMisc)
            {
                if (dto.GoodsReceiptId is > 0)
                    throw new InvalidOperationException("Ad-hoc charge lines are not supported when invoicing from a goods receipt.");

                var desc = string.IsNullOrWhiteSpace(row.ChargeDescription) ? null : row.ChargeDescription.Trim();
                if (string.IsNullOrEmpty(desc))
                    throw new InvalidOperationException("Each ad-hoc charge line must have a description (charge name).");
                if (desc.Length > 500)
                    throw new InvalidOperationException("Charge description is too long.");
                if (row.GoodsReceiptLineId.HasValue)
                    throw new InvalidOperationException("Ad-hoc charge lines cannot reference a goods receipt line.");
                if (row.Quantity <= 0)
                    throw new InvalidOperationException("Ad-hoc charge line: quantity must be positive.");
                if (row.UnitPrice < 0)
                    throw new InvalidOperationException("Ad-hoc charge line: unit price cannot be negative.");

                var miscTotal = RoundMoney(row.Quantity * row.UnitPrice);
                if (miscTotal <= 0)
                    continue;

                normalized.Add((null, desc, null, row.Quantity, row.UnitPrice, miscTotal));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.ChargeDescription))
                throw new InvalidOperationException($"Procurement line {plId}: charge description must be empty for order lines.");

            if (!plById.TryGetValue(plId!.Value, out var pl))
                throw new InvalidOperationException($"Procurement line {plId} is not on this order.");
            if (pl.ProcurementId != dto.ProcurementId)
                throw new InvalidOperationException($"Procurement line {plId} does not match order.");

            if (row.Quantity <= 0)
                throw new InvalidOperationException($"Line {plId}: quantity must be positive.");
            if (row.UnitPrice < 0)
                throw new InvalidOperationException($"Line {plId}: unit price cannot be negative.");

            if (dto.GoodsReceiptId is > 0)
            {
                if (row.GoodsReceiptLineId is null or <= 0)
                    throw new InvalidOperationException("Each line must reference a goods receipt line when a goods receipt is selected.");
                var grlId = row.GoodsReceiptLineId.Value;
                if (grLineById == null || !grLineById.TryGetValue(grlId, out var grl))
                    throw new InvalidOperationException($"Goods receipt line {grlId} not found.");
                if (grl.ProcurementLineId != plId)
                    throw new InvalidOperationException($"Goods receipt line {grlId} does not match procurement line {plId}.");
                if (row.Quantity > grl.QuantityReceived)
                    throw new InvalidOperationException($"Line {plId}: quantity cannot exceed quantity on the goods receipt ({grl.QuantityReceived}).");
            }
            else
            {
                if (row.GoodsReceiptLineId.HasValue)
                    throw new InvalidOperationException("Goods receipt line must be empty when no goods receipt is selected.");
                if (row.Quantity > pl.Quantity)
                    throw new InvalidOperationException($"Line {plId}: quantity cannot exceed ordered quantity ({pl.Quantity}).");
            }

            hasPositivePoLine = true;
            var lineTotal = RoundMoney(row.Quantity * row.UnitPrice);
            normalized.Add((plId, null, dto.GoodsReceiptId is > 0 ? row.GoodsReceiptLineId : null, row.Quantity, row.UnitPrice, lineTotal));
        }

        if (!hasPositivePoLine)
            throw new InvalidOperationException("Enter quantity > 0 for at least one purchase order line.");

        if (normalized.Count == 0)
            throw new InvalidOperationException("At least one invoice line with amount > 0 is required.");

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
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow,
        };

        _context.SupplierInvoices.Add(entity);
        await _context.SaveChangesAsync();

        foreach (var url in NormalizeAttachmentFileUrls(dto.AttachmentFileUrls))
        {
            _context.Documents.Add(new Document
            {
                FileUrl = url,
                DocumentType = DocumentTypeSupplierInvoiceAttachment,
                UploadedBy = userId,
                UploadedDate = DateTime.UtcNow,
                SupplierInvoiceId = entity.SupplierInvoiceId,
            });
        }

        foreach (var row in normalized)
        {
            _context.SupplierInvoiceLines.Add(new SupplierInvoiceLine
            {
                SupplierInvoiceId = entity.SupplierInvoiceId,
                ProcurementLineId = row.ProcurementLineId,
                ChargeDescription = row.ChargeDescription,
                GoodsReceiptLineId = dto.GoodsReceiptId is > 0 ? row.GoodsReceiptLineId : null,
                Quantity = row.Quantity,
                UnitPrice = row.UnitPrice,
                LineTotal = row.LineTotal,
            });
        }

        await _context.SaveChangesAsync();

        return entity.SupplierInvoiceId;
    }

    public async Task CancelAsync(int id)
    {
        var inv = await _context.SupplierInvoices.FirstOrDefaultAsync(i => i.SupplierInvoiceId == id);
        if (inv == null)
            throw new KeyNotFoundException($"Supplier invoice {id} not found.");
        if (inv.Status == StatusCancelled)
            throw new InvalidOperationException("Invoice is already cancelled.");

        inv.Status = StatusCancelled;
        await _context.SaveChangesAsync();
    }

    private static IReadOnlyList<string> NormalizeAttachmentFileUrls(IReadOnlyList<string>? urls)
    {
        if (urls == null || urls.Count == 0)
            return Array.Empty<string>();
        return urls
            .Select(u => u?.Trim())
            .Where(u => !string.IsNullOrEmpty(u) && u!.Length <= 2000)
            .Distinct(StringComparer.Ordinal)
            .Take(20)
            .ToList()!;
    }

    private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
