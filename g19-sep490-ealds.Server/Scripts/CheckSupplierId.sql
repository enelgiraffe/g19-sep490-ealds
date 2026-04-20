-- Check for duplicate SupplierId columns in AssetInstance table
USE [EALDS_F2];
GO

-- This query will show if there are duplicate column names
SELECT name, COUNT(*) as duplicate_count
FROM sys.columns
WHERE object_id = OBJECT_ID('AssetInstance')
GROUP BY name
HAVING COUNT(*) > 1;

-- Also show all columns
SELECT column_id, name
FROM sys.columns
WHERE object_id = OBJECT_ID('AssetInstance')
ORDER BY column_id;
