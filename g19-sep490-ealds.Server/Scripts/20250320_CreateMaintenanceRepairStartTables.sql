/*
  EALDS — Tách phiếu "bắt đầu" ra bảng riêng + liên kết tài liệu (DocumentLink).
  SQL Server. Chạy an toàn: chỉ CREATE khi bảng chưa tồn tại.

  Ghi chú:
  - Document.ProcurementId hiện NOT NULL → upload file mới cho BD/SC vẫn cần xử lý app/DB
    (VD: ALTER ProcurementId NULL hoặc bản ghi Procurement “hệ thống”). Bảng DocumentLink
    chỉ ánh xạ DocumentId đã tồn tại → phiếu Start.

  Sau khi chạy: scaffold lại hoặc thêm entity + DbSet.

  EntityType (DocumentLink): 10 = MaintenanceStart, 11 = RepairStart (mở rộng sau).
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.MaintenaceTask', N'U') IS NULL
BEGIN
    RAISERROR(N'Thiếu bảng dbo.MaintenaceTask.', 16, 1);
    RETURN;
END
IF OBJECT_ID(N'dbo.RepairTask', N'U') IS NULL
BEGIN
    RAISERROR(N'Thiếu bảng dbo.RepairTask.', 16, 1);
    RETURN;
END
IF OBJECT_ID(N'dbo.Document', N'U') IS NULL
BEGIN
    RAISERROR(N'Thiếu bảng dbo.Document (DocumentLink sẽ không tạo FK).', 16, 1);
    RETURN;
END
GO

/* ========== MaintenanceStart ========== */
IF OBJECT_ID(N'dbo.MaintenanceStart', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaintenanceStart (
        MaintenanceStartId   INT IDENTITY(1, 1) NOT NULL,
        MaintenaceTaskId     INT NOT NULL,
        AssetRequestId       INT NULL,
        ReportNumber         NVARCHAR(100) NULL,
        MaintenanceDate      DATETIME2(3) NULL,
        MaintenanceProvider  NVARCHAR(500) NULL,
        EstimatedCost        DECIMAL(18, 2) NULL,
        ExpectedCompletionDate DATETIME2(3) NULL,
        ExpectedCompletionFrom DATETIME2(3) NULL,
        ExpectedCompletionTo   DATETIME2(3) NULL,
        MaintenanceContent   NVARCHAR(MAX) NULL,
        DetailedDescription  NVARCHAR(MAX) NULL,
        LocationType         NVARCHAR(50) NULL,
        LocationDetail       NVARCHAR(500) NULL,
        StartedByUserId      INT NULL,
        StartedAt            DATETIME2(3) NOT NULL CONSTRAINT DF_MaintenanceStart_StartedAt DEFAULT (SYSUTCDATETIME()),
        StartComment         NVARCHAR(2000) NULL,
        CONSTRAINT PK_MaintenanceStart PRIMARY KEY CLUSTERED (MaintenanceStartId),
        CONSTRAINT UQ_MaintenanceStart_Task UNIQUE (MaintenaceTaskId),
        CONSTRAINT FK_MaintenanceStart_MaintenaceTask
            FOREIGN KEY (MaintenaceTaskId) REFERENCES dbo.MaintenaceTask (TaskId) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_MaintenanceStart_AssetRequest
        ON dbo.MaintenanceStart (AssetRequestId)
        WHERE AssetRequestId IS NOT NULL;
END
GO

/* FK tùy chọn tới AssetRequest (nếu bảng tồn tại) */
IF OBJECT_ID(N'dbo.AssetRequest', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MaintenanceStart_AssetRequest')
   AND OBJECT_ID(N'dbo.MaintenanceStart', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.MaintenanceStart
        ADD CONSTRAINT FK_MaintenanceStart_AssetRequest
        FOREIGN KEY (AssetRequestId) REFERENCES dbo.AssetRequest (AssetRequestId);
END
GO

/* ========== RepairStart ========== */
IF OBJECT_ID(N'dbo.RepairStart', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairStart (
        RepairStartId           INT IDENTITY(1, 1) NOT NULL,
        RepairTaskId            INT NOT NULL,
        AssetRequestId          INT NULL,
        ReportNumber            NVARCHAR(100) NULL,
        DamageDate              DATETIME2(3) NULL,
        DamageCondition         NVARCHAR(MAX) NULL,
        RepairDate              DATETIME2(3) NULL,
        ExpectedCompletionDate  DATETIME2(3) NULL,
        ExpectedCompletionFrom  DATETIME2(3) NULL,
        ExpectedCompletionTo    DATETIME2(3) NULL,
        EstimatedCost           DECIMAL(18, 2) NULL,
        RepairProgressStatus    NVARCHAR(MAX) NULL,
        StartedByUserId         INT NULL,
        StartedAt               DATETIME2(3) NOT NULL CONSTRAINT DF_RepairStart_StartedAt DEFAULT (SYSUTCDATETIME()),
        StartComment            NVARCHAR(2000) NULL,
        CONSTRAINT PK_RepairStart PRIMARY KEY CLUSTERED (RepairStartId),
        CONSTRAINT UQ_RepairStart_Task UNIQUE (RepairTaskId),
        CONSTRAINT FK_RepairStart_RepairTask
            FOREIGN KEY (RepairTaskId) REFERENCES dbo.RepairTask (TaskId) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_RepairStart_AssetRequest
        ON dbo.RepairStart (AssetRequestId)
        WHERE AssetRequestId IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AssetRequest', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RepairStart_AssetRequest')
   AND OBJECT_ID(N'dbo.RepairStart', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.RepairStart
        ADD CONSTRAINT FK_RepairStart_AssetRequest
        FOREIGN KEY (AssetRequestId) REFERENCES dbo.AssetRequest (AssetRequestId);
END
GO

/* ========== DocumentLink (Document ↔ phiếu Start / mở rộng) ========== */
IF OBJECT_ID(N'dbo.DocumentLink', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentLink (
        DocumentLinkId INT IDENTITY(1, 1) NOT NULL,
        DocumentId     INT NOT NULL,
        EntityType     INT NOT NULL,
        EntityId       INT NOT NULL,
        LinkRole       NVARCHAR(50) NULL,
        CreatedAt      DATETIME2(3) NOT NULL CONSTRAINT DF_DocumentLink_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_DocumentLink PRIMARY KEY CLUSTERED (DocumentLinkId),
        CONSTRAINT FK_DocumentLink_Document
            FOREIGN KEY (DocumentId) REFERENCES dbo.Document (DocumentId) ON DELETE CASCADE,
        CONSTRAINT UQ_DocumentLink_Doc_Entity UNIQUE (DocumentId, EntityType, EntityId)
    );
    CREATE NONCLUSTERED INDEX IX_DocumentLink_Entity
        ON dbo.DocumentLink (EntityType, EntityId);
END
GO

/*
  EntityType gợi ý:
    10 = MaintenanceStart.MaintenanceStartId
    11 = RepairStart.RepairStartId
*/

PRINT N'Hoàn tất: MaintenanceStart, RepairStart, DocumentLink (nếu chưa có).';
GO
