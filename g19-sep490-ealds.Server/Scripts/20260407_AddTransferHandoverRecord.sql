-- Bảng biên bản bàn giao / tiếp nhận theo từng lần xác nhận gửi hoặc nhận (TransferHandoverRecord).
-- Tương đương migration: 20260407120000_AddTransferHandoverRecord

IF OBJECT_ID(N'dbo.TransferHandoverRecord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransferHandoverRecord (
        TransferHandoverRecordId INT NOT NULL IDENTITY(1, 1),
        TransferId INT NOT NULL,
        Side NVARCHAR(20) NOT NULL,
        ActionByUserId INT NOT NULL,
        OccurredAt DATETIME2(7) NOT NULL,
        DetailsJson NVARCHAR(MAX) NOT NULL,
        UserNote NVARCHAR(2000) NULL,
        CONSTRAINT PK_TransferHandoverRecord PRIMARY KEY (TransferHandoverRecordId),
        CONSTRAINT FK_TransferHandoverRecord_TransferRecord_TransferId
            FOREIGN KEY (TransferId) REFERENCES dbo.TransferRecord (TransferId)
            ON DELETE CASCADE,
        CONSTRAINT FK_TransferHandoverRecord_User_ActionByUserId
            FOREIGN KEY (ActionByUserId) REFERENCES dbo.[User] (UserId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TransferHandoverRecord_ActionByUserId'
      AND object_id = OBJECT_ID(N'dbo.TransferHandoverRecord')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TransferHandoverRecord_ActionByUserId
        ON dbo.TransferHandoverRecord (ActionByUserId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TransferHandoverRecord_TransferId'
      AND object_id = OBJECT_ID(N'dbo.TransferHandoverRecord')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TransferHandoverRecord_TransferId
        ON dbo.TransferHandoverRecord (TransferId);
END
GO
