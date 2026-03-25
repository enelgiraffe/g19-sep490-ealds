-- ============================================================
-- EALDS - Backfill missing interval columns for MaintenanceSchedule
-- Fix runtime error: Invalid column name 'IntervalUnit'/'IntervalValue'
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.MaintenanceSchedule', N'U') IS NULL
BEGIN
    RAISERROR(N'Bang dbo.MaintenanceSchedule khong ton tai. Kiem tra DB/schema.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.MaintenanceSchedule', 'IntervalUnit') IS NULL
    ALTER TABLE dbo.MaintenanceSchedule ADD IntervalUnit INT NULL;
GO

IF COL_LENGTH('dbo.MaintenanceSchedule', 'IntervalValue') IS NULL
    ALTER TABLE dbo.MaintenanceSchedule ADD IntervalValue INT NULL;
GO

PRINT N'Hoan tat: da them cot IntervalUnit/IntervalValue cho MaintenanceSchedule (neu chua co).';
GO

