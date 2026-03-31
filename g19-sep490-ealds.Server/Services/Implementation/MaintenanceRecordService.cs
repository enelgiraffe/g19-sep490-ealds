using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class MaintenanceRecordService : IMaintenanceRecordService
{
    private readonly EaldsDbContext _context;

    public MaintenanceRecordService(EaldsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MaintenanceRecordResponseDTO>> GetRecordsByAssetAsync(int assetId)
    {
        var records = await (
            from mr in _context.MaintenanceRecords.AsNoTracking()
            join ai in _context.AssetInstances.AsNoTracking() on mr.AssetInstanceId equals ai.AssetInstanceId
            where ai.AssetId == assetId
            orderby mr.ExecutionDate descending
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
                Status = (MaintenanceRecordStatus)mr.Status
            }
        ).ToListAsync();

        return records;
    }

    public async Task<IEnumerable<MaintenanceRecordResponseDTO>> GetRecordsByInstanceAsync(int assetInstanceId)
    {
        var records = await (
            from mr in _context.MaintenanceRecords.AsNoTracking()
            join ai in _context.AssetInstances.AsNoTracking() on mr.AssetInstanceId equals ai.AssetInstanceId
            where mr.AssetInstanceId == assetInstanceId
            orderby mr.ExecutionDate descending
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
                Status = (MaintenanceRecordStatus)mr.Status
            }
        ).ToListAsync();

        return records;
    }
}
