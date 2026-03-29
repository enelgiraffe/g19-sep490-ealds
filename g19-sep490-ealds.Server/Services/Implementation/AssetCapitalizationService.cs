using g19_sep490_ealds.Server.Events.Command;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models.DTO.RequestDTO;
using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    public async Task<AssetCapitalizationResponseDTO> CapitalizeAssetAsync(AssetCapitalizationRequestDTO request, int userId)
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

            if (request.AssetRequestId.HasValue && request.AssetRequestId.Value > 0)
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
                    userId);
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
                userId);

            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
