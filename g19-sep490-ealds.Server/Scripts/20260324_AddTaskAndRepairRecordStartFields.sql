-- ============================================================
-- EALDS - Incremental columns from migration ideas
-- Source intent: AddStartFieldsToTasksAndRepairRecord
-- Strategy: SQL script only (idempotent, safe to re-run)
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ========== RepairTask ========== */
IF OBJECT_ID(N'dbo.RepairTask', N'U') IS NULL
BEGIN
    RAISERROR(N'Bang dbo.RepairTask khong ton tai. Kiem tra DB/schema.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.RepairTask', 'RepairDate') IS NULL
    ALTER TABLE dbo.RepairTask ADD RepairDate DATETIME NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'ExpectedCompletionDate') IS NULL
    ALTER TABLE dbo.RepairTask ADD ExpectedCompletionDate DATETIME NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'RepairProgressStatus') IS NULL
    ALTER TABLE dbo.RepairTask ADD RepairProgressStatus NVARCHAR(MAX) NULL;
GO

/* ========== MaintenaceTask ========== */
IF OBJECT_ID(N'dbo.MaintenaceTask', N'U') IS NULL
BEGIN
    RAISERROR(N'Bang dbo.MaintenaceTask khong ton tai. Kiem tra DB/schema.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'PerformerUserId') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD PerformerUserId INT NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'MaintenanceProvider') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD MaintenanceProvider NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'EstimatedCost') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD EstimatedCost DECIMAL(18, 2) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'ExpectedCompletionDate') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD ExpectedCompletionDate DATETIME NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'MaintenanceContent') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD MaintenanceContent NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'LocationType') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD LocationType NVARCHAR(MAX) NULL;
GO

/* ========== RepairRecord ========== */
IF OBJECT_ID(N'dbo.RepairRecord', N'U') IS NULL
BEGIN
    RAISERROR(N'Bang dbo.RepairRecord khong ton tai. Kiem tra DB/schema.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.RepairRecord', 'DamageDate') IS NULL
    ALTER TABLE dbo.RepairRecord ADD DamageDate DATETIME NULL;
GO

IF COL_LENGTH('dbo.RepairRecord', 'DamageCondition') IS NULL
    ALTER TABLE dbo.RepairRecord ADD DamageCondition NVARCHAR(MAX) NULL;
GO

PRINT N'Hoan tat: da them cac cot start fields cho RepairTask/MaintenaceTask va RepairRecord (neu chua co).';
GO
