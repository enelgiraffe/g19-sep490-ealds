-- ============================================================
-- THEM BANG THUC HIEN THANH LY (giai doan cuoi)
-- Chay tren database EALDS.
-- Idempotent: co the chay lai an toan.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'dbo.DisposalExecution', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DisposalExecution
        (
            DisposalExecutionId INT IDENTITY(1,1) NOT NULL,
            AssetRequestId INT NOT NULL,
            AppraisalId INT NULL,                          -- link dot tham dinh da duoc duyet
            DisposalRecordId INT NULL,                     -- link record thanh ly hien co (neu co)

            PlannedExecutionDate DATETIME NULL,            -- ngay du kien thuc hien
            ExecutedDate DATETIME NULL,                    -- ngay thuc hien thuc te
            ExecutionMethod INT NULL,                      -- cach thuc thanh ly thuc te

            BuyerName NVARCHAR(255) NULL,                 -- don vi/muc tieu tiep nhan
            BuyerContact NVARCHAR(255) NULL,
            ContractNo NVARCHAR(100) NULL,
            InvoiceNo NVARCHAR(100) NULL,
            MinutesNo NVARCHAR(100) NULL,                 -- so bien ban ban giao/thanh ly

            ActualDisposalValue DECIMAL(18,2) NULL,       -- so tien thuc thu
            ExpenseValue DECIMAL(18,2) NULL,              -- chi phi lien quan (neu co)
            NetValue AS (ISNULL(ActualDisposalValue, 0) - ISNULL(ExpenseValue, 0)),

            AttachmentUrls NVARCHAR(MAX) NULL,            -- danh sach file/chung tu (json/url)
            ExecutionNote NVARCHAR(MAX) NULL,

            Status INT NOT NULL CONSTRAINT DF_DisposalExecution_Status DEFAULT (0),
                                                     -- 0 Draft, 1 Submitted, 2 Completed, 3 Rejected
            SubmittedBy INT NULL,
            SubmittedDate DATETIME NULL,
            ApprovedBy INT NULL,
            ApprovedDate DATETIME NULL,

            CreatedBy INT NOT NULL,
            CreatedDate DATETIME NOT NULL CONSTRAINT DF_DisposalExecution_CreatedDate DEFAULT (GETDATE()),
            UpdatedBy INT NULL,
            UpdatedDate DATETIME NULL,

            CONSTRAINT PK_DisposalExecution PRIMARY KEY (DisposalExecutionId),
            CONSTRAINT UQ_DisposalExecution_AssetRequest UNIQUE (AssetRequestId),
            CONSTRAINT FK_DisposalExecution_AssetRequest FOREIGN KEY (AssetRequestId) REFERENCES dbo.AssetRequest(AssetRequestId),
            CONSTRAINT FK_DisposalExecution_Appraisal FOREIGN KEY (AppraisalId) REFERENCES dbo.DisposalAppraisal(AppraisalId),
            CONSTRAINT FK_DisposalExecution_DisposalRecord FOREIGN KEY (DisposalRecordId) REFERENCES dbo.DisposalRecord(DiposalId),
            CONSTRAINT FK_DisposalExecution_SubmittedBy FOREIGN KEY (SubmittedBy) REFERENCES dbo.[User](UserId),
            CONSTRAINT FK_DisposalExecution_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES dbo.[User](UserId),
            CONSTRAINT FK_DisposalExecution_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.[User](UserId),
            CONSTRAINT FK_DisposalExecution_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.[User](UserId)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DisposalExecution_Status' AND object_id = OBJECT_ID(N'dbo.DisposalExecution'))
    BEGIN
        CREATE INDEX IX_DisposalExecution_Status
            ON dbo.DisposalExecution(Status);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DisposalExecution_ExecutedDate' AND object_id = OBJECT_ID(N'dbo.DisposalExecution'))
    BEGIN
        CREATE INDEX IX_DisposalExecution_ExecutedDate
            ON dbo.DisposalExecution(ExecutedDate);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DisposalExecution_SubmittedBy' AND object_id = OBJECT_ID(N'dbo.DisposalExecution'))
    BEGIN
        CREATE INDEX IX_DisposalExecution_SubmittedBy
            ON dbo.DisposalExecution(SubmittedBy);
    END;

    COMMIT TRAN;
    PRINT N'Da tao/cap nhat xong bang thuc hien thanh ly.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;

