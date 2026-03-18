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
    Liquidated = 6,
    Capitalized = 7,
    Damaged = 8
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

public enum InventorySessionStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    Confirmed = 4
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
    AssetNotFound = 16
}