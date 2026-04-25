using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class GoodsReceiptService : IGoodsReceiptService
{
    public const int StatusPosted = 1;
    private const int DocumentTypeGoodsReceiptAttachment = 50;

    private readonly EaldsDbContext _context;
    private readonly ILogger<GoodsReceiptService> _logger;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly IMaintenanceTemplateService _maintenanceTemplates;
    private readonly int _allocationRequestTypeId;

    public GoodsReceiptService(
        EaldsDbContext context,
        ILogger<GoodsReceiptService> logger,
        IAssetRequestNotificationService requestNotifications,
        IMaintenanceTemplateService maintenanceTemplates,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _requestNotifications = requestNotifications;
        _maintenanceTemplates = maintenanceTemplates;
        _allocationRequestTypeId = configuration.GetValue<int>("App:AllocationRequestTypeId", 6);
    }

    public async Task<GoodsReceiptListResponseDto> GetListAsync(
        int? goodsReceiptId, int? procurementId, int? supplierId,
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var q = _context.GoodsReceipts.AsNoTracking().AsQueryable();

        if (goodsReceiptId.HasValue)
            q = q.Where(r => r.GoodsReceiptId == goodsReceiptId.Value);

        if (procurementId.HasValue)
            q = q.Where(r => r.ProcurementId == procurementId.Value);

        if (supplierId.HasValue)
            q = q.Where(r => r.Procurement.SupplierId == supplierId.Value);

        if (dateFrom.HasValue)
            q = q.Where(r => r.CreatedDate >= dateFrom.Value);

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1);
            q = q.Where(r => r.CreatedDate < end);
        }

        var total = await q.CountAsync();

        var pageRows = await q
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.GoodsReceiptId,
                r.ProcurementId,
                ContractNo = r.Procurement.ContractNo,
                SupplierName = r.Procurement.Supplier != null ? r.Procurement.Supplier.Name : null,
                r.CreatedDate,
                r.Status,
                TotalQty = r.Lines.Sum(l => l.QuantityReceived),
            })
            .ToListAsync();

        var items = pageRows.Select(r => new GoodsReceiptListItemDto
        {
            GoodsReceiptId = r.GoodsReceiptId,
            ProcurementId = r.ProcurementId,
            ContractNo = r.ContractNo,
            SupplierName = r.SupplierName,
            TotalReceivedQuantity = r.TotalQty,
            Status = r.Status,
            CreatedDate = r.CreatedDate,
        }).ToList();

        return new GoodsReceiptListResponseDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        };
    }

    public async Task<GoodsReceiptDetailDto> GetByIdAsync(int id)
    {
        var header = await _context.GoodsReceipts.AsNoTracking()
            .Include(r => r.Procurement).ThenInclude(p => p.Supplier!)
            .FirstOrDefaultAsync(r => r.GoodsReceiptId == id);

        if (header == null)
            throw new KeyNotFoundException($"Goods receipt {id} not found.");

        var lines = await _context.GoodsReceiptLines.AsNoTracking()
            .Include(l => l.ProcurementLine)
            .Include(l => l.Asset)
            .Where(l => l.GoodsReceiptId == id)
            .OrderBy(l => l.GoodsReceiptLineId)
            .ToListAsync();

        var lineIds = lines.Select(l => l.GoodsReceiptLineId).ToList();
        var instanceRows = await _context.AssetInstances.AsNoTracking()
            .Where(i => i.GoodsReceiptLineId != null && lineIds.Contains(i.GoodsReceiptLineId.Value))
            .Select(i => new InstanceRow(i.GoodsReceiptLineId!.Value, i.AssetInstanceId, i.InstanceCode, i.SerialNumber))
            .ToListAsync();

        var byLine = instanceRows.GroupBy(x => x.GoodsReceiptLineId).ToDictionary(g => g.Key, g => g.ToList());

        var lineDtos = lines.Select(l =>
        {
            var pl = l.ProcurementLine;
            byLine.TryGetValue(l.GoodsReceiptLineId, out var insts);
            return new GoodsReceiptDetailLineDto
            {
                GoodsReceiptLineId = l.GoodsReceiptLineId,
                ProcurementLineId = l.ProcurementLineId,
                AssetId = l.AssetId,
                AssetCode = l.Asset?.Code,
                AssetName = l.Asset?.Name,
                OrderedQuantity = pl.Quantity,
                QuantityReceivedOnThisReceipt = l.QuantityReceived,
                CumulativeReceivedQuantity = pl.ReceivedQuantity,
                OpenQuantity = pl.Quantity - pl.ReceivedQuantity,
                Instances = (insts ?? new List<InstanceRow>()).Select(i => new GoodsReceiptInstanceDto
                {
                    AssetInstanceId = i.AssetInstanceId,
                    InstanceCode = i.InstanceCode,
                    SerialNumber = i.SerialNumber,
                }).ToList(),
            };
        }).ToList();

        var attachments = await _context.Documents.AsNoTracking()
            .Where(d => d.GoodsReceiptId == id)
            .OrderBy(d => d.DocumentId)
            .Select(d => new DocumentAttachmentDto { DocumentId = d.DocumentId, FileUrl = d.FileUrl })
            .ToListAsync();

        return new GoodsReceiptDetailDto
        {
            GoodsReceiptId = header.GoodsReceiptId,
            ProcurementId = header.ProcurementId,
            ContractNo = header.Procurement.ContractNo,
            SupplierName = header.Procurement.Supplier?.Name,
            CreatedDate = header.CreatedDate,
            Status = header.Status,
            Note = header.Note,
            Attachments = attachments,
            Lines = lineDtos,
        };
    }

    public async Task<int> CreateAsync(int userId, GoodsReceiptCreateDto dto)
    {
        if (dto == null)
            throw new InvalidOperationException("Body required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new InvalidOperationException("At least one line with quantity is required.");

        if (!await _context.Warehouses.AnyAsync(w => w.WarehouseId == dto.WarehouseId))
            throw new InvalidOperationException("Warehouse not found.");

        var normalized = dto.Lines
            .Where(l => l.QuantityReceived > 0)
            .ToList();
        if (normalized.Count == 0)
            throw new InvalidOperationException("Each line must have quantity received greater than zero.");

        var procurement = await _context.Procurements
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.ProcurementId == dto.ProcurementId);
        if (procurement == null)
            throw new KeyNotFoundException("Purchase order not found.");

        if (procurement.Status == PurchaseOrderService.StatusCancelled)
            throw new InvalidOperationException("Cannot receive goods for a cancelled purchase order.");
        if (procurement.Status == PurchaseOrderService.StatusCompleted)
            throw new InvalidOperationException("Purchase order is already fully received.");

        var lineById = procurement.Lines.ToDictionary(l => l.LineId);
        foreach (var row in normalized)
        {
            if (!lineById.TryGetValue(row.ProcurementLineId, out var pl))
                throw new InvalidOperationException($"Procurement line {row.ProcurementLineId} does not belong to this order.");
            var open = pl.Quantity - pl.ReceivedQuantity;
            if (row.QuantityReceived > open)
                throw new InvalidOperationException($"Line {row.ProcurementLineId}: received quantity exceeds open quantity ({open}).");
            if (!IsWholeUnit(row.QuantityReceived))
                throw new InvalidOperationException($"Line {row.ProcurementLineId}: quantity must be a whole number of units for asset instances.");

            var assetId = row.AssetId is > 0 ? row.AssetId : pl.AssetId;
            if (assetId is not > 0)
                throw new InvalidOperationException($"Line {row.ProcurementLineId}: catalog asset is required to generate instances.");

            if (!await _context.Assets.AsNoTracking().AnyAsync(a => a.AssetId == assetId!.Value))
                throw new InvalidOperationException($"Line {row.ProcurementLineId}: asset not found.");

            var n = (int)row.QuantityReceived;
            if (row.InstanceSerialNumbers != null && row.InstanceSerialNumbers.Count != n)
                throw new InvalidOperationException($"Line {row.ProcurementLineId}: instanceSerialNumbers must have {n} entries (or be omitted).");
            if (row.InstanceCodes != null && row.InstanceCodes.Count != n)
                throw new InvalidOperationException($"Line {row.ProcurementLineId}: instanceCodes must have {n} entries (or be omitted).");
        }

        var ownsTx = _context.Database.CurrentTransaction == null;
        await using IDbContextTransaction? tx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            var receipt = new GoodsReceipt
            {
                ProcurementId = procurement.ProcurementId,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId,
                Status = StatusPosted,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            };
            _context.GoodsReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            foreach (var url in NormalizeAttachmentFileUrls(dto.AttachmentFileUrls))
            {
                _context.Documents.Add(new Document
                {
                    FileUrl = url,
                    DocumentType = DocumentTypeGoodsReceiptAttachment,
                    UploadedBy = userId,
                    UploadedDate = DateTime.UtcNow,
                    GoodsReceiptId = receipt.GoodsReceiptId,
                });
            }

            if (_context.ChangeTracker.HasChanges())
                await _context.SaveChangesAsync();

            var grLines = new List<GoodsReceiptLine>();
            foreach (var row in normalized)
            {
                var pl = lineById[row.ProcurementLineId];
                var assetId = row.AssetId is > 0 ? row.AssetId!.Value : pl.AssetId!.Value;
                var grLine = new GoodsReceiptLine
                {
                    GoodsReceiptId = receipt.GoodsReceiptId,
                    ProcurementLineId = pl.LineId,
                    QuantityReceived = row.QuantityReceived,
                    AssetId = assetId,
                };
                grLines.Add(grLine);
                _context.GoodsReceiptLines.Add(grLine);
            }

            await _context.SaveChangesAsync();

            var existingCodes = await _context.AssetInstances.AsNoTracking()
                .Select(i => i.InstanceCode)
                .ToListAsync();

            DateOnly purchaseDate;
            if (!string.IsNullOrWhiteSpace(dto.PostingDate) && DateOnly.TryParse(dto.PostingDate, out var parsed))
                purchaseDate = parsed;
            else
                purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);

            for (var i = 0; i < grLines.Count; i++)
            {
                var grLine = grLines[i];
                var row = normalized[i];
                var pl = lineById[row.ProcurementLineId];
                var asset = await _context.Assets.AsNoTracking().FirstAsync(a => a.AssetId == grLine.AssetId!.Value);
                var n = (int)row.QuantityReceived;

                List<string> codes;
                if (row.InstanceCodes != null && row.InstanceCodes.Any(c => !string.IsNullOrWhiteSpace(c)))
                {
                    codes = new List<string>(n);
                    for (var idx = 0; idx < n; idx++)
                    {
                        var customCode = row.InstanceCodes[idx];
                        if (!string.IsNullOrWhiteSpace(customCode))
                        {
                            var trimmed = customCode.Trim();
                            if (existingCodes.Contains(trimmed))
                                throw new InvalidOperationException($"Line {row.ProcurementLineId}: instance code '{trimmed}' already exists.");
                            codes.Add(trimmed);
                            existingCodes.Add(trimmed);
                        }
                        else
                        {
                            var prefix = $"GRL{grLine.GoodsReceiptLineId}-";
                            if (prefix.Length > 32)
                                prefix = prefix[..32];
                            var generated = GenerateSequentialCodesForPrefix(prefix, 1, existingCodes);
                            codes.Add(generated[0]);
                            existingCodes.Add(generated[0]);
                        }
                    }
                }
                else
                {
                    var prefix = $"GRL{grLine.GoodsReceiptLineId}-";
                    if (prefix.Length > 32)
                        prefix = prefix[..32];
                    codes = GenerateSequentialCodesForPrefix(prefix, n, existingCodes);
                    foreach (var c in codes)
                        existingCodes.Add(c);
                }

                var lineTotal = row.QuantityReceived * pl.UnitPrice;
                var (values, currents) = SplitValueAcrossInstances(lineTotal, n);

                for (var k = 0; k < n; k++)
                {
                    var serial = row.InstanceSerialNumbers?[k];
                    if (serial != null)
                        serial = string.IsNullOrWhiteSpace(serial) ? null : serial.Trim();

                    _context.AssetInstances.Add(new AssetInstance
                    {
                        AssetId = asset.AssetId,
                        GoodsReceiptLineId = grLine.GoodsReceiptLineId,
                        WarehouseId = dto.WarehouseId,
                        DepreciationPolicyId = null,
                        InstanceCode = codes[k],
                        SerialNumber = serial,
                        Status = (int)AssetStatus.Available,
                        InUseDate = null,
                        PurchaseDate = purchaseDate,
                        OriginalPrice = values[k],
                        CurrentValue = currents[k],
                        SupplierId = procurement.SupplierId,
                        ContractNo = procurement.ContractNo,
                        Condition = null,
                        Note = null,
                    });
                }

                pl.ReceivedQuantity += row.QuantityReceived;
            }

            ApplyProcurementReceiptStatus(procurement);
            await _context.SaveChangesAsync();

            var newInstanceIds = await _context.AssetInstances
                .AsNoTracking()
                .Where(i => i.GoodsReceiptLine != null && i.GoodsReceiptLine.GoodsReceiptId == receipt.GoodsReceiptId)
                .Select(i => i.AssetInstanceId)
                .ToListAsync();

            foreach (var instanceId in newInstanceIds)
            {
                try
                {
                    await _maintenanceTemplates.EnsureSchedulesForNewInstanceAsync(instanceId, userId);
                }
                catch
                {
                    // Do not abort goods receipt if maintenance schedule sync fails.
                }
            }

            var promotedIds = await PurchaseLinkedAllocationRequestService.TryPromoteAwaitingGoodsReceiptForProcurementAsync(
                _context,
                procurement.ProcurementId,
                _allocationRequestTypeId);
            foreach (var allocationRequestId in promotedIds)
                await _requestNotifications.NotifyFirstApproversAsync(allocationRequestId);

            if (tx != null)
                await tx.CommitAsync();

            return receipt.GoodsReceiptId;
        }
        catch
        {
            if (tx != null)
                await tx.RollbackAsync();
            throw;
        }
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

    private static bool IsWholeUnit(decimal q) => q > 0 && q == decimal.Truncate(q);

    private static (decimal[] Values, decimal[] Currents) SplitValueAcrossInstances(decimal totalValue, int qty)
    {
        var o = new decimal[qty];
        if (qty == 1)
        {
            o[0] = totalValue;
            return (o, (decimal[])o.Clone());
        }

        var each = Math.Round(totalValue / qty, 2, MidpointRounding.AwayFromZero);
        for (var i = 0; i < qty - 1; i++)
            o[i] = each;
        o[qty - 1] = totalValue - each * (qty - 1);
        return (o, (decimal[])o.Clone());
    }

    private static List<string> GenerateSequentialCodesForPrefix(string prefix, int count, List<string> existingCodes)
    {
        var maxSuffix = 0;
        foreach (var code in existingCodes)
        {
            if (code.Length <= prefix.Length)
                continue;
            if (!code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            var suffix = code[prefix.Length..];
            if (suffix.Length == 0 || !suffix.All(char.IsDigit))
                continue;
            if (int.TryParse(suffix, System.Globalization.NumberStyles.Integer, null, out var n))
                maxSuffix = Math.Max(maxSuffix, n);
        }

        var endNumber = maxSuffix + count;
        var width = Math.Max(2, endNumber.ToString().Length);
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var num = maxSuffix + 1 + i;
            list.Add(prefix + num.ToString().PadLeft(width, '0'));
        }

        return list;
    }

    private static void ApplyProcurementReceiptStatus(Procurement p)
    {
        var lines = p.Lines.ToList();
        if (lines.Count == 0)
            return;

        var allComplete = lines.All(l => l.ReceivedQuantity >= l.Quantity);
        var anyReceived = lines.Any(l => l.ReceivedQuantity > 0);
        if (allComplete)
            p.Status = PurchaseOrderService.StatusCompleted;
        else if (anyReceived)
            p.Status = PurchaseOrderService.StatusPartiallyReceived;
        else
            p.Status = PurchaseOrderService.StatusCreated;
    }

    private sealed record InstanceRow(int GoodsReceiptLineId, int AssetInstanceId, string InstanceCode, string? SerialNumber);
}
