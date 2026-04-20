-- ============================================================
-- SCRIPT Kiem tra va Them cac cot thieu (chi them neu chua co)
-- Database: EALDS_F2
-- ============================================================

USE [EALDS_F2];
GO

PRINT '===== BAT DAU Kiem TRA =====';
PRINT '';

-- ============================================================
-- 1. Bang User - Kiem tra AccessFailedCount
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('User') AND name = 'AccessFailedCount')
BEGIN
    ALTER TABLE [User] ADD [AccessFailedCount] int NOT NULL DEFAULT 0;
    PRINT '1. Da them AccessFailedCount vao bang User.';
END
ELSE
BEGIN
    PRINT '1. AccessFailedCount da ton tai trong bang User.';
END
GO

-- ============================================================
-- 2. Bang User - Kiem tra LockoutEnd
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('User') AND name = 'LockoutEnd')
BEGIN
    ALTER TABLE [User] ADD [LockoutEnd] datetime2 NULL;
    PRINT '2. Da them LockoutEnd vao bang User.';
END
ELSE
BEGIN
    PRINT '2. LockoutEnd da ton tai trong bang User.';
END
GO

-- ============================================================
-- 3. Bang AssetInstance - Kiem tra SupplierId
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AssetInstance') AND name = 'SupplierId')
BEGIN
    ALTER TABLE [AssetInstance] ADD [SupplierId] int NULL;
    PRINT '3. Da them SupplierId vao bang AssetInstance.';
END
ELSE
BEGIN
    PRINT '3. SupplierId da ton tai trong bang AssetInstance.';
END
GO

-- ============================================================
-- 4. Bang InventoryDiscrepancy - Kiem tra ResolvedAt
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InventoryDiscrepancy') AND name = 'ResolvedAt')
BEGIN
    ALTER TABLE [InventoryDiscrepancy] ADD [ResolvedAt] datetime2 NULL;
    PRINT '4. Da them ResolvedAt vao bang InventoryDiscrepancy.';
END
ELSE
BEGIN
    PRINT '4. ResolvedAt da ton tai trong bang InventoryDiscrepancy.';
END
GO

-- ============================================================
-- 5. Hien thi ket qua cuoi cung
-- ============================================================
PRINT '';
PRINT '===== KET QUA CUOI CUNG =====';
PRINT '';

PRINT '--- Bang User ---';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'User'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '--- Bang AssetInstance ---';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AssetInstance'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '--- Bang InventoryDiscrepancy ---';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InventoryDiscrepancy'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '===== HOAN TAT! Khoi dong lai ung dung. =====';
GO
