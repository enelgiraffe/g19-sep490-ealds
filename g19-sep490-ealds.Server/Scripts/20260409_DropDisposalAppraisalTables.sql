-- =============================================
-- Script: Drop Disposal Appraisal Tables
-- Date: 2026-04-09
-- Description: 
--   Xóa các bảng liên quan đến Disposal Appraisal
--   (DisposalAppraisalReport, DisposalAppraisalMember, DisposalAppraisal)
--   Các bảng này không còn được sử dụng trong hệ thống
-- =============================================

USE [EALDS_F1]
GO

PRINT '=========================================='
PRINT 'BẮT ĐẦU XÓA CÁC BẢNG DISPOSAL APPRAISAL'
PRINT '=========================================='
PRINT ''

-- =============================================
-- BƯỚC 1: XÓA DisposalAppraisalReport
-- (phụ thuộc vào DisposalAppraisal)
-- =============================================

IF OBJECT_ID('dbo.DisposalAppraisalReport', 'U') IS NOT NULL
BEGIN
    PRINT '1. Đang xóa bảng: DisposalAppraisalReport'
    PRINT '   - Xóa các Foreign Keys...'
    
    -- Xóa FK: FK_DisposalAppraisalReport_Appraisal
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalReport_Appraisal')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisalReport] 
        DROP CONSTRAINT [FK_DisposalAppraisalReport_Appraisal]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisalReport_Appraisal'
    END
    
    -- Xóa FK: FK_DisposalAppraisalReport_DirectorReviewedBy
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalReport_DirectorReviewedBy')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisalReport] 
        DROP CONSTRAINT [FK_DisposalAppraisalReport_DirectorReviewedBy]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisalReport_DirectorReviewedBy'
    END
    
    -- Xóa FK: FK_DisposalAppraisalReport_SubmittedBy
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalReport_SubmittedBy')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisalReport] 
        DROP CONSTRAINT [FK_DisposalAppraisalReport_SubmittedBy]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisalReport_SubmittedBy'
    END
    
    -- Xóa FK: FK_DisposalAppraisalReport_UpdatedBy
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalReport_UpdatedBy')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisalReport] 
        DROP CONSTRAINT [FK_DisposalAppraisalReport_UpdatedBy]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisalReport_UpdatedBy'
    END
    
    -- Xóa bảng
    PRINT '   - Xóa bảng DisposalAppraisalReport...'
    DROP TABLE [dbo].[DisposalAppraisalReport]
    PRINT '   ✓ ĐÃ XÓA THÀNH CÔNG: DisposalAppraisalReport'
    PRINT ''
END
ELSE
BEGIN
    PRINT '1. Bảng DisposalAppraisalReport không tồn tại (đã xóa trước đó)'
    PRINT ''
END
GO

-- =============================================
-- BƯỚC 2: XÓA DisposalAppraisalMember
-- (phụ thuộc vào DisposalAppraisal)
-- =============================================

IF OBJECT_ID('dbo.DisposalAppraisalMember', 'U') IS NOT NULL
BEGIN
    PRINT '2. Đang xóa bảng: DisposalAppraisalMember'
    PRINT '   - Xóa các Foreign Keys...'
    
    -- Xóa FK: FK_DisposalAppraisalMember_AddedBy
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalMember_AddedBy')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisalMember] 
        DROP CONSTRAINT [FK_DisposalAppraisalMember_AddedBy]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisalMember_AddedBy'
    END
    
    -- Xóa FK: FK_DisposalAppraisalMember_Appraisal
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalMember_Appraisal')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisalMember] 
        DROP CONSTRAINT [FK_DisposalAppraisalMember_Appraisal]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisalMember_Appraisal'
    END
    
    -- Xóa FK: FK_DisposalAppraisalMember_User
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalMember_User')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisalMember] 
        DROP CONSTRAINT [FK_DisposalAppraisalMember_User]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisalMember_User'
    END
    
    -- Xóa bảng
    PRINT '   - Xóa bảng DisposalAppraisalMember...'
    DROP TABLE [dbo].[DisposalAppraisalMember]
    PRINT '   ✓ ĐÃ XÓA THÀNH CÔNG: DisposalAppraisalMember'
    PRINT ''
END
ELSE
BEGIN
    PRINT '2. Bảng DisposalAppraisalMember không tồn tại (đã xóa trước đó)'
    PRINT ''
END
GO

-- =============================================
-- BƯỚC 3: XÓA FK TỪ DisposalExecution
-- (trước khi xóa DisposalAppraisal)
-- =============================================

