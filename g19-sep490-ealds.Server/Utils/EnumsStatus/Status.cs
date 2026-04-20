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
    UnderMaintenance = 2,
    InRepair = 9,
    Reserved = 3,
    Disposed = 4,
    Lost = 5,
    Liquidated = 6,
    Capitalized = 7,
    Damaged = 8,
    Active = 1
}

public enum AssetLifeActionType
{
    Created = 1,
    StatusChanged = 2,
    Capitalized = 3,
    Transferred = 4,
    Repaired = 5,
    Disposed = 6,
    Updated = 7
}

public enum InventorySessionStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    Confirmed = 4,
    /// <summary>Legacy: awaiting book-side reconciliation (may exist on historical rows).</summary>
    PendingAccountant = 6
}

public enum InventoryTaskStatus
{
    Pending = 0,
    Checked = 1
}

[Flags]
public enum DiscrepancyType
{
    None = 0,
    LocationMismatch = 1,
    UserMismatch = 2,
    ValueMismatch = 4,
    ConditionMismatch = 8,
    AssetNotFound = 16,
    QuantityMismatch = 32
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

public enum MaintenanceRecordStatus
{
    Completed = 1,
    Failed = 2,
    Partial = 3
}
