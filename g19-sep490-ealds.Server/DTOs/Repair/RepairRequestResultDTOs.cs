namespace g19_sep490_ealds.Server.DTOs.Repair;

public class RepairRequestCreateResultDTO
{
    public int AssetRequestId { get; set; }
    public int TaskId { get; set; }
}

public class RepairStartResultDTO
{
    public int AssetRequestId { get; set; }
    public int Status { get; set; }
    public int? TaskId { get; set; }
}

public class RepairCompleteResultDTO
{
    public int RecordId { get; set; }
    public int TaskId { get; set; }
}
