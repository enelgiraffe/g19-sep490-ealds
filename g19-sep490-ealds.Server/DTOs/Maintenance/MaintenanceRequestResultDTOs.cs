namespace g19_sep490_ealds.Server.DTOs.Maintenance;

public class MaintenanceRequestCreateResultDTO
{
    public int AssetRequestId { get; set; }
    public int TaskId { get; set; }
}

public class MaintenanceStartResultDTO
{
    public int AssetRequestId { get; set; }
    public int Status { get; set; }
    public int? TaskId { get; set; }
}

public class MaintenanceCompleteResultDTO
{
    public int RecordId { get; set; }
    public int TaskId { get; set; }
}