using System;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using g19_sep490_ealds.Server.Utils.EnumsStatus;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class RepairRecordService : IRepairRecordService
{
    private readonly EaldsDbContext _context;

    public RepairRecordService(EaldsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RepairRecordHistoryResponseDTO>> GetHistoryByAssetAsync(int assetId)
    {
        var rows = await (
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
                rt.RepairProgressStatus,
                rr.SupplierId,
                TaskSupplierId = rt.SupplierId,
                rr.RepairWarrantyStartDate,
                rr.RepairWarrantyEndDate,
                rr.RepairWarrantyPeriodValue,
                rr.RepairWarrantyPeriodUnit,
                rr.RepairWarrantyConditions,
                rr.RepairWarrantyNote
            }
        ).ToListAsync();

        var nameMap = await LoadSupplierNamesAsync(rows.SelectMany(r => new[] { r.SupplierId, r.TaskSupplierId }));

        return rows
            .Select(row => MapToHistoryDto(
                row.RepairId,
                row.TaskId,
                row.AssetInstanceId,
                row.InstanceCode,
                row.RepairDate,
                row.ActualCost,
                row.Result,
                row.DetailedDescription,
                row.RepairTaskReason,
                row.RepairProgressStatus,
                ResolveRepairUnitName(row.SupplierId, row.TaskSupplierId, nameMap),
                row.RepairWarrantyStartDate,
                row.RepairWarrantyEndDate,
                row.RepairWarrantyPeriodValue,
                row.RepairWarrantyPeriodUnit,
                row.RepairWarrantyConditions,
                row.RepairWarrantyNote))
            .OrderByDescending(r => r.ExecutionDate)
            .ToList();
    }

    public async Task<IEnumerable<RepairRecordHistoryResponseDTO>> GetHistoryByInstanceAsync(int assetInstanceId)
    {
        var rows = await (
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
                rt.RepairProgressStatus,
                rr.SupplierId,
                TaskSupplierId = rt.SupplierId,
                rr.RepairWarrantyStartDate,
                rr.RepairWarrantyEndDate,
                rr.RepairWarrantyPeriodValue,
                rr.RepairWarrantyPeriodUnit,
                rr.RepairWarrantyConditions,
                rr.RepairWarrantyNote
            }
        ).ToListAsync();

        var nameMap = await LoadSupplierNamesAsync(rows.SelectMany(r => new[] { r.SupplierId, r.TaskSupplierId }));

        return rows
            .Select(row => MapToHistoryDto(
                row.RepairId,
                row.TaskId,
                row.AssetInstanceId,
                row.InstanceCode,
                row.RepairDate,
                row.ActualCost,
                row.Result,
                row.DetailedDescription,
                row.RepairTaskReason,
                row.RepairProgressStatus,
                ResolveRepairUnitName(row.SupplierId, row.TaskSupplierId, nameMap),
                row.RepairWarrantyStartDate,
                row.RepairWarrantyEndDate,
                row.RepairWarrantyPeriodValue,
                row.RepairWarrantyPeriodUnit,
                row.RepairWarrantyConditions,
                row.RepairWarrantyNote))
            .OrderByDescending(r => r.ExecutionDate)
            .ToList();
    }

    private async Task<Dictionary<int, string>> LoadSupplierNamesAsync(IEnumerable<int?> ids)
    {
        var set = ids.Where(id => id.HasValue && id.Value > 0).Select(id => id!.Value).Distinct().ToList();
        if (set.Count == 0)
            return new Dictionary<int, string>();

        return await _context.Suppliers.AsNoTracking()
            .Where(s => set.Contains(s.SupplierId))
            .ToDictionaryAsync(s => s.SupplierId, s => s.Name);
    }

    private static string? ResolveRepairUnitName(
        int? recordSupplierId,
        int? taskSupplierId,
        IReadOnlyDictionary<int, string> names)
    {
        if (recordSupplierId.HasValue
            && recordSupplierId.Value > 0
            && names.TryGetValue(recordSupplierId.Value, out var n1))
            return n1;
        if (taskSupplierId.HasValue
            && taskSupplierId.Value > 0
            && names.TryGetValue(taskSupplierId.Value, out var n2))
            return n2;
        return null;
    }

    private static RepairRecordHistoryResponseDTO MapToHistoryDto(
        int repairId,
        int taskId,
        int assetInstanceId,
        string instanceCode,
        DateTime repairDate,
        decimal actualCost,
        string? result,
        string? detailedDescription,
        string? repairTaskReason,
        string? repairProgressStatus,
        string? repairUnitName,
        DateOnly? repairWarrantyStartDate,
        DateOnly? repairWarrantyEndDate,
        int? repairWarrantyPeriodValue,
        string? repairWarrantyPeriodUnit,
        string? repairWarrantyConditions,
        string? repairWarrantyNote)
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

        return new RepairRecordHistoryResponseDTO
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
            RepairUnitName = repairUnitName,
            RepairWarrantyStartDate = repairWarrantyStartDate,
            RepairWarrantyEndDate = repairWarrantyEndDate,
            RepairWarrantyPeriodValue = repairWarrantyPeriodValue is > 0 ? repairWarrantyPeriodValue : null,
            RepairWarrantyPeriodUnit = string.IsNullOrWhiteSpace(repairWarrantyPeriodUnit)
                ? null
                : repairWarrantyPeriodUnit.Trim(),
            RepairWarrantyConditions = string.IsNullOrWhiteSpace(repairWarrantyConditions)
                ? null
                : repairWarrantyConditions.Trim(),
            RepairWarrantyNote = string.IsNullOrWhiteSpace(repairWarrantyNote) ? null : repairWarrantyNote.Trim()
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
