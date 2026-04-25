namespace g19_sep490_ealds.Server.DTOs.Dashboard;

public sealed class DirectorDashboardSummaryDto
{
    public DirectorDashboardKpiDto Kpi { get; set; } = null!;
    public List<DirectorDashboardPendingRowDto> PendingPreview { get; set; } = new();
    public List<DirectorDashboardAssetStatusSliceDto> AssetStatusBreakdown { get; set; } = new();
}

public sealed class DirectorDashboardKpiDto
{
    public int TotalAssets { get; set; }
    public decimal TotalAssetValue { get; set; }
    public int PendingApprovals { get; set; }
    public int AssetsDueMaintenance { get; set; }
}

public sealed class DirectorDashboardPendingRowDto
{
    public string Id { get; set; } = "";
    public string RequestType { get; set; } = "";
    public string Department { get; set; } = "";
    public DateTime CreateDate { get; set; }
    public string Status { get; set; } = "";
}

public sealed class DirectorDashboardAssetStatusSliceDto
{
    public string Name { get; set; } = "";
    public long Value { get; set; }
    public string Color { get; set; } = "#1677ff";
}
