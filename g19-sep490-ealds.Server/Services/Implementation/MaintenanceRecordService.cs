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

        var repairRecords = await (
            from rr in _context.RepairRecords.AsNoTracking()
            join rt in _context.RepairTasks.AsNoTracking() on rr.TaskId equals rt.TaskId
            join ai in _context.AssetInstances.AsNoTracking() on rt.AssetInstanceId equals ai.AssetInstanceId
            where ai.AssetId == assetId
            select new
            {
                rr.RepairId,
                rr.TaskId,
                rt.AssetInstanceId,
                ai.InstanceCode,
                rr.RepairDate,
                rr.ActualCost,
                rr.Result,
                rr.DetailedDescription,
                RepairTaskReason = rt.Reason,
                rt.RepairProgressStatus
            }
        ).ToListAsync();

        var mappedRepairRecords = repairRecords.Select(row => MapRepairToDto(
            row.RepairId,
            row.TaskId,
            row.AssetInstanceId,
            row.InstanceCode,
            row.RepairDate,
            row.ActualCost,
            row.Result,
            row.DetailedDescription,
            row.RepairTaskReason,
            row.RepairProgressStatus));

        return maintenanceRecords
            .Concat(mappedRepairRecords)
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

        var repairRecords = await (
            from rr in _context.RepairRecords.AsNoTracking()
            join rt in _context.RepairTasks.AsNoTracking() on rr.TaskId equals rt.TaskId
            join ai in _context.AssetInstances.AsNoTracking() on rt.AssetInstanceId equals ai.AssetInstanceId
            where rt.AssetInstanceId == assetInstanceId
            select new
            {
                rr.RepairId,
                rr.TaskId,
                rt.AssetInstanceId,
                ai.InstanceCode,
                rr.RepairDate,
                rr.ActualCost,
                rr.Result,
                rr.DetailedDescription,
                RepairTaskReason = rt.Reason,
                rt.RepairProgressStatus
            }
        ).ToListAsync();

        var mappedRepairRecords = repairRecords.Select(row => MapRepairToDto(
            row.RepairId,
            row.TaskId,
            row.AssetInstanceId,
            row.InstanceCode,
            row.RepairDate,
            row.ActualCost,
            row.Result,
            row.DetailedDescription,
            row.RepairTaskReason,
            row.RepairProgressStatus));

        return maintenanceRecords
            .Concat(mappedRepairRecords)
            .OrderByDescending(r => r.ExecutionDate)
            .ToList();
    }

    private static MaintenanceRecordResponseDTO MapRepairToDto(
        int repairId,
        int taskId,
        int assetInstanceId,
        string instanceCode,
        DateTime repairDate,
        decimal actualCost,
        string? result,
        string? detailedDescription,
        string? repairTaskReason,
        string? repairProgressStatus)
    {
        var progress = repairProgressStatus?.Trim();
        var detailCol = detailedDescription?.Trim();
        var narrative = ExtractRepairCompletionNarrative(result);
        var (firstNarrative, remainderNarrative) = SplitFirstLineRest(narrative);

        var workPerformed = !string.IsNullOrWhiteSpace(progress)
            ? progress
            : !string.IsNullOrWhiteSpace(detailCol)
                ? (result?.Trim() ?? string.Empty)
                : (!string.IsNullOrWhiteSpace(firstNarrative) ? firstNarrative : (result?.Trim() ?? string.Empty));

        string? technicalNote = detailCol;
        if (string.IsNullOrWhiteSpace(technicalNote) && !string.IsNullOrWhiteSpace(remainderNarrative))
            technicalNote = remainderNarrative;

        if (string.IsNullOrWhiteSpace(technicalNote) &&
            !string.IsNullOrWhiteSpace(narrative) &&
            !string.Equals(narrative.Trim(), workPerformed.Trim(), StringComparison.Ordinal))
            technicalNote = narrative;

        if (technicalNote != null &&
            string.Equals(technicalNote.Trim(), workPerformed.Trim(), StringComparison.Ordinal))
            technicalNote = null;

        return new MaintenanceRecordResponseDTO
        {
            RecordId = repairId,
            TaskId = taskId,
            AssetInstanceId = assetInstanceId,
            InstanceCode = instanceCode,
            ExecutionDate = repairDate,
            TotalCost = actualCost,
            WorkPerformed = workPerformed,
            ConditionBefore = repairTaskReason?.Trim() ?? string.Empty,
            ConditionAfter = narrative,
            TechnicalNote = technicalNote,
            Status = MaintenanceRecordStatus.Completed,
            RecordSource = "repair"
        };
    }

    private static (string First, string? Remainder) SplitFirstLineRest(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (string.Empty, null);
        var idx = text.IndexOf('\n');
        if (idx < 0)
            return (text.Trim(), null);
        var first = text[..idx].Trim();
        var rest = text[(idx + 1)..].Trim();
        return (first, string.IsNullOrWhiteSpace(rest) ? null : rest);
    }

    /// <summary>
    /// Legacy RepairRecord.Result có thể chứa ReportNumber/ReturnToUseDate; bản ghi mới chỉ lưu kết quả ngắn ở Result,
    /// mô tả chi tiết ở cột DetailedDescription.
    /// </summary>
    private static string ExtractRepairCompletionNarrative(string? rawResult)
    {
        if (string.IsNullOrWhiteSpace(rawResult))
            return string.Empty;

        var lines = rawResult
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line =>
                !line.StartsWith("ReportNumber:", StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("ReturnToUseDate:", StringComparison.OrdinalIgnoreCase));

        return lines.Any() ? string.Join("\n", lines) : string.Empty;
    }
}
