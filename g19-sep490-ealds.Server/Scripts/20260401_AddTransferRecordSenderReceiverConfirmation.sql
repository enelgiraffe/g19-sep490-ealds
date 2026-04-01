-- Thêm cột lưu trạng thái xác nhận bàn giao / nhận tài sản (TransferRecord).

IF COL_LENGTH('dbo.TransferRecord', 'IsSenderConfirmed') IS NULL
BEGIN
    ALTER TABLE dbo.TransferRecord ADD IsSenderConfirmed BIT NOT NULL
        CONSTRAINT DF_TransferRecord_IsSenderConfirmed DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.TransferRecord', 'SenderConfirmedAt') IS NULL
BEGIN
    ALTER TABLE dbo.TransferRecord ADD SenderConfirmedAt DATETIME2(7) NULL;
END
GO

IF COL_LENGTH('dbo.TransferRecord', 'IsReceiverConfirmed') IS NULL
BEGIN
    ALTER TABLE dbo.TransferRecord ADD IsReceiverConfirmed BIT NOT NULL
        CONSTRAINT DF_TransferRecord_IsReceiverConfirmed DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.TransferRecord', 'ReceiverConfirmedAt') IS NULL
BEGIN
    ALTER TABLE dbo.TransferRecord ADD ReceiverConfirmedAt DATETIME2(7) NULL;
END
GO
