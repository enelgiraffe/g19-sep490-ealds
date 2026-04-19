/*
  Manual DB change (same as EF migration 20260419120000_DocumentGoodsReceiptSupplierInvoice).
  Adds optional links from Document to GoodsReceipt and SupplierInvoice for attachment URLs.

  Run against your Ealds database after backup. Safe to re-run: skips objects that already exist.
*/

SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.Document', N'GoodsReceiptId') IS NULL
BEGIN
    ALTER TABLE dbo.Document ADD GoodsReceiptId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Document', N'SupplierInvoiceId') IS NULL
BEGIN
    ALTER TABLE dbo.Document ADD SupplierInvoiceId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_Document_GoodsReceiptId'
      AND i.object_id = OBJECT_ID(N'dbo.Document')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Document_GoodsReceiptId
        ON dbo.Document (GoodsReceiptId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_Document_SupplierInvoiceId'
      AND i.object_id = OBJECT_ID(N'dbo.Document')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Document_SupplierInvoiceId
        ON dbo.Document (SupplierInvoiceId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Document_GoodsReceipt_GoodsReceiptId')
BEGIN
    ALTER TABLE dbo.Document
        ADD CONSTRAINT FK_Document_GoodsReceipt_GoodsReceiptId
        FOREIGN KEY (GoodsReceiptId) REFERENCES dbo.GoodsReceipt (GoodsReceiptId)
        ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Document_SupplierInvoice_SupplierInvoiceId')
BEGIN
    ALTER TABLE dbo.Document
        ADD CONSTRAINT FK_Document_SupplierInvoice_SupplierInvoiceId
        FOREIGN KEY (SupplierInvoiceId) REFERENCES dbo.SupplierInvoice (SupplierInvoiceId)
        ON DELETE CASCADE;
END
GO