IF OBJECT_ID('dbo.DisposalExecution', 'U') IS NOT NULL
BEGIN
    PRINT '3. Xóa Foreign Key từ DisposalExecution đến DisposalAppraisal'
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalExecution_Appraisal')
    BEGIN
        ALTER TABLE [dbo].[DisposalExecution] 
        DROP CONSTRAINT [FK_DisposalExecution_Appraisal]
        PRINT '   ✓ Đã xóa: FK_DisposalExecution_Appraisal'
        PRINT ''
    END
    ELSE
    BEGIN
        PRINT '   - FK_DisposalExecution_Appraisal không tồn tại'
        PRINT ''
    END
END
GO

-- =============================================
-- BƯỚC 4: XÓA DisposalAppraisal (BẢNG CHÍNH)
-- =============================================

IF OBJECT_ID('dbo.DisposalAppraisal', 'U') IS NOT NULL
BEGIN
    PRINT '4. Đang xóa bảng: DisposalAppraisal'
    PRINT '   - Xóa các Foreign Keys...'
    
    -- Xóa FK: FK_DisposalAppraisal_AssetRequest
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_AssetRequest')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisal] 
        DROP CONSTRAINT [FK_DisposalAppraisal_AssetRequest]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisal_AssetRequest'
    END
    
    -- Xóa FK: FK_DisposalAppraisal_CreatedBy
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_CreatedBy')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisal] 
        DROP CONSTRAINT [FK_DisposalAppraisal_CreatedBy]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisal_CreatedBy'
    END
    
    -- Xóa FK: FK_DisposalAppraisal_MeetingDepartment
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_MeetingDepartment')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisal] 
        DROP CONSTRAINT [FK_DisposalAppraisal_MeetingDepartment]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisal_MeetingDepartment'
    END
    
    -- Xóa FK: FK_DisposalAppraisal_ReporterUser
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_ReporterUser')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisal] 
        DROP CONSTRAINT [FK_DisposalAppraisal_ReporterUser]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisal_ReporterUser'
    END
    
    -- Xóa FK: FK_DisposalAppraisal_UpdatedBy
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_UpdatedBy')
    BEGIN
        ALTER TABLE [dbo].[DisposalAppraisal] 
        DROP CONSTRAINT [FK_DisposalAppraisal_UpdatedBy]
        PRINT '     ✓ Đã xóa: FK_DisposalAppraisal_UpdatedBy'
    END
    
    -- Xóa bảng
    PRINT '   - Xóa bảng DisposalAppraisal...'
    DROP TABLE [dbo].[DisposalAppraisal]
    PRINT '   ✓ ĐÃ XÓA THÀNH CÔNG: DisposalAppraisal'
    PRINT ''
END
ELSE
BEGIN
    PRINT '4. Bảng DisposalAppraisal không tồn tại (đã xóa trước đó)'
    PRINT ''
END
GO

-- =============================================
-- TỔNG KẾT
-- =============================================

PRINT ''
PRINT '=========================================='
PRINT 'HOÀN TẤT XÓA CÁC BẢNG'
PRINT '=========================================='
PRINT ''
PRINT 'Các bảng đã xóa:'
PRINT '  ✓ DisposalAppraisalReport'
PRINT '  ✓ DisposalAppraisalMember'
PRINT '  ✓ DisposalAppraisal'
PRINT ''
PRINT 'Các Foreign Key đã xóa:'
PRINT '  ✓ FK_DisposalAppraisalReport_Appraisal'
PRINT '  ✓ FK_DisposalAppraisalReport_DirectorReviewedBy'
PRINT '  ✓ FK_DisposalAppraisalReport_SubmittedBy'
PRINT '  ✓ FK_DisposalAppraisalReport_UpdatedBy'
PRINT '  ✓ FK_DisposalAppraisalMember_AddedBy'
PRINT '  ✓ FK_DisposalAppraisalMember_Appraisal'
PRINT '  ✓ FK_DisposalAppraisalMember_User'
PRINT '  ✓ FK_DisposalExecution_Appraisal'
PRINT '  ✓ FK_DisposalAppraisal_AssetRequest'
PRINT '  ✓ FK_DisposalAppraisal_CreatedBy'
PRINT '  ✓ FK_DisposalAppraisal_MeetingDepartment'
PRINT '  ✓ FK_DisposalAppraisal_ReporterUser'
PRINT '  ✓ FK_DisposalAppraisal_UpdatedBy'
PRINT ''
PRINT '✓ SCRIPT THỰC THI THÀNH CÔNG!'
PRINT ''
GO
