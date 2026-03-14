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
    Draft = 0,
    Purchased = 1,
    ReadyForUse = 2,
    Capitalized = 3,
    Disposed = 4
}

public enum AssetLifeActionType
{
    Created = 1,
    StatusChanged = 2,
    Capitalized = 3,
    Transferred = 4,
    Repaired = 5,
    Disposed = 6
}

public enum MaintenanceFrequencyType
{
    OneTime = 1,
    Periodic = 2
}

public enum MaintenanceRepeatIntervalUnit
{
    Day = 1,
    Week = 2,
    Month = 3,
    Year = 4
}

public enum ScheduleType
{
    Auto = 1,
    Request = 2
}

public enum MaintenanceTaskStatus
{
    Pending = 0,
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}