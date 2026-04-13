-- RepairRecord: bảo hành theo lần sửa chữa (không cập nhật bảo hành tài sản)
IF COL_LENGTH('dbo.RepairRecord', 'RepairWarrantyEndDate') IS NULL
BEGIN
    ALTER TABLE dbo.RepairRecord ADD RepairWarrantyEndDate DATE NULL;
END
GO

IF COL_LENGTH('dbo.RepairRecord', 'RepairWarrantyNote') IS NULL
BEGIN
    ALTER TABLE dbo.RepairRecord ADD RepairWarrantyNote NVARCHAR(2000) NULL;
END
GO
