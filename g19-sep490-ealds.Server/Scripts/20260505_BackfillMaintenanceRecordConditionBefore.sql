/*
  EALDS — Backfill MaintenanceRecord.ConditionBefore
  SQL Server script

  Mục tiêu:
    - Điền lại cột `MaintenanceRecord.ConditionBefore` bị trống.
    - Ưu tiên lấy từ `AssetRequest.Description` của request bảo dưỡng (RequestTypeId = 2).
    - Nếu vẫn trống, fallback sang `AssetInstance.Condition`.

  Cách dùng:
    1) Sao lưu DB trước khi chạy.
    2) Chỉnh @MaintenanceRequestTypeId nếu hệ thống bạn không dùng 2.
    3) Chạy toàn bộ file trong SSMS.
    4) Xem phần PRINT/SELECT ở cuối.
*/

SET NOCOUNT ON;

DECLARE @MaintenanceRequestTypeId INT = 2;
DECLARE @UseRequestTypeFilter BIT = 1; -- 1 = chỉ lấy AssetRequest có RequestTypeId = @MaintenanceRequestTypeId
DECLARE @ShouldCommit BIT = 0; -- mặc định rollback để xem kết quả; đổi về 1 nếu muốn commit

BEGIN TRAN;

-- Snapshot counts before
PRINT N'=== Snapshot trước cập nhật ===';
SELECT
  TotalMaintenanceRecords = COUNT(1),
  ConditionBeforeNullOrEmpty = SUM(
    CASE
      WHEN mr.ConditionBefore IS NULL THEN 1
      WHEN LTRIM(RTRIM(mr.ConditionBefore)) = N'' THEN 1
      ELSE 0
    END
  )
FROM dbo.MaintenanceRecord mr;

-- Step 1: Fill from AssetRequest.Description
PRINT N'=== Bước 1: Lấy từ AssetRequest.Description ===';

UPDATE mr
SET mr.ConditionBefore = LTRIM(RTRIM(ar.Description))
FROM dbo.MaintenanceRecord AS mr
INNER JOIN dbo.MaintenanceTask AS mt
  ON mt.TaskId = mr.TaskId
INNER JOIN dbo.AssetRequest AS ar
  ON ar.AssetRequestId = mt.AssetRequestId
WHERE (mr.ConditionBefore IS NULL OR LTRIM(RTRIM(mr.ConditionBefore)) = N'')
  AND ar.Description IS NOT NULL
  AND LTRIM(RTRIM(ar.Description)) <> N''
  AND (
    @UseRequestTypeFilter = 0
    OR ar.RequestTypeId = @MaintenanceRequestTypeId
  );

PRINT CONCAT(N'Rows updated (Step 1): ', @@ROWCOUNT);

-- Step 2: Fallback from AssetInstance.Condition
PRINT N'=== Bước 2: Fallback từ AssetInstance.Condition ===';

UPDATE mr
SET mr.ConditionBefore = LTRIM(RTRIM(ai.Condition))
FROM dbo.MaintenanceRecord AS mr
INNER JOIN dbo.AssetInstance AS ai
  ON ai.AssetInstanceId = mr.AssetInstanceId
WHERE (mr.ConditionBefore IS NULL OR LTRIM(RTRIM(mr.ConditionBefore)) = N'')
  AND ai.Condition IS NOT NULL
  AND LTRIM(RTRIM(ai.Condition)) <> N'';

PRINT CONCAT(N'Rows updated (Step 2): ', @@ROWCOUNT);

-- Snapshot counts after
PRINT N'=== Snapshot sau cập nhật ===';
SELECT
  TotalMaintenanceRecords = COUNT(1),
  ConditionBeforeNullOrEmpty = SUM(
    CASE
      WHEN mr.ConditionBefore IS NULL THEN 1
      WHEN LTRIM(RTRIM(mr.ConditionBefore)) = N'' THEN 1
      ELSE 0
    END
  )
FROM dbo.MaintenanceRecord mr;

IF @ShouldCommit = 1
BEGIN
  COMMIT TRAN;
  PRINT N'Đã COMMIT TRAN.';
END
ELSE
BEGIN
  ROLLBACK TRAN;
  PRINT N'Đã ROLLBACK TRAN (chạy thử).';
END

PRINT N'Hoàn tất backfill MaintenanceRecord.ConditionBefore.';

