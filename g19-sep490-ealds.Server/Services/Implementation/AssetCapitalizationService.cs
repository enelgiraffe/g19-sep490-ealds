using g19_sep490_ealds.Server.Events.Command;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTO.RequestDTO;
using g19_sep490_ealds.Server.Models.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;

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
}