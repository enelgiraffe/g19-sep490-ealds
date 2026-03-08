namespace g19_sep490_ealds.Server.Utils.EnumsStatus;

public enum AcceptanceRecordStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Funding = 4
}

public enum AssetStatus
{
    Available = 0,
    InUse = 1,
    InMaintenance = 2,
    Reserved = 3,
    Disposed = 4,
    Lost = 5,
    Liquidated = 6
}