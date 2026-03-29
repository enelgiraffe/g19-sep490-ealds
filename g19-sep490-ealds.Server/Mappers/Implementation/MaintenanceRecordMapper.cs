using g19_sep490_ealds.Server.DTO.ResponseDTO.AssetMaintenance;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Utils.EnumsStatus;

namespace g19_sep490_ealds.Server.Mappers.Implementation;

public class MaintenanceRecordMapper : IMaintenanceRecordMapper
{
    public MaintenanceRecordResponseDTO EntityToResponse(MaintenanceRecord entity)
    {
        return new MaintenanceRecordResponseDTO
        {
            RecordId = entity.RecordId,
            TaskId = entity.TaskId,
            ExecutionDate = entity.ExecutionDate,
            TotalCost = entity.TotalCost,
            WorkPerformed = entity.WorkPerformed,
            ConditionBefore = entity.ConditionBefore,
            ConditionAfter = entity.ConditionAfter,
            TechnicalNote = null,
            Status = (MaintenanceRecordStatus)entity.Status
        };
    }

    public IEnumerable<MaintenanceRecordResponseDTO> ListEntityToResponse(IEnumerable<MaintenanceRecord> entities)
    {
        return entities.Select(x => EntityToResponse(x)).ToList();
    }
}