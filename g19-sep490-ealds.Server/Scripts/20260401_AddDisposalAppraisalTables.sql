-- ============================================================
-- THEM BANG THAM DINH THANH LY (khong phu thuoc WorkflowStep)
-- Chay tren database EALDS.
-- Idempotent: co the chay lai an toan.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    -- 1) Bang dot tham dinh cho tung yeu cau thanh ly
    IF OBJECT_ID(N'dbo.DisposalAppraisal', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DisposalAppraisal
        (
            AppraisalId INT IDENTITY(1,1) NOT NULL,
            AssetRequestId INT NOT NULL,
            ScheduledAt DATETIME NULL,                    -- ngay hen tham dinh
            MeetingLocation NVARCHAR(255) NULL,
            ReporterUserId INT NULL,                      -- nguoi duoc phan cong nhap bien ban
            Status INT NOT NULL CONSTRAINT DF_DisposalAppraisal_Status DEFAULT (0),
                                                       -- 0: Draft, 1: Scheduled, 2: ReportSubmitted, 3: DirectorReviewed (du phong), 4: Hoi dong da xac nhan
            Notes NVARCHAR(1000) NULL,
            CreatedBy INT NOT NULL,
            CreatedDate DATETIME NOT NULL CONSTRAINT DF_DisposalAppraisal_CreatedDate DEFAULT (GETDATE()),
            UpdatedBy INT NULL,
            UpdatedDate DATETIME NULL,
            CONSTRAINT PK_DisposalAppraisal PRIMARY KEY (AppraisalId),
            CONSTRAINT UQ_DisposalAppraisal_AssetRequest UNIQUE (AssetRequestId),
            CONSTRAINT FK_DisposalAppraisal_AssetRequest FOREIGN KEY (AssetRequestId) REFERENCES dbo.AssetRequest(AssetRequestId),
            CONSTRAINT FK_DisposalAppraisal_ReporterUser FOREIGN KEY (ReporterUserId) REFERENCES dbo.[User](UserId),
            CONSTRAINT FK_DisposalAppraisal_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.[User](UserId),
            CONSTRAINT FK_DisposalAppraisal_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.[User](UserId)
        );
    END;

    -- 2) Bang thanh vien hoi dong tham dinh
    IF OBJECT_ID(N'dbo.DisposalAppraisalMember', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DisposalAppraisalMember
        (
            AppraisalMemberId INT IDENTITY(1,1) NOT NULL,
            AppraisalId INT NOT NULL,
            UserId INT NOT NULL,
            IsReporter BIT NOT NULL CONSTRAINT DF_DisposalAppraisalMember_IsReporter DEFAULT (0),
            MemberRole NVARCHAR(100) NULL,                -- Chu tich, Uy vien, Thu ky...
            AddedBy INT NOT NULL,
            AddedDate DATETIME NOT NULL CONSTRAINT DF_DisposalAppraisalMember_AddedDate DEFAULT (GETDATE()),
            CONSTRAINT PK_DisposalAppraisalMember PRIMARY KEY (AppraisalMemberId),
            CONSTRAINT UQ_DisposalAppraisalMember_AppraisalUser UNIQUE (AppraisalId, UserId),
            CONSTRAINT FK_DisposalAppraisalMember_Appraisal FOREIGN KEY (AppraisalId) REFERENCES dbo.DisposalAppraisal(AppraisalId),
            CONSTRAINT FK_DisposalAppraisalMember_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId),
            CONSTRAINT FK_DisposalAppraisalMember_AddedBy FOREIGN KEY (AddedBy) REFERENCES dbo.[User](UserId)
        );
    END;

    -- 3) Bien ban tham dinh (1 dot tham dinh co 1 bien ban hien hanh)
    IF OBJECT_ID(N'dbo.DisposalAppraisalReport', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DisposalAppraisalReport
        (
            AppraisalReportId INT IDENTITY(1,1) NOT NULL,
            AppraisalId INT NOT NULL,
            MinutesNo NVARCHAR(100) NULL,                 -- so bien ban
            MeetingDate DATETIME NULL,
            AppraisedValue DECIMAL(18,2) NULL,            -- gia tri de xuat thanh ly
            MarketReferenceValue DECIMAL(18,2) NULL,      -- gia tri tham chieu thi truong
            Summary NVARCHAR(MAX) NULL,                   -- tom tat tham dinh
            Recommendation NVARCHAR(MAX) NULL,            -- kien nghi
            AttachmentUrls NVARCHAR(MAX) NULL,            -- json/string URL dinh kem
            SubmittedBy INT NOT NULL,
            SubmittedDate DATETIME NOT NULL CONSTRAINT DF_DisposalAppraisalReport_SubmittedDate DEFAULT (GETDATE()),
            UpdatedBy INT NULL,
            UpdatedDate DATETIME NULL,
            DirectorDecision INT NULL,                    -- 1: dong y, 2: yeu cau bo sung, 3: khong dong y
            DirectorComment NVARCHAR(1000) NULL,
            DirectorReviewedBy INT NULL,
            DirectorReviewedDate DATETIME NULL,
            CONSTRAINT PK_DisposalAppraisalReport PRIMARY KEY (AppraisalReportId),
            CONSTRAINT UQ_DisposalAppraisalReport_Appraisal UNIQUE (AppraisalId),
            CONSTRAINT FK_DisposalAppraisalReport_Appraisal FOREIGN KEY (AppraisalId) REFERENCES dbo.DisposalAppraisal(AppraisalId),
            CONSTRAINT FK_DisposalAppraisalReport_SubmittedBy FOREIGN KEY (SubmittedBy) REFERENCES dbo.[User](UserId),
            CONSTRAINT FK_DisposalAppraisalReport_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.[User](UserId),
            CONSTRAINT FK_DisposalAppraisalReport_DirectorReviewedBy FOREIGN KEY (DirectorReviewedBy) REFERENCES dbo.[User](UserId)
        );
    END;

    -- 4) Index ho tro query tab pill "toi la nguoi lien quan"
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DisposalAppraisal_ReporterUserId' AND object_id = OBJECT_ID(N'dbo.DisposalAppraisal'))
    BEGIN
        CREATE INDEX IX_DisposalAppraisal_ReporterUserId
            ON dbo.DisposalAppraisal(ReporterUserId);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DisposalAppraisal_ScheduledAt' AND object_id = OBJECT_ID(N'dbo.DisposalAppraisal'))
    BEGIN
        CREATE INDEX IX_DisposalAppraisal_ScheduledAt
            ON dbo.DisposalAppraisal(ScheduledAt);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DisposalAppraisalMember_UserId' AND object_id = OBJECT_ID(N'dbo.DisposalAppraisalMember'))
    BEGIN
        CREATE INDEX IX_DisposalAppraisalMember_UserId
            ON dbo.DisposalAppraisalMember(UserId);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DisposalAppraisalReport_SubmittedBy' AND object_id = OBJECT_ID(N'dbo.DisposalAppraisalReport'))
    BEGIN
        CREATE INDEX IX_DisposalAppraisalReport_SubmittedBy
            ON dbo.DisposalAppraisalReport(SubmittedBy);
    END;

    COMMIT TRAN;
    PRINT N'Da tao/cap nhat xong cac bang tham dinh thanh ly.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;

