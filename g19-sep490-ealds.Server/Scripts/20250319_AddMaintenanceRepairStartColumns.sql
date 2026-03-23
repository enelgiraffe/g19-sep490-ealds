/*
  EALDS — Thêm cột lưu phiếu "bắt đầu bảo dưỡng" / "bắt đầu sửa chữa" (scaffold / DB-first).
  Chạy trên SQL Server. Có thể chạy lại an toàn (chỉ ADD cột nếu chưa tồn tại).

  Sau khi chạy: scaffold lại hoặc thêm property thủ công vào MaintenaceTask / RepairTask cho khớp tên cột.
*/

SET NOCOUNT ON;
GO

/* ========== MaintenaceTask ========== */
IF OBJECT_ID(N'dbo.MaintenaceTask', N'U') IS NULL
BEGIN
    RAISERROR(N'Bảng dbo.MaintenaceTask không tồn tại. Kiểm tra tên DB/schema.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'ReportNumber') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD ReportNumber NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'MaintenanceProvider') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD MaintenanceProvider NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'EstimatedCost') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD EstimatedCost DECIMAL(18, 2) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'ExpectedCompletionDate') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD ExpectedCompletionDate DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'ExpectedCompletionFrom') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD ExpectedCompletionFrom DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'ExpectedCompletionTo') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD ExpectedCompletionTo DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'MaintenanceContent') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD MaintenanceContent NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'DetailedDescription') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD DetailedDescription NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'LocationType') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD LocationType NVARCHAR(50) NULL;
GO

-- JSON mảng số DocumentId (ví dụ [1,2,3]) — hoặc để trống nếu sau này dùng bảng liên kết
IF COL_LENGTH('dbo.MaintenaceTask', 'AttachmentDocumentIdsJson') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD AttachmentDocumentIdsJson NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'AttachmentUrlsJson') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD AttachmentUrlsJson NVARCHAR(MAX) NULL;
GO

-- Audit lúc bấm "bắt đầu" (StartedBy khác AssignTo nếu cần)
IF COL_LENGTH('dbo.MaintenaceTask', 'StartedByUserId') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD StartedByUserId INT NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'StartedAt') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD StartedAt DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.MaintenaceTask', 'StartComment') IS NULL
    ALTER TABLE dbo.MaintenaceTask ADD StartComment NVARCHAR(2000) NULL;
GO

/*
  Ghi chú mapping với code hiện tại:
  - Ngày bảo dưỡng / địa điểm: vẫn có thể dùng PlannedDate, Address; các cột trên bổ sung cho form chi tiết.
*/

/* ========== RepairTask ========== */
IF OBJECT_ID(N'dbo.RepairTask', N'U') IS NULL
BEGIN
    RAISERROR(N'Bảng dbo.RepairTask không tồn tại. Kiểm tra tên DB/schema.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.RepairTask', 'ReportNumber') IS NULL
    ALTER TABLE dbo.RepairTask ADD ReportNumber NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'DamageDate') IS NULL
    ALTER TABLE dbo.RepairTask ADD DamageDate DATETIME2(3) NULL;
GO

-- Bổ sung ngoài Reason (ghi nhận ban đầu); có thể đồng bộ với Reason khi start
IF COL_LENGTH('dbo.RepairTask', 'DamageCondition') IS NULL
    ALTER TABLE dbo.RepairTask ADD DamageCondition NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'RepairDate') IS NULL
    ALTER TABLE dbo.RepairTask ADD RepairDate DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'ExpectedCompletionDate') IS NULL
    ALTER TABLE dbo.RepairTask ADD ExpectedCompletionDate DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'ExpectedCompletionFrom') IS NULL
    ALTER TABLE dbo.RepairTask ADD ExpectedCompletionFrom DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'ExpectedCompletionTo') IS NULL
    ALTER TABLE dbo.RepairTask ADD ExpectedCompletionTo DATETIME2(3) NULL;
GO

-- Chi phí dự kiến: dùng cột EstimatedCost sẵn có trên RepairTask (cập nhật khi start).

IF COL_LENGTH('dbo.RepairTask', 'RepairProgressStatus') IS NULL
    ALTER TABLE dbo.RepairTask ADD RepairProgressStatus NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'AttachmentDocumentIdsJson') IS NULL
    ALTER TABLE dbo.RepairTask ADD AttachmentDocumentIdsJson NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'AttachmentUrlsJson') IS NULL
    ALTER TABLE dbo.RepairTask ADD AttachmentUrlsJson NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'StartedByUserId') IS NULL
    ALTER TABLE dbo.RepairTask ADD StartedByUserId INT NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'StartedAt') IS NULL
    ALTER TABLE dbo.RepairTask ADD StartedAt DATETIME2(3) NULL;
GO

IF COL_LENGTH('dbo.RepairTask', 'StartComment') IS NULL
    ALTER TABLE dbo.RepairTask ADD StartComment NVARCHAR(2000) NULL;
GO

PRINT N'Hoàn tất: đã thêm cột (nếu chưa có) cho MaintenaceTask và RepairTask.';
GO
