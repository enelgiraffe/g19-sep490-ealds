using g19_sep490_ealds.Server.DTO.RequestDTO;
using g19_sep490_ealds.Server.DTO.ResponseDTO;
using g19_sep490_ealds.Server.Events.Command;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;

namespace g19_sep490_ealds.Server.Services.ServiceImplementation;

public class AssetCapitalizationService : IAssetCapitalizationService
{
    private readonly EALDSDbcontext _context;
    private readonly IAssetCapitalizationMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<AssetCapitalizationService> _logger;

    public AssetCapitalizationService(
        EALDSDbcontext context,
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
        var asset = await _context.AssetInstances.FindAsync(request.AssetInstanceId);

        if (asset == null)
            throw new Exception("AssetInstance not found");

        if (asset.StatusEnum == AssetStatus.Capitalized)
            throw new Exception("AssetInstance is already Capitalized");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var oldStatus = (int)asset.StatusEnum;
            var role = 1;
            //tam check, sau tao validate sau
            if (asset.OriginalPrice <= 30000000)
            {
                _logger.LogWarning("Tai san khong du dieu kien la TSCD");
            }

            var entity = _mapper.ToEntity(request.AssetInstanceId, request.Note, userId);
            _context.AssetCapitalizations.Add(entity);

            asset.StatusEnum = AssetStatus.Capitalized;

            await _context.SaveChangesAsync();

            await _mediator.Publish(
                new AssetStatusChangedEvent(
                    asset.AssetInstanceId,
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

    public async Task<AssetCapitalizationResponseDTO> ChangeStatusAssetAsync(AssetCapitalizationRequestDTO request, int userName)
    {
        var asset = await _context.AssetInstances.FindAsync(request.AssetInstanceId);

        if (asset == null)
            throw new Exception("AssetInstance not found");

        if (asset.StatusEnum == AssetStatus.Capitalized)
            throw new Exception("Asset is already Capitalized");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var oldStatus = (int)asset.StatusEnum;
            var role = 1;

            var entity = _mapper.ToEntity(request.AssetInstanceId, request.Note, userName);
            _context.AssetCapitalizations.Add(entity);

            asset.StatusEnum = AssetStatus.Purchased;

            await _context.SaveChangesAsync();

            await _mediator.Publish(
                new AssetStatusChangedEvent(
                    asset.AssetInstanceId,
                    oldStatus,
                    (int)asset.StatusEnum,
                    userName,
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