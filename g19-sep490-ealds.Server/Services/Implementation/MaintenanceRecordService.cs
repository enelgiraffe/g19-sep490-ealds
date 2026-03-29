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

    public MaintenanceRecordService(
        IMaintenanceRecordMapper mapper, EALDSDbcontext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<MaintenanceRecordResponseDTO>> GetRecordsByAssetAsync(int assetId)
    {
        var records = await _context.MaintenanceRecords
        .Where(x => x.AssetInstanceId == assetId)
        .OrderByDescending(x => x.ExecutionDate)
        .ToListAsync();

        if (!records.Any())
            throw new KeyNotFoundException("Tài sản chưa có lịch sử bảo trì");

        return _mapper.ListEntityToResponse(records);

    }
}