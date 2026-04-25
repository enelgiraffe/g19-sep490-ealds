using g19_sep490_ealds.Server.Mappers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class MaintenanceRecordService : IMaintenanceRecordService
{
    private readonly EaldsDbContext _context;
    private readonly IMaintenanceRecordMapper _mapper;

    public MaintenanceRecordService(
        IMaintenanceRecordMapper mapper, EaldsDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<MaintenanceRecordResponseDTO>> GetRecordsByAssetAsync(int assetId)
    {
        var maintenanceRecords = await (
            from mr in _context.MaintenanceRecords.AsNoTracking()
            join ai in _context.AssetInstances.AsNoTracking() on mr.AssetInstanceId equals ai.AssetInstanceId
            where ai.AssetId == assetId
            select new MaintenanceRecordResponseDTO
            {
                RecordId = mr.RecordId,
                TaskId = mr.TaskId,
                AssetInstanceId = mr.AssetInstanceId,
                InstanceCode = ai.InstanceCode,
                ExecutionDate = mr.ExecutionDate,
                TotalCost = mr.TotalCost,
                WorkPerformed = mr.WorkPerformed,
                ConditionBefore = mr.ConditionBefore,
                ConditionAfter = mr.ConditionAfter,
                TechnicalNote = null,
                Status = (MaintenanceRecordStatus)mr.Status,
                RecordSource = "maintenance"
            }
        ).ToListAsync();

        return maintenanceRecords
            .OrderByDescending(r => r.ExecutionDate)
            .ToList();
    }

    public async Task<IEnumerable<MaintenanceRecordResponseDTO>> GetRecordsByInstanceAsync(int assetInstanceId)
    {
        var maintenanceRecords = await (
            from mr in _context.MaintenanceRecords.AsNoTracking()
            join ai in _context.AssetInstances.AsNoTracking() on mr.AssetInstanceId equals ai.AssetInstanceId
            where mr.AssetInstanceId == assetInstanceId
            select new MaintenanceRecordResponseDTO
            {
                RecordId = mr.RecordId,
                TaskId = mr.TaskId,
                AssetInstanceId = mr.AssetInstanceId,
                InstanceCode = ai.InstanceCode,
                ExecutionDate = mr.ExecutionDate,
                TotalCost = mr.TotalCost,
                WorkPerformed = mr.WorkPerformed,
                ConditionBefore = mr.ConditionBefore,
                ConditionAfter = mr.ConditionAfter,
                TechnicalNote = null,
                Status = (MaintenanceRecordStatus)mr.Status,
                RecordSource = "maintenance"
            }
        ).ToListAsync();

        return maintenanceRecords
            .OrderByDescending(r => r.ExecutionDate)
            .ToList();
    }
}
