-- Adds AssetRequestId to TransferRecord (NOT NULL FK to AssetRequest).
-- Run after backup. If the column already exists, skip or adjust.

IF COL_LENGTH(N'dbo.TransferRecord', N'AssetRequestId') IS NULL
BEGIN
    ALTER TABLE dbo.TransferRecord
    ADD AssetRequestId INT NULL;

    -- Backfill: match by business rules if possible; otherwise set to a placeholder request or delete orphans.
    -- Example only — replace with your data fix:
    -- UPDATE tr SET tr.AssetRequestId = ar.AssetRequestId
    -- FROM dbo.TransferRecord tr
    -- INNER JOIN dbo.AssetRequest ar ON ...

    ALTER TABLE dbo.TransferRecord
    ALTER COLUMN AssetRequestId INT NOT NULL;

    ALTER TABLE dbo.TransferRecord
    ADD CONSTRAINT FK_TransferRecord_AssetRequest
        FOREIGN KEY (AssetRequestId) REFERENCES dbo.AssetRequest(AssetRequestId);
END
GO
