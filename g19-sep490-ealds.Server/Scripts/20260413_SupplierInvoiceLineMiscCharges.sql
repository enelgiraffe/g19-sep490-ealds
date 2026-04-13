/*
  Supplier invoice: optional PO line + ad-hoc charge rows (ChargeDescription).
  Idempotent for SQL Server. Prefer dotnet ef database update when using EF migrations.
*/
IF COL_LENGTH(N'dbo.SupplierInvoiceLine', N'ChargeDescription') IS NULL
BEGIN
    ALTER TABLE dbo.SupplierInvoiceLine
        ADD ChargeDescription NVARCHAR(500) NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_SupplierInvoiceLine_ProcurementLine'
)
BEGIN
    ALTER TABLE dbo.SupplierInvoiceLine DROP CONSTRAINT FK_SupplierInvoiceLine_ProcurementLine;
END
GO

ALTER TABLE dbo.SupplierInvoiceLine ALTER COLUMN ProcurementLineId INT NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_SupplierInvoiceLine_ProcurementLine'
)
BEGIN
    ALTER TABLE dbo.SupplierInvoiceLine
        ADD CONSTRAINT FK_SupplierInvoiceLine_ProcurementLine
            FOREIGN KEY (ProcurementLineId) REFERENCES dbo.ProcurementLine (LineId);
END
GO
