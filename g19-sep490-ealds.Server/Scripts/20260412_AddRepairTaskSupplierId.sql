-- RepairTask: đơn vị sửa chữa (Supplier) gán khi bắt đầu sửa chữa
IF COL_LENGTH('dbo.RepairTask', 'SupplierId') IS NULL
BEGIN
    ALTER TABLE dbo.RepairTask ADD SupplierId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RepairTask_SupplierId' AND object_id = OBJECT_ID('dbo.RepairTask'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RepairTask_SupplierId ON dbo.RepairTask (SupplierId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RepairTask_Supplier')
BEGIN
    ALTER TABLE dbo.RepairTask WITH CHECK
    ADD CONSTRAINT FK_RepairTask_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Supplier (SupplierId);
END
GO
