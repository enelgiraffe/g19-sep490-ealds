-- ============================================================
-- THÊM REQUEST TYPE CHO THANH LÝ (RequestTypeId = 5)
-- Chạy trên database EALDS.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @DisposalRequestTypeId INT = 5;
    DECLARE @TransferRequestTypeId INT = 3; -- ưu tiên lấy workflow giống điều chuyển
    DECLARE @PurchaseRequestTypeId INT = 1; -- fallback nếu thiếu request type 3
    DECLARE @WorkflowId INT;

    -- 1) Nếu đã có RequestTypeId = 5 thì không tạo lại
    IF EXISTS (SELECT 1 FROM RequestType WHERE RequestTypeId = @DisposalRequestTypeId)
    BEGIN
        PRINT N'RequestTypeId = 5 đã tồn tại. Không cần thêm mới.';
        SELECT RequestTypeId, WorkflowId
        FROM RequestType
        WHERE RequestTypeId = @DisposalRequestTypeId;

        COMMIT TRAN;
        RETURN;
    END;

    -- 2) Chọn workflow: ưu tiên từ request type 3 (Điều chuyển), fallback sang type 1 (Mua sắm)
    SELECT TOP (1) @WorkflowId = WorkflowId
    FROM RequestType
    WHERE RequestTypeId = @TransferRequestTypeId;

    IF @WorkflowId IS NULL
    BEGIN
        SELECT TOP (1) @WorkflowId = WorkflowId
        FROM RequestType
        WHERE RequestTypeId = @PurchaseRequestTypeId;
    END;

    -- 3) Nếu vẫn không có workflow phù hợp thì báo lỗi để tránh tạo dữ liệu mồ côi
    IF @WorkflowId IS NULL
    BEGIN
        THROW 50001, 'Khong tim thay WorkflowId tham chieu (RequestTypeId=3 hoac 1).', 1;
    END;

    -- 4) Tạo RequestType thanh lý (RequestTypeId là IDENTITY nên cần bật IDENTITY_INSERT)
    SET IDENTITY_INSERT RequestType ON;

    INSERT INTO RequestType (RequestTypeId, WorkflowId)
    VALUES (@DisposalRequestTypeId, @WorkflowId);

    SET IDENTITY_INSERT RequestType OFF;

    PRINT N'Đã thêm RequestTypeId = 5 thành công.';
    SELECT RequestTypeId, WorkflowId
    FROM RequestType
    WHERE RequestTypeId = @DisposalRequestTypeId;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF (OBJECT_ID('RequestType') IS NOT NULL)
    BEGIN
        BEGIN TRY
            SET IDENTITY_INSERT RequestType OFF;
        END TRY
        BEGIN CATCH
            -- ignore secondary error
        END CATCH
    END

    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorNumber INT = ERROR_NUMBER();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;
