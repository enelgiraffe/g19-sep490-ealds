using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
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
            from mr in _context.MaintenanceRecords
            join t in _context.MaintenaceTasks on mr.TaskId equals t.TaskId
            where t.AssetId == assetId
            orderby mr.ExecutionDate descending
            select new MaintenanceRecordResponseDTO
            {
                RecordId = mr.RecordId,
                TaskId = mr.TaskId,
                ExecutionDate = mr.ExecutionDate,
                TotalCost = mr.TotalCost,
                WorkPerformed = mr.WorkPerformed,
                ConditionBefore = mr.ConditionBefore,
                ConditionAfter = mr.ConditionAfter,
                TechnicalNote = mr.TechnicalNote,
                Status = (Utils.EnumsStatus.MaintenanceRecordStatus)mr.Status
            }
        ).ToListAsync();

        return records;
    }
}
