using g19_sep490_ealds.Server.Events.Command;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models.DTO.RequestDTO;
using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace g19_sep490_ealds.Server.Services.ServiceImplementation;

public class AssetCapitalizationService : IAssetCapitalizationService
{
    private const int PurchaseRequestApprovedStatus = 2;
    private const int PurchaseRequestCapitalizedStatus = 5;

    private readonly EaldsDbContext _context;
    private readonly IAssetCapitalizationMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<AssetCapitalizationService> _logger;

    public AssetCapitalizationService(
        EaldsDbContext context,
        IAssetCapitalizationMapper mapper,
        IMediator mediator,
        ILogger<AssetCapitalizationService> logger)
    {
        _context = context;
        _mapper = mapper;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<AssetCapitalizationResponseDTO> CapitalizeAssetAsync(
        AssetCapitalizationRequestDTO request,
        int userId,
        bool skipPurchaseRequestSideEffects = false)
    {
        AssetInstance? instance = null;
        if (request.AssetInstanceId is int iid && iid > 0)
        {
            instance = await _context.AssetInstances
                .Include(ai => ai.Asset)
                .FirstOrDefaultAsync(ai => ai.AssetInstanceId == iid);
        }
        else if (request.AssetId is int aid && aid > 0)
        {
            instance = await _context.AssetInstances
                .Include(ai => ai.Asset)
                .Where(ai => ai.AssetId == aid)
                .OrderBy(ai => ai.AssetInstanceId)
                .FirstOrDefaultAsync();
        }

        if (instance == null)
            throw new Exception("Asset instance not found");

        if (instance.Status == (int)AssetStatus.Capitalized)
            throw new Exception("Asset is already Capitalized");

        var ownsTransaction = _context.Database.CurrentTransaction == null;
        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync();
        }
        try
        {
            var oldStatus = instance.Status;
            var role = 1;

            if (instance.OriginalPrice <= 30000000m)
            {
                _logger.LogWarning("Tai san khong du dieu kien la TSCD");
            }

            var entity = _mapper.ToEntity(instance.AssetInstanceId, request.Note, userId);
            _context.AssetCapitalizations.Add(entity);

            instance.Status = (int)AssetStatus.Capitalized;

            await _context.SaveChangesAsync();

            await _mediator.Publish(
                new AssetStatusChangedEvent(
                    instance.AssetInstanceId,
                    oldStatus,
                    instance.Status,
                    userId,
                    role
                )
            );

            if (!skipPurchaseRequestSideEffects &&
                request.AssetRequestId.HasValue &&
                request.AssetRequestId.Value > 0)
            {
                await SaveProcurementDocumentsAsync(request.AssetRequestId.Value, request.Documents, userId);
                await MarkRequestAsCapitalizedAsync(request.AssetRequestId.Value, userId);
            }

            if (ownsTransaction && transaction != null)
            {
                await transaction.CommitAsync();
            }

            return _mapper.ToResponse(entity, instance.Asset.AssetId);
        }
        catch
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<AssetCapitalizationResponseDTO> CapitalizeFromPurchaseRequestAsync(
        AssetCapitalizationFromRequestDTO request,
        int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var ar = await _context.AssetRequests.FindAsync(request.AssetRequestId);
            if (ar == null)
                throw new Exception("AssetRequest not found");

            if (ar.Status != PurchaseRequestApprovedStatus)
            {
                if (ar.Status == PurchaseRequestCapitalizedStatus)
                    throw new Exception("Purchase request has already been capitalized.");
                throw new Exception("Only purchase requests with status=2 (Director approved) can be capitalized.");
            }

            await PurchaseRequestLineHelper.EnsureLinesAsync(_context, ar);
            var structuredLineCount = await _context.AssetRequestPurchaseLines
                .CountAsync(l => l.AssetRequestId == ar.AssetRequestId);
            if (structuredLineCount >= 1)
                throw new Exception(
                    "This purchase has structured line items. Use PUT /api/AssetCapitalization/capitalize-purchase-request-lines.");

            if (ar.AssetId.HasValue)
            {
                var existingInstance = await _context.AssetInstances
                    .Where(ai => ai.AssetId == ar.AssetId.Value)
                    .OrderBy(ai => ai.AssetInstanceId)
                    .FirstOrDefaultAsync()
                    ?? throw new Exception("No asset instance for catalog asset.");

                var resultExisting = await CapitalizeAssetAsync(
                    new AssetCapitalizationRequestDTO
                    {
                        AssetInstanceId = existingInstance.AssetInstanceId,
                        Note = request.Note,
                        AssetRequestId = ar.AssetRequestId,
                        Documents = request.Documents
                    },
                    userId,
                    skipPurchaseRequestSideEffects: false);
                await transaction.CommitAsync();
                return resultExisting;
            }

            if (await _context.Assets.AnyAsync(a => a.Code == request.Code))
                throw new Exception("Asset code already exists.");

            var asset = new Asset
            {
                Code = request.Code,
                Name = ResolveAssetNameFromRequest(ar, request.Name),
                AssetTypeId = request.AssetTypeId,
                Status = (int)AssetStatus.Available,
                Unit = request.Unit,
                Quantity = request.Quantity,
                CreatedBy = userId,
                Specification = string.IsNullOrWhiteSpace(request.AssetSpecification) ? null : request.AssetSpecification.Trim(),
                Note = string.IsNullOrWhiteSpace(request.AssetNote) ? null : request.AssetNote.Trim(),
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            var instance = new AssetInstance
            {
                AssetId = asset.AssetId,
                WarehouseId = request.WarehouseId,
                InstanceCode = request.Code,
                PurchaseDate = request.PurchaseDate,
                OriginalPrice = request.OriginalPrice,
                CurrentValue = request.CurrentValue,
                Status = (int)AssetStatus.Available,
                InUseDate = request.PurchaseDate,
            };

            _context.AssetInstances.Add(instance);
            await _context.SaveChangesAsync();

            ar.AssetId = asset.AssetId;
            await _context.SaveChangesAsync();

            var result = await CapitalizeAssetAsync(
                new AssetCapitalizationRequestDTO
                {
                    AssetInstanceId = instance.AssetInstanceId,
                    Note = request.Note,
                    AssetRequestId = ar.AssetRequestId,
                    Documents = request.Documents
                },
                userId,
                skipPurchaseRequestSideEffects: false);

            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<CapitalizePurchaseRequestLinesResponseDTO> CapitalizePurchaseRequestLinesAsync(
        CapitalizePurchaseRequestLinesDTO request,
        int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var ar = await _context.AssetRequests.FindAsync(request.AssetRequestId);
            if (ar == null)
                throw new Exception("AssetRequest not found");

            if (ar.Status != PurchaseRequestApprovedStatus)
            {
                if (ar.Status == PurchaseRequestCapitalizedStatus)
                    throw new Exception("Purchase request has already been capitalized.");
                throw new Exception("Only purchase requests with status=2 (Director approved) can be capitalized.");
            }

            var lines = await PurchaseRequestLineHelper.EnsureLinesAsync(_context, ar);
            if (lines.Count == 0)
                throw new Exception("No purchase line items found. Add equipment lines to the request or use the legacy capitalization endpoint.");

            var pending = lines.Where(l => l.AssetId == null).OrderBy(l => l.LineIndex).ToList();
            if (pending.Count == 0)
                throw new Exception("All lines are already capitalized.");

            if (request.Lines == null || request.Lines.Count != pending.Count)
                throw new Exception("Provide exactly one capitalization input per pending line.");

            for (var i = 0; i < pending.Count; i++)
            {
                if (request.Lines[i].LineId != pending[i].LineId)
                    throw new Exception("Line inputs must match pending lines in order.");
            }

            if (!await _context.Warehouses.AnyAsync(w => w.WarehouseId == request.WarehouseId))
                throw new Exception($"WarehouseId {request.WarehouseId} does not exist.");

            if (!await _context.AssetTypes.AnyAsync(t => t.AssetTypeId == request.AssetTypeId))
                throw new Exception($"AssetTypeId {request.AssetTypeId} does not exist.");

            var purchaseDate = request.PurchaseDate ?? DateOnly.FromDateTime(ar.CreateDate);
            var results = new List<AssetCapitalizationResponseDTO>();
            var firstNewAssetId = (int?)null;

            foreach (var (line, input) in pending.Zip(request.Lines))
            {
                var catPrefix = (input.AssetCatalogPrefix ?? string.Empty).Trim();
                if (!IsValidCodePrefix(catPrefix))
                    throw new Exception("Invalid asset catalog code prefix (letters/digits only, 1–32 characters).");

                var name = string.IsNullOrWhiteSpace(line.ItemName) ? ar.Title : line.ItemName.Trim();
                var qty = line.Quantity < 1 ? 1 : line.Quantity;
                var unit = string.IsNullOrWhiteSpace(line.Unit) ? "Cái" : line.Unit.Trim();
                var lineTotal = ParseEstimatedPriceToDecimal(line.EstimatedPrice) * qty;
                if (lineTotal < 0)
                    lineTotal = 0;

                var instPrefix = (input.InstanceCodePrefix ?? string.Empty).Trim();
                if (qty > 1 && string.IsNullOrWhiteSpace(instPrefix))
                    throw new Exception($"Instance code prefix is required when quantity is greater than 1 (line {line.LineIndex + 1}).");
                if (qty > 1 && !IsValidCodePrefix(instPrefix))
                    throw new Exception("Invalid instance code prefix (letters/digits only, 1–32 characters).");

                var assetId = await CreateCatalogAssetWithInstancesAsync(
                    userId,
                    catPrefix,
                    name,
                    request.AssetTypeId,
                    unit,
                    qty,
                    lineTotal,
                    qty > 1 ? instPrefix : null,
                    request.WarehouseId,
                    purchaseDate,
                    request.AssetSpecification,
                    request.AssetNote);

                line.AssetId = assetId;
                line.CapitalizedAt = DateTime.UtcNow;
                firstNewAssetId ??= assetId;
                await _context.SaveChangesAsync();

                var instances = await _context.AssetInstances
                    .Where(ai => ai.AssetId == assetId)
                    .OrderBy(ai => ai.AssetInstanceId)
                    .ToListAsync();

                foreach (var inst in instances)
                {
                    var cap = await CapitalizeAssetAsync(
                        new AssetCapitalizationRequestDTO
                        {
                            AssetInstanceId = inst.AssetInstanceId,
                            Note = request.Note,
                            AssetRequestId = ar.AssetRequestId,
                            Documents = null,
                        },
                        userId,
                        skipPurchaseRequestSideEffects: true);
                    results.Add(cap);
                }
            }

            if (!ar.AssetId.HasValue && firstNewAssetId.HasValue)
                ar.AssetId = firstNewAssetId;

            await SaveProcurementDocumentsAsync(ar.AssetRequestId, request.Documents, userId);

            var allLinesCapitalized = await _context.AssetRequestPurchaseLines
                .Where(l => l.AssetRequestId == ar.AssetRequestId)
                .AllAsync(l => l.AssetId != null);
            if (allLinesCapitalized)
                await MarkRequestAsCapitalizedAsync(ar.AssetRequestId, userId);

            await transaction.CommitAsync();

            ar = await _context.AssetRequests.AsNoTracking().FirstAsync(x => x.AssetRequestId == ar.AssetRequestId);
            return new CapitalizePurchaseRequestLinesResponseDTO
            {
                AssetRequestId = ar.AssetRequestId,
                Status = ar.Status,
                CapitalizedInstances = results,
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<int> CreateCatalogAssetWithInstancesAsync(
        int userId,
        string assetCatalogPrefix,
        string name,
        int assetTypeId,
        string unit,
        int qty,
        decimal lineTotal,
        string? instanceCodePrefix,
        int warehouseId,
        DateOnly purchaseDate,
        string? assetSpecification,
        string? assetNote)
    {
        var catalogCodes = await GenerateAssetCatalogCodesForPrefixAsync(assetCatalogPrefix, 1);
        var catalogCode = catalogCodes[0];
        if (await _context.Assets.AnyAsync(a => a.Code == catalogCode))
            throw new Exception($"Asset code {catalogCode} already exists.");

        var asset = new Asset
        {
            Code = catalogCode,
            Name = name,
            AssetTypeId = assetTypeId,
            Status = (int)AssetStatus.Available,
            Unit = unit,
            Quantity = qty,
            CreatedBy = userId,
            Specification = string.IsNullOrWhiteSpace(assetSpecification) ? null : assetSpecification.Trim(),
            Note = string.IsNullOrWhiteSpace(assetNote) ? null : assetNote.Trim(),
        };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        List<string> instanceCodes;
        if (qty > 1)
        {
            instanceCodes = await GenerateInstanceCodesForPrefixAsync(instanceCodePrefix!, qty);
            foreach (var code in instanceCodes)
            {
                if (await _context.AssetInstances.AnyAsync(i => i.InstanceCode == code))
                    throw new Exception($"Instance code {code} already exists.");
            }
        }
        else
        {
            if (await _context.AssetInstances.AnyAsync(i => i.InstanceCode == catalogCode))
                throw new Exception($"Instance code {catalogCode} already exists.");
            instanceCodes = new List<string> { catalogCode };
        }

        var (originals, currents) = SplitValueAcrossInstances(lineTotal, lineTotal, qty);
        for (var index = 0; index < qty; index++)
        {
            var instance = new AssetInstance
            {
                AssetId = asset.AssetId,
                WarehouseId = warehouseId,
                InstanceCode = instanceCodes[index],
                Status = (int)AssetStatus.Available,
                InUseDate = purchaseDate,
                PurchaseDate = purchaseDate,
                OriginalPrice = originals[index],
                CurrentValue = currents[index],
            };
            _context.AssetInstances.Add(instance);
            await _context.SaveChangesAsync();
        }

        return asset.AssetId;
    }

    private static bool IsValidCodePrefix(string prefix) =>
        prefix.Length is >= 1 and <= 32 && prefix.All(char.IsLetterOrDigit);

    private async Task<List<string>> GenerateInstanceCodesForPrefixAsync(string prefix, int count)
    {
        var codes = await _context.AssetInstances
            .AsNoTracking()
            .Select(i => i.InstanceCode)
            .ToListAsync();
        return GenerateSequentialCodesForPrefix(prefix, count, codes);
    }

    private async Task<List<string>> GenerateAssetCatalogCodesForPrefixAsync(string prefix, int count)
    {
        var codes = await _context.Assets
            .AsNoTracking()
            .Select(a => a.Code)
            .ToListAsync();
        return GenerateSequentialCodesForPrefix(prefix, count, codes);
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
            if (int.TryParse(suffix, NumberStyles.Integer, null, out var n))
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

    private static (decimal[] Originals, decimal[] Currents) SplitValueAcrossInstances(
        decimal originalPrice,
        decimal currentValue,
        int qty)
    {
        var o = new decimal[qty];
        var c = new decimal[qty];
        if (qty == 1)
        {
            o[0] = originalPrice;
            c[0] = currentValue;
            return (o, c);
        }

        var oEach = Math.Round(originalPrice / qty, 2, MidpointRounding.AwayFromZero);
        var cEach = Math.Round(currentValue / qty, 2, MidpointRounding.AwayFromZero);
        for (var i = 0; i < qty - 1; i++)
        {
            o[i] = oEach;
            c[i] = cEach;
        }

        o[qty - 1] = originalPrice - oEach * (qty - 1);
        c[qty - 1] = currentValue - cEach * (qty - 1);
        return (o, c);
    }

    private static decimal ParseEstimatedPriceToDecimal(string? estimatedPrice)
    {
        if (string.IsNullOrWhiteSpace(estimatedPrice))
            return 0;
        var raw = estimatedPrice.Trim();
        var digitsOnly = new string(raw.Where(ch => char.IsDigit(ch) || ch is '.' or ',' or '-').ToArray());
        if (string.IsNullOrEmpty(digitsOnly))
            return 0;
        var normalized = digitsOnly.Replace(".", "", StringComparison.Ordinal).Replace(",", ".", StringComparison.Ordinal);
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private async Task SaveProcurementDocumentsAsync(
        int assetRequestId,
        IEnumerable<CapitalizationDocumentInputDTO>? documents,
        int userId)
    {
        if (documents == null) return;

        var normalizedDocs = documents
            .Where(d => d != null && !string.IsNullOrWhiteSpace(d.Url))
            .Select(d => d.Url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedDocs.Count == 0) return;

        var procurement = await _context.Procurements
            .AsNoTracking()
            .Where(p => p.AssetRequestId == assetRequestId)
            .OrderByDescending(p => p.ProcurementId)
            .FirstOrDefaultAsync();
        if (procurement == null) return;

        var existingUrls = await _context.Documents
            .AsNoTracking()
            .Where(d => d.ProcurementId == procurement.ProcurementId)
            .Select(d => d.FileUrl)
            .ToListAsync();
        var existingSet = existingUrls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toInsert = normalizedDocs
            .Where(u => !existingSet.Contains(u))
            .Select(u => new Document
            {
                ProcurementId = procurement.ProcurementId,
                DocumentType = 0,
                FileUrl = u,
                UploadedBy = userId,
                UploadedDate = DateTime.UtcNow
            })
            .ToList();

        if (toInsert.Count == 0) return;
        _context.Documents.AddRange(toInsert);
        await _context.SaveChangesAsync();
    }

    private async Task MarkRequestAsCapitalizedAsync(int assetRequestId, int userId)
    {
        var request = await _context.AssetRequests.FirstOrDefaultAsync(x => x.AssetRequestId == assetRequestId);
        if (request == null || request.Status == PurchaseRequestCapitalizedStatus) return;

        var fromStatus = request.Status;
        request.Status = PurchaseRequestCapitalizedStatus;

        var actionRoleId = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .FirstOrDefaultAsync();

        _context.AssetRequestRecords.Add(new AssetRequestRecord
        {
            AssetRequestId = request.AssetRequestId,
            FromStatus = fromStatus,
            ToStatus = request.Status,
            Action = 1,
            ActionByUserId = userId,
            ActionRoleId = actionRoleId,
            Comment = "Capitalized to fixed asset",
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    private static string ResolveAssetNameFromRequest(AssetRequest request, string? fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(request.ProposedData))
        {
            try
            {
                using var doc = JsonDocument.Parse(request.ProposedData);
                if (doc.RootElement.TryGetProperty("equipment", out var equipment)
                    && equipment.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in equipment.EnumerateArray())
                    {
                        if (!item.TryGetProperty("name", out var nameElement)) continue;
                        var name = nameElement.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(name))
                            return name;
                    }
                }
            }
            catch
            {
                // Ignore malformed proposedData and fallback safely.
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
            return fallbackName.Trim();

        return request.Title;
    }
}
