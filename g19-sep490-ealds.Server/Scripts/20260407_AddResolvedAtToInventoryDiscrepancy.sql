-- ============================================================
-- THEM COT ResolvedAt VAO BANG InventoryDiscrepancy
-- Nguyen nhan: EF Core model co ResolvedAt nhung database thieu cot nay
-- Loi: "Invalid column name 'ResolvedAt'" khi InventoryController.GetSessions()
-- ============================================================

-- ========== BUOC 1: KIEM TRA ==========
EXEC sp_columns 'InventoryDiscrepancy';

-- Kiem tra xem cot da ton tai chua
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'InventoryDiscrepancy' AND COLUMN_NAME = 'ResolvedAt'
)
BEGIN
    PRINT N'Cot ResolvedAt chua ton tai trong bang InventoryDiscrepancy - se duoc tao...';

    -- ========== BUOC 2: THEM COT ==========
    ALTER TABLE InventoryDiscrepancy
    ADD ResolvedAt datetime2 NULL;
    PRINT N'Da them cot ResolvedAt (nullable) vao bang InventoryDiscrepancy.';
END
ELSE
BEGIN
    PRINT N'Cot ResolvedAt da ton tai trong bang InventoryDiscrepancy.';
END

-- ========== BUOC 3: KIEM TRA SAU KHI THEM ==========
EXEC sp_columns 'InventoryDiscrepancy';
PRINT N'Hoan tat kiem tra cau truc bang InventoryDiscrepancy.';
