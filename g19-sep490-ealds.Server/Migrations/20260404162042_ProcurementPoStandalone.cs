using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations;

/// <summary>
/// Standalone purchase orders: nullable AssetRequestId, Currency, ProcurementLine.
/// Uses raw SQL so we do not drop/recreate unrelated FKs (EF model drift vs DB names).
/// </summary>
public partial class ProcurementPoStandalone : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DECLARE @sql nvarchar(max);

-- Drop existing FK from Procurement.AssetRequestId -> AssetRequest (name varies by DB)
SELECT @sql = 'ALTER TABLE [Procurement] DROP CONSTRAINT [' + fk.name + ']'
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables pt ON fkc.parent_object_id = pt.object_id
INNER JOIN sys.columns pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
INNER JOIN sys.tables rt ON fkc.referenced_object_id = rt.object_id
WHERE pt.name = N'Procurement' AND pc.name = N'AssetRequestId' AND rt.name = N'AssetRequest';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

ALTER TABLE [Procurement] ALTER COLUMN [AssetRequestId] int NULL;

IF COL_LENGTH(N'Procurement', N'Currency') IS NULL
BEGIN
    ALTER TABLE [Procurement] ADD [Currency] nvarchar(10) NOT NULL CONSTRAINT DF_Procurement_Currency DEFAULT N'VND';
END

IF OBJECT_ID(N'[ProcurementLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [ProcurementLine] (
        [LineId] int NOT NULL IDENTITY(1,1),
        [ProcurementId] int NOT NULL,
        [LineIndex] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [AssetId] int NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Unit] nvarchar(50) NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [ExpectedDeliveryDate] date NULL,
        CONSTRAINT [PK_ProcurementLine] PRIMARY KEY ([LineId]),
        CONSTRAINT [FK_ProcurementLine_Procurement] FOREIGN KEY ([ProcurementId]) REFERENCES [Procurement] ([ProcurementId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProcurementLine_Asset] FOREIGN KEY ([AssetId]) REFERENCES [Asset] ([AssetId])
    );
    CREATE INDEX [IX_ProcurementLine_ProcurementId] ON [ProcurementLine] ([ProcurementId]);
    CREATE INDEX [IX_ProcurementLine_AssetId] ON [ProcurementLine] ([AssetId]);
END

-- Re-create FK Procurement -> AssetRequest (optional)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
    INNER JOIN sys.tables pt ON fk.parent_object_id = pt.object_id
    WHERE pt.name = N'Procurement' AND fk.name = N'FK_Procurement_AssetRequest_AssetRequestId')
BEGIN
    ALTER TABLE [Procurement] WITH CHECK ADD CONSTRAINT [FK_Procurement_AssetRequest_AssetRequestId]
        FOREIGN KEY ([AssetRequestId]) REFERENCES [AssetRequest] ([AssetRequestId]);
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ProcurementLine]', N'U') IS NOT NULL
    DROP TABLE [ProcurementLine];

IF COL_LENGTH(N'Procurement', N'Currency') IS NOT NULL
BEGIN
    ALTER TABLE [Procurement] DROP CONSTRAINT IF EXISTS [DF_Procurement_Currency];
    ALTER TABLE [Procurement] DROP COLUMN [Currency];
END

DECLARE @sql nvarchar(max);
SELECT @sql = 'ALTER TABLE [Procurement] DROP CONSTRAINT [' + fk.name + ']'
FROM sys.foreign_keys fk
INNER JOIN sys.tables pt ON fk.parent_object_id = pt.object_id
WHERE pt.name = N'Procurement' AND fk.name = N'FK_Procurement_AssetRequest_AssetRequestId';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

-- Restore NOT NULL: fails if NULLs exist — acceptable for rollback
ALTER TABLE [Procurement] ALTER COLUMN [AssetRequestId] int NOT NULL;

SELECT @sql = 'ALTER TABLE [Procurement] DROP CONSTRAINT [' + fk.name + ']'
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables pt ON fkc.parent_object_id = pt.object_id
INNER JOIN sys.columns pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
INNER JOIN sys.tables rt ON fkc.referenced_object_id = rt.object_id
WHERE pt.name = N'Procurement' AND pc.name = N'AssetRequestId' AND rt.name = N'AssetRequest';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

ALTER TABLE [Procurement] WITH CHECK ADD CONSTRAINT [FK__Procureme__Asset__7F2BE32F]
    FOREIGN KEY ([AssetRequestId]) REFERENCES [AssetRequest] ([AssetRequestId]);
");
    }
}
