using g19_sep490_ealds.Server.Events.Command;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.ServiceInterface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace g19_sep490_ealds.Server.Services.ServiceImplementation;

public class AssetCapitalizationService : IAssetCapitalizationService
{
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
        int userId)
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
}
