-- RepairRecord: bổ sung trường bảo hành sửa chữa (tương tự Guarantee / bảo hành cá thể)
IF COL_LENGTH('dbo.RepairRecord', 'RepairWarrantyStartDate') IS NULL
BEGIN
    ALTER TABLE dbo.RepairRecord ADD RepairWarrantyStartDate DATE NULL;
END
GO

IF COL_LENGTH('dbo.RepairRecord', 'RepairWarrantyPeriodValue') IS NULL
BEGIN
    ALTER TABLE dbo.RepairRecord ADD RepairWarrantyPeriodValue INT NULL;
END
GO

IF COL_LENGTH('dbo.RepairRecord', 'RepairWarrantyPeriodUnit') IS NULL
BEGIN
    ALTER TABLE dbo.RepairRecord ADD RepairWarrantyPeriodUnit NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('dbo.RepairRecord', 'RepairWarrantyConditions') IS NULL
BEGIN
    ALTER TABLE dbo.RepairRecord ADD RepairWarrantyConditions NVARCHAR(MAX) NULL;
END
GO
