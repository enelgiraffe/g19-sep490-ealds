-- Purchase request line items: one row per equipment line from ProposedData; links to Asset after capitalization.
-- Idempotent. Run after backup.

IF OBJECT_ID(N'dbo.AssetRequestPurchaseLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetRequestPurchaseLine (
        LineId            INT IDENTITY(1,1) NOT NULL,
        AssetRequestId    INT NOT NULL,
        LineIndex         INT NOT NULL,
        ItemName          NVARCHAR(500) NULL,
        Quantity          INT NOT NULL CONSTRAINT DF_ARPL_Quantity DEFAULT (1),
        Unit              NVARCHAR(50) NULL,
        ModelCode         NVARCHAR(100) NULL,
        EstimatedPrice    NVARCHAR(100) NULL,
        AssetId           INT NULL,
        CapitalizedAt     DATETIME2(7) NULL,
        CONSTRAINT PK_AssetRequestPurchaseLine PRIMARY KEY (LineId),
        CONSTRAINT FK_ARPL_AssetRequest FOREIGN KEY (AssetRequestId)
            REFERENCES dbo.AssetRequest(AssetRequestId),
        CONSTRAINT FK_ARPL_Asset FOREIGN KEY (AssetId)
            REFERENCES dbo.Asset(AssetId),
        CONSTRAINT UQ_ARPL_Request_Line UNIQUE (AssetRequestId, LineIndex)
    );

    CREATE NONCLUSTERED INDEX IX_ARPL_AssetRequestId
        ON dbo.AssetRequestPurchaseLine(AssetRequestId);
END
GO
