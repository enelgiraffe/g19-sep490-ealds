using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class PurchaseOrderService : IPurchaseOrderService
{
    public const int StatusDraft = -1;
    public const int StatusCreated = 0;
    public const int StatusPartiallyReceived = 1;
    public const int StatusCancelled = 2;
    public const int StatusCompleted = 3;

    private readonly EaldsDbContext _context;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(EaldsDbContext context, ILogger<PurchaseOrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PurchaseOrderListResponseDto> GetListAsync(
        int? procurementId, int? supplierId, int? status,
        bool receivingEligible, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var q = _context.Procurements.AsNoTracking().AsQueryable();

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

        return new PurchaseOrderListResponseDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        };
    }

    public async Task<PurchaseOrderDetailDto> GetByIdAsync(int id)
    {
        var header = await _context.Procurements
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.AssetRequest)
            .FirstOrDefaultAsync(p => p.ProcurementId == id);

        if (header == null)
            throw new KeyNotFoundException($"Purchase order {id} not found.");

        var lines = await _context.ProcurementLines
            .AsNoTracking()
            .Where(l => l.ProcurementId == id)
            .OrderBy(l => l.LineIndex)
            .Select(l => new PurchaseOrderLineItemDto
            {
                LineId = l.LineId,
                LineIndex = l.LineIndex,
                Description = l.Description,
                AssetTypeId = l.AssetId != null && l.Asset != null ? l.Asset.AssetTypeId : null,
                AssetTypeName = l.Asset != null && l.Asset.AssetType != null ? l.Asset.AssetType.Name : null,
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

        return new PurchaseOrderDetailDto
        {
            ProcurementId = header.ProcurementId,
            AssetRequestId = header.AssetRequestId,
            SupplierId = header.SupplierId ?? 0,
            SupplierName = header.Supplier?.Name,
            ContractNo = header.ContractNo,
            Title = header.Title,
            AssetRequestTitle = header.AssetRequest != null ? header.AssetRequest.Title : null,
            Currency = header.Currency,
            TotalAmount = header.TotalAmount,
            Status = header.Status,
            CreateDate = header.CreateDate,
            Lines = lines,
        };
    }

    public async Task<int> CreateAsync(int userId, PurchaseOrderCreateDto dto)
    {
        if (dto == null)
            throw new InvalidOperationException("Body required.");
        if (dto.SupplierId <= 0)
            throw new InvalidOperationException("Supplier is required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new InvalidOperationException("At least one line item is required.");

        var supplierExists = await _context.Suppliers.AsNoTracking().AnyAsync(s => s.SupplierId == dto.SupplierId);
        if (!supplierExists)
            throw new InvalidOperationException("Supplier not found.");

        if (dto.AssetRequestId.HasValue)
        {
            var arOk = await _context.AssetRequests.AsNoTracking()
                .AnyAsync(a => a.AssetRequestId == dto.AssetRequestId.Value);
            if (!arOk)
                throw new InvalidOperationException("Linked requisition (AssetRequest) not found.");
        }

        foreach (var line in dto.Lines)
        {
            if (line.Quantity <= 0)
                throw new InvalidOperationException("Each line must have quantity greater than zero.");
            if (line.AssetId.HasValue && line.AssetId.Value > 0)
            {
                var assetOk = await _context.Assets.AsNoTracking().AnyAsync(a => a.AssetId == line.AssetId.Value);
                if (!assetOk)
                    throw new InvalidOperationException($"AssetId {line.AssetId} not found.");
            }
        }

        var currency = string.IsNullOrWhiteSpace(dto.Currency) ? "VND" : dto.Currency.Trim().ToUpperInvariant();
        if (currency.Length > 10)
            throw new InvalidOperationException("Currency code is too long.");

        var title = dto.Lines
            .Select(l => l.Description?.Trim())
            .FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "Purchase order";

        var total = dto.Lines.Sum(l => l.Quantity * l.UnitPrice);

        var contractNo = string.IsNullOrWhiteSpace(dto.ContractNo)
            ? string.Empty
            : dto.ContractNo.Trim();

        if (!dto.IsDraft && string.IsNullOrEmpty(contractNo))
            throw new InvalidOperationException("Số chứng từ là bắt buộc khi tạo đơn (không phải nháp).");

        if (contractNo.Length > 100)
            throw new InvalidOperationException("Số chứng từ quá dài (tối đa 100 ký tự).");

        if (!string.IsNullOrEmpty(contractNo))
        {
            var duplicateContract = await _context.Procurements.AsNoTracking()
                .AnyAsync(p => p.ContractNo == contractNo);
            if (duplicateContract)
                throw new InvalidOperationException("Số chứng từ đã tồn tại.");
        }

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
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
        };

        _context.Procurements.Add(entity);
        await _context.SaveChangesAsync();

        if (string.IsNullOrEmpty(entity.ContractNo))
            entity.ContractNo = $"PO-{entity.ProcurementId}";

        var idx = 0;
        foreach (var line in dto.Lines)
        {
            _context.ProcurementLines.Add(new ProcurementLine
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

        await _context.SaveChangesAsync();

        return entity.ProcurementId;
    }

    public async Task UpdateAsync(int userId, int id, PurchaseOrderUpdateDto dto)
    {
        if (dto == null)
            throw new InvalidOperationException("Body required.");

        var entity = await _context.Procurements
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.ProcurementId == id);

        if (entity == null)
            throw new KeyNotFoundException($"Purchase order {id} not found.");

        if (entity.Status == StatusCancelled)
            throw new InvalidOperationException("Cannot edit a cancelled purchase order.");

        if (entity.Lines.Any(l => l.ReceivedQuantity > 0))
            throw new InvalidOperationException("Cannot edit a purchase order after goods have been received.");

        if (dto.SupplierId > 0)
        {
            var supplierExists = await _context.Suppliers.AsNoTracking().AnyAsync(s => s.SupplierId == dto.SupplierId);
            if (!supplierExists)
                throw new InvalidOperationException("Supplier not found.");
            entity.SupplierId = dto.SupplierId;
        }

        if (dto.AssetRequestId.HasValue)
        {
            var arOk = await _context.AssetRequests.AsNoTracking()
                .AnyAsync(a => a.AssetRequestId == dto.AssetRequestId.Value);
            if (!arOk)
                throw new InvalidOperationException("Linked requisition (AssetRequest) not found.");
        }

        var currency = string.IsNullOrWhiteSpace(dto.Currency) ? "VND" : dto.Currency.Trim().ToUpperInvariant();
        if (currency.Length > 10)
            throw new InvalidOperationException("Currency code is too long.");

        entity.AssetRequestId = dto.AssetRequestId;
        entity.Currency = currency;

        // Validate và cập nhật ContractNo
        var newContractNo = dto.ContractNo != null ? dto.ContractNo.Trim() : entity.ContractNo;
        
        // Nếu đang chuyển từ draft sang created, bắt buộc phải có ContractNo
        if (!dto.IsDraft && entity.Status == StatusDraft && string.IsNullOrEmpty(newContractNo))
            throw new InvalidOperationException("Số chứng từ là bắt buộc khi tạo đơn (không phải nháp).");

        if (!string.IsNullOrEmpty(newContractNo))
        {
            if (newContractNo.Length > 100)
                throw new InvalidOperationException("Số chứng từ quá dài (tối đa 100 ký tự).");
            var duplicateContract = await _context.Procurements.AsNoTracking()
                .AnyAsync(p => p.ProcurementId != id && p.ContractNo == newContractNo);
            if (duplicateContract)
                throw new InvalidOperationException("Số chứng từ đã tồn tại.");
            entity.ContractNo = newContractNo;
        }

        // Cập nhật trạng thái dựa trên IsDraft
        if (dto.IsDraft)
        {
            // Giữ hoặc chuyển về draft
            entity.Status = StatusDraft;
        }
        else if (entity.Status == StatusDraft)
        {
            // Chuyển từ draft sang created
            entity.Status = StatusCreated;
        }

        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            await _context.SaveChangesAsync();
            return;
        }

        foreach (var line in dto.Lines)
        {
            if (line.Quantity <= 0)
                throw new InvalidOperationException("Each line must have quantity greater than zero.");
            if (line.AssetId.HasValue && line.AssetId.Value > 0)
            {
                var assetOk = await _context.Assets.AsNoTracking().AnyAsync(a => a.AssetId == line.AssetId.Value);
                if (!assetOk)
                    throw new InvalidOperationException($"AssetId {line.AssetId} not found.");
            }
        }

        var title = dto.Lines
            .Select(l => l.Description?.Trim())
            .FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? entity.Title;

        var total = dto.Lines.Sum(l => l.Quantity * l.UnitPrice);

        entity.Title = title.Length > 255 ? title[..255] : title;
        entity.TotalAmount = total;
        entity.RemainingAmount = total;

        _context.ProcurementLines.RemoveRange(entity.Lines);

        var idx = 0;
        foreach (var line in dto.Lines)
        {
            _context.ProcurementLines.Add(new ProcurementLine
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

        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int id)
    {
        var entity = await _context.Procurements.FirstOrDefaultAsync(p => p.ProcurementId == id);
        if (entity == null)
            throw new KeyNotFoundException($"Purchase order {id} not found.");
        if (entity.Status == StatusCancelled)
            throw new InvalidOperationException("Already cancelled.");

        var hasReceipts = await _context.ProcurementLines.AsNoTracking()
            .AnyAsync(l => l.ProcurementId == id && l.ReceivedQuantity > 0);
        if (hasReceipts)
            throw new InvalidOperationException("Cannot cancel a purchase order that has goods receipts.");

        entity.Status = StatusCancelled;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Procurements.FirstOrDefaultAsync(p => p.ProcurementId == id);
        if (entity == null)
            throw new KeyNotFoundException($"Purchase order {id} not found.");

        if (entity.Status != StatusDraft && entity.Status != StatusCreated)
            throw new InvalidOperationException("Only purchase orders in Draft or Created status can be deleted.");

        var hasReceipts = await _context.ProcurementLines.AsNoTracking()
            .AnyAsync(l => l.ProcurementId == id && l.ReceivedQuantity > 0);
        if (hasReceipts)
            throw new InvalidOperationException("Cannot delete a purchase order that has goods receipts.");

        var lines = await _context.ProcurementLines.Where(l => l.ProcurementId == id).ToListAsync();
        if (lines.Count > 0)
            _context.ProcurementLines.RemoveRange(lines);

        _context.Procurements.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
