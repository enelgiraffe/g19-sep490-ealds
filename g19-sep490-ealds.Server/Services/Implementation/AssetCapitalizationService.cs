using g19_sep490_ealds.Server.Events.Command;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTO.RequestDTO;
using g19_sep490_ealds.Server.Models.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.ServiceImplementation;

public class AssetCapitalizationService : IAssetCapitalizationService
{
    private readonly EaldsDbContext _context;
    private readonly IAssetCapitalizationMapper _mapper;
    private readonly IMediator _mediator;

    public AssetCapitalizationService(
        EaldsDbContext context,
        IAssetCapitalizationMapper mapper,
        IMediator mediator)
    {
        _context = context;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<AssetCapitalizationResponseDTO> CapitalizeAssetAsync(AssetCapitalizationRequestDTO request, int userId)
    {
        var asset = await _context.Assets.FindAsync(request.AssetId);

        if (asset == null)
            throw new Exception("Asset not found");

        if (asset.StatusEnum == AssetStatus.Capitalized)
            throw new Exception("Asset is already Capitalized");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var oldStatus = (int)asset.StatusEnum;
            var role = 1;

            var entity = _mapper.ToEntity(request.AssetId, request.Note, userId);
            _context.AssetCapitalizations.Add(entity);

            asset.StatusEnum = AssetStatus.Capitalized;

            await _context.SaveChangesAsync();

            await _mediator.Publish(
                new AssetStatusChangedEvent(
                    asset.AssetId,
                    oldStatus,
                    (int)asset.StatusEnum,
                    userId,
                    role  // hoặc lấy role từ context
                )
            );

            await transaction.CommitAsync();

            return _mapper.ToResponse(entity);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
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

            // "Approved by accountant" in current workflow is status=1 (waiting director approval)
            if (ar.Status != 1)
                throw new Exception("Only purchase requests with status=1 (Accountant approved) can be capitalized.");

            if (ar.AssetId.HasValue)
            {
                // Already has asset => just capitalize
                var resultExisting = await CapitalizeAssetAsync(
                    new AssetCapitalizationRequestDTO { AssetId = ar.AssetId.Value, Note = request.Note },
                    userId);
                await transaction.CommitAsync();
                return resultExisting;
            }

            // Create asset first
            if (await _context.Assets.AnyAsync(a => a.Code == request.Code))
                throw new Exception("Asset code already exists.");

            var asset = new Asset
            {
                Code = request.Code,
                Name = request.Name,
                AssetTypeId = request.AssetTypeId,
                PurchaseDate = request.PurchaseDate,
                OriginalPrice = request.OriginalPrice,
                CurrentValue = request.CurrentValue,
                Status = (int)AssetStatus.Available,
                Unit = request.Unit,
                Quantity = request.Quantity,
                WarehouseId = request.WarehouseId,
                CreatedBy = userId,
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            ar.AssetId = asset.AssetId;
            await _context.SaveChangesAsync();

            var result = await CapitalizeAssetAsync(
                new AssetCapitalizationRequestDTO { AssetId = asset.AssetId, Note = request.Note },
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
}