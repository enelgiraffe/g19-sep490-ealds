-- ============================================================
-- KIỂM TRA VÀ SỬA RequestTypeId CHO AssetRequest ĐANG BỊ SAI
-- Chạy trên database EALDS.
-- RequestTypeId trong config: 1=Mua, 2=Bảo dưỡng, 3=Điều chuyển, 4=Sửa chữa
-- ============================================================

-- CẤU HÌNH (phải khớp với appsettings.json)
DECLARE @PurchaseRequestTypeId     INT = 1;
DECLARE @MaintenanceRequestTypeId INT = 2;
DECLARE @TransferRequestTypeId    INT = 3;
DECLARE @RepairRequestTypeId      INT = 4;

-- ========== BƯỚC 1: KIỂM TRA ==========
-- Xem từng request hiện đang có RequestTypeId là gì và "đúng ra" phải là gì (dựa vào bảng con)
SELECT
    ar.AssetRequestId,
    ar.RequestTypeId AS [RequestTypeId hiện tại],
    CASE
        WHEN tr.RecordId IS NOT NULL THEN @TransferRequestTypeId
        WHEN mt.TaskId IS NOT NULL THEN @MaintenanceRequestTypeId
        WHEN rt.TaskId IS NOT NULL THEN @RepairRequestTypeId
        WHEN p.ProcurementId IS NOT NULL THEN @PurchaseRequestTypeId
        ELSE NULL
    END AS [RequestTypeId đúng],
    CASE
        WHEN tr.RecordId IS NOT NULL THEN N'Điều chuyển'
        WHEN mt.TaskId IS NOT NULL THEN N'Bảo dưỡng'
        WHEN rt.TaskId IS NOT NULL THEN N'Sửa chữa'
        WHEN p.ProcurementId IS NOT NULL THEN N'Mua sắm'
        ELSE N'Chưa xác định'
    END AS [Loại thực tế],
    ar.Title
FROM AssetRequest ar
LEFT JOIN TransferRecord tr ON tr.AssetRequestId = ar.AssetRequestId
LEFT JOIN MaintenaceTask mt ON mt.AssetRequestId = ar.AssetRequestId
LEFT JOIN RepairTask rt ON rt.AssetRequestId = ar.AssetRequestId
LEFT JOIN Procurement p ON p.AssetRequestId = ar.AssetRequestId
ORDER BY ar.AssetRequestId;

-- Chỉ những dòng BỊ SAI (RequestTypeId hiện tại != RequestTypeId đúng)
SELECT
    ar.AssetRequestId,
    ar.RequestTypeId AS [RequestTypeId hiện tại],
    CASE
        WHEN tr.RecordId IS NOT NULL THEN @TransferRequestTypeId
        WHEN mt.TaskId IS NOT NULL THEN @MaintenanceRequestTypeId
        WHEN rt.TaskId IS NOT NULL THEN @RepairRequestTypeId
        WHEN p.ProcurementId IS NOT NULL THEN @PurchaseRequestTypeId
    END AS [RequestTypeId đúng],
    CASE
        WHEN tr.RecordId IS NOT NULL THEN N'Điều chuyển'
        WHEN mt.TaskId IS NOT NULL THEN N'Bảo dưỡng'
        WHEN rt.TaskId IS NOT NULL THEN N'Sửa chữa'
        WHEN p.ProcurementId IS NOT NULL THEN N'Mua sắm'
    END AS [Loại thực tế],
    ar.Title
FROM AssetRequest ar
LEFT JOIN TransferRecord tr ON tr.AssetRequestId = ar.AssetRequestId
LEFT JOIN MaintenaceTask mt ON mt.AssetRequestId = ar.AssetRequestId
LEFT JOIN RepairTask rt ON rt.AssetRequestId = ar.AssetRequestId
LEFT JOIN Procurement p ON p.AssetRequestId = ar.AssetRequestId
WHERE (
    (tr.RecordId IS NOT NULL AND ar.RequestTypeId <> @TransferRequestTypeId)
    OR (mt.TaskId IS NOT NULL AND ar.RequestTypeId <> @MaintenanceRequestTypeId)
    OR (rt.TaskId IS NOT NULL AND ar.RequestTypeId <> @RepairRequestTypeId)
    OR (p.ProcurementId IS NOT NULL AND ar.RequestTypeId <> @PurchaseRequestTypeId)
)
ORDER BY ar.AssetRequestId;

-- ========== BƯỚC 2: SỬA (chạy sau khi đã kiểm tra xong) ==========
-- Sửa: Điều chuyển (có trong TransferRecord)
UPDATE ar
SET ar.RequestTypeId = @TransferRequestTypeId
FROM AssetRequest ar
INNER JOIN TransferRecord tr ON tr.AssetRequestId = ar.AssetRequestId
WHERE ar.RequestTypeId <> @TransferRequestTypeId;

-- Sửa: Bảo dưỡng (có trong MaintenaceTask)
UPDATE ar
SET ar.RequestTypeId = @MaintenanceRequestTypeId
FROM AssetRequest ar
INNER JOIN MaintenaceTask mt ON mt.AssetRequestId = ar.AssetRequestId
WHERE ar.RequestTypeId <> @MaintenanceRequestTypeId;

-- Sửa: Sửa chữa (có trong RepairTask)
UPDATE ar
SET ar.RequestTypeId = @RepairRequestTypeId
FROM AssetRequest ar
INNER JOIN RepairTask rt ON rt.AssetRequestId = ar.AssetRequestId
WHERE ar.RequestTypeId <> @RepairRequestTypeId;

-- Sửa: Mua sắm (có trong Procurement)
UPDATE ar
SET ar.RequestTypeId = @PurchaseRequestTypeId
FROM AssetRequest ar
INNER JOIN Procurement p ON p.AssetRequestId = ar.AssetRequestId
WHERE ar.RequestTypeId <> @PurchaseRequestTypeId;

-- Kiểm tra lại sau khi sửa (không còn dòng nào = đã đúng hết)
SELECT
    ar.AssetRequestId,
    ar.RequestTypeId,
    CASE
        WHEN tr.RecordId IS NOT NULL THEN N'Điều chuyển'
        WHEN mt.TaskId IS NOT NULL THEN N'Bảo dưỡng'
        WHEN rt.TaskId IS NOT NULL THEN N'Sửa chữa'
        WHEN p.ProcurementId IS NOT NULL THEN N'Mua sắm'
        ELSE N'Chưa xác định'
    END AS [Loại],
    ar.Title
FROM AssetRequest ar
LEFT JOIN TransferRecord tr ON tr.AssetRequestId = ar.AssetRequestId
LEFT JOIN MaintenaceTask mt ON mt.AssetRequestId = ar.AssetRequestId
LEFT JOIN RepairTask rt ON rt.AssetRequestId = ar.AssetRequestId
LEFT JOIN Procurement p ON p.AssetRequestId = ar.AssetRequestId
ORDER BY ar.AssetRequestId;
