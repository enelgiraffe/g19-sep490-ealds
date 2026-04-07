-- ============================================================
-- THÊM CỘT AssetInstanceId VÀO BẢNG AssetRequest
-- Nguyên nhân: EF Core model có AssetInstanceId nhưng database thiếu cột này
-- Lỗi: "Invalid column name 'AssetInstanceId'" khi DirectorViewController.Get()
-- ============================================================

-- ========== BƯỚC 1: KIỂM TRA ==========
-- Xem cấu trúc hiện tại của bảng AssetRequest
EXEC sp_columns 'AssetRequest';

-- Kiểm tra xem cột đã tồn tại chưa
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AssetRequest' AND COLUMN_NAME = 'AssetInstanceId'
)
BEGIN
    PRINT N'Cột AssetInstanceId chưa tồn tại trong bảng AssetRequest - sẽ được tạo...';

    -- ========== BƯỚC 2: THÊM CỘT ==========
    ALTER TABLE AssetRequest
    ADD AssetInstanceId INT NULL;
    PRINT N'Đã thêm cột AssetInstanceId (nullable) vào bảng AssetRequest.';
END
ELSE
BEGIN
    PRINT N'Cột AssetInstanceId đã tồn tại trong bảng AssetRequest.';
END

-- ========== BƯỚC 3: THÊM KHÓA NGOẠI (nếu chưa có) ==========
-- Kiểm tra xem khóa ngoại đã tồn tại chưa
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK__AssetRequ__AssetInst'
)
BEGIN
    ALTER TABLE AssetRequest
    ADD CONSTRAINT FK__AssetRequ__AssetInst
    FOREIGN KEY (AssetInstanceId)
    REFERENCES AssetInstance(AssetInstanceId);
    PRINT N'Đã thêm khóa ngoại FK__AssetRequ__AssetInst.';
END
ELSE
BEGIN
    PRINT N'Khóa ngoại FK__AssetRequ__AssetInst đã tồn tại.';
END

-- ========== BƯỚC 4: KIỂM TRA SAU KHI THÊM ==========
EXEC sp_columns 'AssetRequest';
PRINT N'Hoàn tất kiểm tra cấu trúc bảng AssetRequest.';
