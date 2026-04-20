-- ============================================================
-- GHI NHAN CAC MIGRATION DA CHAY ROI (MA CO SO DU LIEU)
-- Nguyen nhan: Database da co cac bang nhung EF Migration history
-- chua duoc danh dau la da ap dung -> gap loi "table already exists"
-- ============================================================

USE [EALDS_F1];
GO
SELECT * FROM [__EFMigrationsHistory] ORDER BY [MigrationId];

-- ============================================================
-- CAC MIGRATION CAN DANH DAU LA DA AP DUNG
-- (Cac bang nay da ton tai trong database)
-- ============================================================

-- Migration co so: tao tat ca cac bang chinh
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260323150726_AddStartFieldsToTasksAndRepairRecord')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260323150726_AddStartFieldsToTasksAndRepairRecord', N'8.0.11');
    PRINT N'Da ghi nhan: 20260323150726_AddStartFieldsToTasksAndRepairRecord';
END
ELSE
BEGIN
    PRINT N'Da ghi nhan roi: 20260323150726_AddStartFieldsToTasksAndRepairRecord';
END

-- Migration them cot vao PurchaseOrder
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260404162042_ProcurementPoStandalone')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260404162042_ProcurementPoStandalone', N'8.0.11');
    PRINT N'Da ghi nhan: 20260404162042_ProcurementPoStandalone';
END
ELSE
BEGIN
    PRINT N'Da ghi nhan roi: 20260404162042_ProcurementPoStandalone';
END

-- Migration them GoodsReceipts / ReceivedQty
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260405171403_GoodsReceiptsAndPoReceivedQty')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405171403_GoodsReceiptsAndPoReceivedQty', N'8.0.11');
    PRINT N'Da ghi nhan: 20260405171403_GoodsReceiptsAndPoReceivedQty';
END
ELSE
BEGIN
    PRINT N'Da ghi nhan roi: 20260405171403_GoodsReceiptsAndPoReceivedQty';
END

-- Migration them SupplierInvoices
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260406042342_SupplierInvoices')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260406042342_SupplierInvoices', N'8.0.11');
    PRINT N'Da ghi nhan: 20260406042342_SupplierInvoices';
END
ELSE
BEGIN
    PRINT N'Da ghi nhan roi: 20260406042342_SupplierInvoices';
END

-- Migration xoa cac bang disposal appraisal
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260407051059_RemoveDisposalAppraisalTables')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260407051059_RemoveDisposalAppraisalTables', N'8.0.11');
    PRINT N'Da ghi nhan: 20260407051059_RemoveDisposalAppraisalTables';
END
ELSE
BEGIN
    PRINT N'Da ghi nhan roi: 20260407051059_RemoveDisposalAppraisalTables';
END

-- Kiem tra lich su migration sau khi ghi nhan
SELECT * FROM [__EFMigrationsHistory] ORDER BY [MigrationId];
PRINT N'Hoan tat. Tat ca cac migration da duoc danh dau la da ap dung.';
