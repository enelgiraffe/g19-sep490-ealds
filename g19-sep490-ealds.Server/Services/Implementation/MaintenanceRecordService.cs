using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class MaintenanceRecordService : IMaintenanceRecordService
{
    private readonly EALDSDbcontext _context;
    private readonly IMaintenanceRecordMapper _mapper;

    public MaintenanceRecordService(EALDSDbcontext context, IMaintenanceRecordMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MaintenanceRecordResponseDTO>> GetRecordsByAssetAsync(int assetId)
    {
        var records = await _context.MaintenanceRecords
        .Where(x => x.AssetId == assetId)
        .OrderByDescending(x => x.ExecutionDate)
        .ToListAsync();

        if (!records.Any())
            throw new KeyNotFoundException("Tài sản chưa có lịch sử bảo trì");

        return _mapper.ListEntityToResponse(records);
    }
}