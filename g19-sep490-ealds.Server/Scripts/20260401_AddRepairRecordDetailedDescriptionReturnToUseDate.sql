-- Thêm cột cho bảng RepairRecord (hoàn thành sửa chữa: mô tả chi tiết + ngày đưa lại SD).
-- Chạy thủ công trên SQL Server (không dùng EF migration).
-- An toàn khi chạy lại: chỉ ADD nếu cột chưa tồn tại.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'dbo' AND t.name = N'RepairRecord' AND c.name = N'DetailedDescription'
)
BEGIN
    ALTER TABLE dbo.RepairRecord
    ADD DetailedDescription NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'dbo' AND t.name = N'RepairRecord' AND c.name = N'ReturnToUseDate'
)
BEGIN
    ALTER TABLE dbo.RepairRecord
    ADD ReturnToUseDate DATETIME2(7) NULL;
END
GO
