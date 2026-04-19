using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;
using g19_sep490_ealds.Server.Services;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace g19_sep490_ealds.Server.Controllers;

/// <summary>Post goods receipt (biên nhận hàng) per UC2.</summary>
[ApiController]
[Route("api/goods-receipts")]
[Authorize(Roles = "ACCOUNTANT")]
public class GoodsReceiptsController : ControllerBase
{
    public const int GoodsReceiptStatusPosted = 1;

    /// <summary>Document.DocumentType for files attached to a goods receipt.</summary>
    private const int DocumentTypeGoodsReceiptAttachment = 50;

    private readonly EaldsDbContext _db;
    private readonly IAssetRequestNotificationService _requestNotifications;
    private readonly int _allocationRequestTypeId;

    public GoodsReceiptsController(
        EaldsDbContext db,
        IAssetRequestNotificationService requestNotifications,
        IConfiguration configuration)
    {
        _db = db;
        _requestNotifications = requestNotifications;
        _allocationRequestTypeId = configuration.GetValue<int>("App:AllocationRequestTypeId", 6);
    }

    private int? GetActorUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claim, out var id) && id > 0)
            return id;
        return null;
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
            p.Status = PurchaseOrdersController.StatusCompleted;
        else if (anyReceived)
            p.Status = PurchaseOrdersController.StatusPartiallyReceived;
        else
            p.Status = PurchaseOrdersController.StatusCreated;
    }

    [HttpGet]
    public async Task<ActionResult<GoodsReceiptListResponseDto>> GetList(
        [FromQuery] int? goodsReceiptId,
        [FromQuery] int? procurementId,
        [FromQuery] int? supplierId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var q = _db.GoodsReceipts.AsNoTracking().AsQueryable();

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

        return Ok(new GoodsReceiptListResponseDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoodsReceiptDetailDto>> GetById(int id)
    {
        var header = await _db.GoodsReceipts.AsNoTracking()
            .Include(r => r.Procurement).ThenInclude(p => p.Supplier!)
            .FirstOrDefaultAsync(r => r.GoodsReceiptId == id);
        if (header == null)
            return NotFound();

        var lines = await _db.GoodsReceiptLines.AsNoTracking()
            .Include(l => l.ProcurementLine)
            .Include(l => l.Asset)
            .Where(l => l.GoodsReceiptId == id)
            .OrderBy(l => l.GoodsReceiptLineId)
            .ToListAsync();

        var lineIds = lines.Select(l => l.GoodsReceiptLineId).ToList();
        var instanceRows = await _db.AssetInstances.AsNoTracking()
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

        var attachments = await _db.Documents.AsNoTracking()
            .Where(d => d.GoodsReceiptId == id)
            .OrderBy(d => d.DocumentId)
            .Select(d => new DocumentAttachmentDto { DocumentId = d.DocumentId, FileUrl = d.FileUrl })
            .ToListAsync();

        return Ok(new GoodsReceiptDetailDto
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
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] GoodsReceiptCreateDto dto)
    {
        if (dto == null)
            return BadRequest("Body required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            return BadRequest("At least one line with quantity is required.");

        var userId = GetActorUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!await _db.Warehouses.AnyAsync(w => w.WarehouseId == dto.WarehouseId))
            return BadRequest("Warehouse not found.");

        var normalized = dto.Lines
            .Where(l => l.QuantityReceived > 0)
            .ToList();
        if (normalized.Count == 0)
            return BadRequest("Each line must have quantity received greater than zero.");

        var procurement = await _db.Procurements
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.ProcurementId == dto.ProcurementId);
        if (procurement == null)
            return NotFound("Purchase order not found.");

        if (procurement.Status == PurchaseOrdersController.StatusCancelled)
            return BadRequest("Cannot receive goods for a cancelled purchase order.");
        if (procurement.Status == PurchaseOrdersController.StatusCompleted)
            return BadRequest("Purchase order is already fully received.");

        var lineById = procurement.Lines.ToDictionary(l => l.LineId);
        foreach (var row in normalized)
        {
            if (!lineById.TryGetValue(row.ProcurementLineId, out var pl))
                return BadRequest($"Procurement line {row.ProcurementLineId} does not belong to this order.");
            var open = pl.Quantity - pl.ReceivedQuantity;
            if (row.QuantityReceived > open)
                return BadRequest($"Line {row.ProcurementLineId}: received quantity exceeds open quantity ({open}).");
            if (!IsWholeUnit(row.QuantityReceived))
                return BadRequest($"Line {row.ProcurementLineId}: quantity must be a whole number of units for asset instances.");

            var assetId = row.AssetId is > 0 ? row.AssetId : pl.AssetId;
            if (assetId is not > 0)
                return BadRequest($"Line {row.ProcurementLineId}: catalog asset is required to generate instances.");

            if (!await _db.Assets.AsNoTracking().AnyAsync(a => a.AssetId == assetId!.Value))
                return BadRequest($"Line {row.ProcurementLineId}: asset not found.");

            var n = (int)row.QuantityReceived;
            if (row.InstanceSerialNumbers != null && row.InstanceSerialNumbers.Count != n)
                return BadRequest($"Line {row.ProcurementLineId}: instanceSerialNumbers must have {n} entries (or be omitted).");
            if (row.InstanceCodes != null && row.InstanceCodes.Count != n)
                return BadRequest($"Line {row.ProcurementLineId}: instanceCodes must have {n} entries (or be omitted).");
        }

        var ownsTx = _db.Database.CurrentTransaction == null;
        await using IDbContextTransaction? tx = ownsTx ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            var receipt = new GoodsReceipt
            {
                ProcurementId = procurement.ProcurementId,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId.Value,
                Status = GoodsReceiptStatusPosted,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            };
            _db.GoodsReceipts.Add(receipt);
            await _db.SaveChangesAsync();

            foreach (var url in NormalizeAttachmentFileUrls(dto.AttachmentFileUrls))
            {
                _db.Documents.Add(new Document
                {
                    FileUrl = url,
                    DocumentType = DocumentTypeGoodsReceiptAttachment,
                    UploadedBy = userId.Value,
                    UploadedDate = DateTime.UtcNow,
                    GoodsReceiptId = receipt.GoodsReceiptId,
                });
            }

            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync();

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
                _db.GoodsReceiptLines.Add(grLine);
            }

            await _db.SaveChangesAsync();

            var existingCodes = await _db.AssetInstances.AsNoTracking()
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
                var asset = await _db.Assets.AsNoTracking().FirstAsync(a => a.AssetId == grLine.AssetId!.Value);
                var n = (int)row.QuantityReceived;
                
                // Sử dụng instanceCodes nếu có, nếu không thì tự sinh
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
                                return BadRequest($"Line {row.ProcurementLineId}: instance code '{trimmed}' already exists.");
                            codes.Add(trimmed);
                            existingCodes.Add(trimmed);
                        }
                        else
                        {
                            // Nếu code rỗng, tự sinh
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
                    // Tự sinh tất cả
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

                    var inst = new AssetInstance
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
                    };
                    _db.AssetInstances.Add(inst);
                }

                pl.ReceivedQuantity += row.QuantityReceived;
            }

            ApplyProcurementReceiptStatus(procurement);
            await _db.SaveChangesAsync();

            var promotedIds = await PurchaseLinkedAllocationRequestService.TryPromoteAwaitingGoodsReceiptForProcurementAsync(
                _db,
                procurement.ProcurementId,
                _allocationRequestTypeId);
            foreach (var allocationRequestId in promotedIds)
                await _requestNotifications.NotifyFirstApproversAsync(allocationRequestId);

            if (tx != null)
                await tx.CommitAsync();

            return CreatedAtAction(nameof(GetById), new { id = receipt.GoodsReceiptId }, new { goodsReceiptId = receipt.GoodsReceiptId });
        }
        catch
        {
            if (tx != null)
                await tx.RollbackAsync();
            throw;
        }
    }

    private sealed record InstanceRow(int GoodsReceiptLineId, int AssetInstanceId, string InstanceCode, string? SerialNumber);
}
