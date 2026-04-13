-- Links auto-generated allocation requests to the purchase requisition (PR).
IF COL_LENGTH('dbo.AssetRequest', 'SourcePurchaseRequestId') IS NULL
BEGIN
    ALTER TABLE dbo.AssetRequest ADD SourcePurchaseRequestId INT NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AssetRequest_SourcePurchaseRequestId' AND object_id = OBJECT_ID('dbo.AssetRequest'))
    CREATE NONCLUSTERED INDEX IX_AssetRequest_SourcePurchaseRequestId ON dbo.AssetRequest (SourcePurchaseRequestId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AssetRequest_AssetRequest_SourcePurchaseRequestId')
    ALTER TABLE dbo.AssetRequest ADD CONSTRAINT FK_AssetRequest_AssetRequest_SourcePurchaseRequestId
        FOREIGN KEY (SourcePurchaseRequestId) REFERENCES dbo.AssetRequest (AssetRequestId);
GO
