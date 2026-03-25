-- ============================================================
-- EALDS - Support asset-specific maintenance schedules
-- - Add Content column for custom schedule content
-- - Make TemplateId nullable (no required inheritance)
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

IF COL_LENGTH('dbo.MaintenanceSchedule', 'Content') IS NULL
    ALTER TABLE dbo.MaintenanceSchedule ADD Content NVARCHAR(MAX) NULL;
GO

DECLARE @templateNullable INT;
SELECT @templateNullable = c.is_nullable
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.MaintenanceSchedule')
  AND c.name = 'TemplateId';

IF @templateNullable = 0
BEGIN
    DECLARE @fkName NVARCHAR(256);
    SELECT TOP 1 @fkName = fk.name
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.MaintenanceSchedule')
      AND c.name = 'TemplateId';

    IF @fkName IS NOT NULL
        EXEC(N'ALTER TABLE dbo.MaintenanceSchedule DROP CONSTRAINT [' + @fkName + '];');

    ALTER TABLE dbo.MaintenanceSchedule ALTER COLUMN TemplateId INT NULL;

    ALTER TABLE dbo.MaintenanceSchedule
        WITH CHECK ADD CONSTRAINT FK_MaintenanceSchedule_TemplateId
        FOREIGN KEY (TemplateId) REFERENCES dbo.MaintenanceTemplate(TemplateId);
END
GO

PRINT N'Hoan tat: MaintenanceSchedule da ho tro content rieng cho tai san.';
GO

