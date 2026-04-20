-- ============================================================
-- ADD ResolvedAt COLUMN TO InventoryDiscrepancy TABLE
-- Nguyen nhan: Migration ban dau tao bang InventoryDiscrepancy
-- khong co cot ResolvedAt, nhung model/EF cau hinh yeu cau cot nay.
-- Gianh cho database EALDS_F1
-- ============================================================

USE [EALDS_F2];
GO

-- Kiem tra cot da ton tai chua
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('InventoryDiscrepancy')
      AND name = 'ResolvedAt'
)
BEGIN
    ALTER TABLE [InventoryDiscrepancy]
    ADD [ResolvedAt] datetime2 NULL;

    PRINT N'Da them cot ResolvedAt vao bang InventoryDiscrepancy.';
END
ELSE
BEGIN
    PRINT N'Cot ResolvedAt da ton tai trong bang InventoryDiscrepancy.';
END
GO

-- Kiem tra ket qua
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InventoryDiscrepancy'
ORDER BY ORDINAL_POSITION;