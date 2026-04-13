-- =============================================
-- Script: Cleanup Unused Tables and Database Fixes
-- Date: 2026-04-09
-- Description: 
--   1. Drop unused tables (WarehouseAsset, DisposalAppraisal*)
--   2. Fix typos in DisposalRecord column names
--   3. Add missing indexes for performance
--   4. Add BudgetAllocation table
-- =============================================

USE [EALDS_F1]
GO

PRINT 'Starting database cleanup and fixes...'
GO

-- =============================================
-- PART 1: DROP UNUSED TABLES
-- =============================================
PRINT ''
PRINT '=== PART 1: Dropping unused tables ==='
GO

-- Drop DisposalAppraisalReport (depends on DisposalAppraisal)
IF OBJECT_ID('dbo.DisposalAppraisalReport', 'U') IS NOT NULL
BEGIN
    PRINT 'Dropping table: DisposalAppraisalReport'
    
    -- Drop foreign keys first
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalReport_Appraisal')
        ALTER TABLE [dbo].[DisposalAppraisalReport] DROP CONSTRAINT [FK_DisposalAppraisalReport_Appraisal]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalReport_DirectorReviewedBy')
        ALTER TABLE [dbo].[DisposalAppraisalReport] DROP CONSTRAINT [FK_DisposalAppraisalReport_DirectorReviewedBy]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalReport_SubmittedBy')
        ALTER TABLE [dbo].[DisposalAppraisalReport] DROP CONSTRAINT [FK_DisposalAppraisalReport_SubmittedBy]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalReport_UpdatedBy')
        ALTER TABLE [dbo].[DisposalAppraisalReport] DROP CONSTRAINT [FK_DisposalAppraisalReport_UpdatedBy]
    
    DROP TABLE [dbo].[DisposalAppraisalReport]
    PRINT '  ✓ DisposalAppraisalReport dropped'
END
ELSE
    PRINT '  - DisposalAppraisalReport does not exist'
GO

-- Drop DisposalAppraisalMember (depends on DisposalAppraisal)
IF OBJECT_ID('dbo.DisposalAppraisalMember', 'U') IS NOT NULL
BEGIN
    PRINT 'Dropping table: DisposalAppraisalMember'
    
    -- Drop foreign keys first
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalMember_AddedBy')
        ALTER TABLE [dbo].[DisposalAppraisalMember] DROP CONSTRAINT [FK_DisposalAppraisalMember_AddedBy]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalMember_Appraisal')
        ALTER TABLE [dbo].[DisposalAppraisalMember] DROP CONSTRAINT [FK_DisposalAppraisalMember_Appraisal]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisalMember_User')
        ALTER TABLE [dbo].[DisposalAppraisalMember] DROP CONSTRAINT [FK_DisposalAppraisalMember_User]
    
    DROP TABLE [dbo].[DisposalAppraisalMember]
    PRINT '  ✓ DisposalAppraisalMember dropped'
END
ELSE
    PRINT '  - DisposalAppraisalMember does not exist'
GO

-- Drop DisposalExecution FK to DisposalAppraisal before dropping DisposalAppraisal
IF OBJECT_ID('dbo.DisposalExecution', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalExecution_Appraisal')
    BEGIN
        PRINT 'Dropping FK: FK_DisposalExecution_Appraisal'
        ALTER TABLE [dbo].[DisposalExecution] DROP CONSTRAINT [FK_DisposalExecution_Appraisal]
        PRINT '  ✓ FK dropped'
    END
END
GO

-- Drop DisposalAppraisal
IF OBJECT_ID('dbo.DisposalAppraisal', 'U') IS NOT NULL
BEGIN
    PRINT 'Dropping table: DisposalAppraisal'
    
    -- Drop remaining foreign keys
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_AssetRequest')
        ALTER TABLE [dbo].[DisposalAppraisal] DROP CONSTRAINT [FK_DisposalAppraisal_AssetRequest]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_CreatedBy')
        ALTER TABLE [dbo].[DisposalAppraisal] DROP CONSTRAINT [FK_DisposalAppraisal_CreatedBy]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_MeetingDepartment')
        ALTER TABLE [dbo].[DisposalAppraisal] DROP CONSTRAINT [FK_DisposalAppraisal_MeetingDepartment]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_ReporterUser')
        ALTER TABLE [dbo].[DisposalAppraisal] DROP CONSTRAINT [FK_DisposalAppraisal_ReporterUser]
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DisposalAppraisal_UpdatedBy')
        ALTER TABLE [dbo].[DisposalAppraisal] DROP CONSTRAINT [FK_DisposalAppraisal_UpdatedBy]
    
    DROP TABLE [dbo].[DisposalAppraisal]
    PRINT '  ✓ DisposalAppraisal dropped'
END
ELSE
    PRINT '  - DisposalAppraisal does not exist'
GO

-- Drop WarehouseAsset (duplicate/unused table)
IF OBJECT_ID('dbo.WarehouseAsset', 'U') IS NOT NULL
BEGIN
    PRINT 'Dropping table: WarehouseAsset (duplicate/unused)'
    DROP TABLE [dbo].[WarehouseAsset]
    PRINT '  ✓ WarehouseAsset dropped'
END
ELSE
    PRINT '  - WarehouseAsset does not exist'
GO

-- =============================================
-- PART 2: FIX TYPOS IN DisposalRecord
-- =============================================
PRINT ''
PRINT '=== PART 2: Fixing typos in DisposalRecord ==='
GO

IF OBJECT_ID('dbo.DisposalRecord', 'U') IS NOT NULL
BEGIN
    -- Check if old columns exist
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisposalRecord') AND name = 'DiposalId')
    BEGIN
        PRINT 'Renaming columns in DisposalRecord...'
        
        -- Rename columns (using sp_rename)
        EXEC sp_rename 'dbo.DisposalRecord.DiposalId', 'DisposalId', 'COLUMN'
        PRINT '  ✓ DiposalId → DisposalId'
        
        EXEC sp_rename 'dbo.DisposalRecord.DiposalMethod', 'DisposalMethod', 'COLUMN'
        PRINT '  ✓ DiposalMethod → DisposalMethod'
        
        EXEC sp_rename 'dbo.DisposalRecord.DiposalValue', 'DisposalValue', 'COLUMN'
        PRINT '  ✓ DiposalValue → DisposalValue'
        
        EXEC sp_rename 'dbo.DisposalRecord.DiposalDate', 'DisposalDate', 'COLUMN'
        PRINT '  ✓ DiposalDate → DisposalDate'
    END
    ELSE
        PRINT '  - Columns already renamed or do not exist'
END
ELSE
    PRINT '  - DisposalRecord table does not exist'
GO

-- =============================================
-- PART 3: ADD MISSING INDEXES FOR PERFORMANCE
-- =============================================
PRINT ''
PRINT '=== PART 3: Adding missing indexes ==='
GO

-- Index on AssetRequest.Status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetRequest_Status' AND object_id = OBJECT_ID('dbo.AssetRequest'))
BEGIN
    PRINT 'Creating index: IX_AssetRequest_Status'
    CREATE NONCLUSTERED INDEX [IX_AssetRequest_Status] ON [dbo].[AssetRequest]([Status] ASC)
    PRINT '  ✓ Index created'
END
ELSE
    PRINT '  - IX_AssetRequest_Status already exists'
GO

-- Index on AssetInstance.Status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetInstance_Status' AND object_id = OBJECT_ID('dbo.AssetInstance'))
BEGIN
    PRINT 'Creating index: IX_AssetInstance_Status'
    CREATE NONCLUSTERED INDEX [IX_AssetInstance_Status] ON [dbo].[AssetInstance]([Status] ASC)
    PRINT '  ✓ Index created'
END
ELSE
    PRINT '  - IX_AssetInstance_Status already exists'
GO

-- Index on Procurement.Status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Procurement_Status' AND object_id = OBJECT_ID('dbo.Procurement'))
BEGIN
    PRINT 'Creating index: IX_Procurement_Status'
    CREATE NONCLUSTERED INDEX [IX_Procurement_Status] ON [dbo].[Procurement]([Status] ASC)
    PRINT '  ✓ Index created'
END
ELSE
    PRINT '  - IX_Procurement_Status already exists'
GO

-- Index on InventorySession.Status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventorySession_Status' AND object_id = OBJECT_ID('dbo.InventorySession'))
BEGIN
    PRINT 'Creating index: IX_InventorySession_Status'
    CREATE NONCLUSTERED INDEX [IX_InventorySession_Status] ON [dbo].[InventorySession]([Status] ASC)
    PRINT '  ✓ Index created'
END
ELSE
    PRINT '  - IX_InventorySession_Status already exists'
GO

-- Index on GoodsReceipt.Status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GoodsReceipt_Status' AND object_id = OBJECT_ID('dbo.GoodsReceipt'))
BEGIN
    PRINT 'Creating index: IX_GoodsReceipt_Status'
    CREATE NONCLUSTERED INDEX [IX_GoodsReceipt_Status] ON [dbo].[GoodsReceipt]([Status] ASC)
    PRINT '  ✓ Index created'
END
ELSE
    PRINT '  - IX_GoodsReceipt_Status already exists'
GO

-- Index on SupplierInvoice.Status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplierInvoice_Status' AND object_id = OBJECT_ID('dbo.SupplierInvoice'))
BEGIN
    PRINT 'Creating index: IX_SupplierInvoice_Status'
    CREATE NONCLUSTERED INDEX [IX_SupplierInvoice_Status] ON [dbo].[SupplierInvoice]([Status] ASC)
    PRINT '  ✓ Index created'
END
ELSE
    PRINT '  - IX_SupplierInvoice_Status already exists'
GO

-- =============================================
-- PART 4: ADD BudgetAllocation TABLE
-- =============================================
PRINT ''
PRINT '=== PART 4: Creating BudgetAllocation table ==='
GO

IF OBJECT_ID('dbo.BudgetAllocation', 'U') IS NULL
BEGIN
    PRINT 'Creating table: BudgetAllocation'
    
    CREATE TABLE [dbo].[BudgetAllocation](
        [BudgetAllocationId] [int] IDENTITY(1,1) NOT NULL,
        [DepartmentId] [int] NOT NULL,
        [FiscalYear] [int] NOT NULL,
        [Quarter] [int] NULL,
        [AllocatedAmount] [decimal](18, 2) NOT NULL,
        [SpentAmount] [decimal](18, 2) NOT NULL DEFAULT 0,
        [RemainingAmount] AS ([AllocatedAmount] - [SpentAmount]) PERSISTED,
        [Category] [nvarchar](100) NULL,
        [Description] [nvarchar](500) NULL,
        [Status] [int] NOT NULL DEFAULT 1, -- 1: Active, 0: Inactive
        [CreatedBy] [int] NOT NULL,
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedDate] [datetime] NULL,
        CONSTRAINT [PK_BudgetAllocation] PRIMARY KEY CLUSTERED ([BudgetAllocationId] ASC),
        CONSTRAINT [FK_BudgetAllocation_Department] FOREIGN KEY([DepartmentId]) REFERENCES [dbo].[Department]([DepartmentId]),
        CONSTRAINT [FK_BudgetAllocation_CreatedBy] FOREIGN KEY([CreatedBy]) REFERENCES [dbo].[User]([UserId]),
        CONSTRAINT [FK_BudgetAllocation_UpdatedBy] FOREIGN KEY([UpdatedBy]) REFERENCES [dbo].[User]([UserId]),
        CONSTRAINT [CK_BudgetAllocation_Quarter] CHECK ([Quarter] >= 1 AND [Quarter] <= 4),
        CONSTRAINT [CK_BudgetAllocation_FiscalYear] CHECK ([FiscalYear] >= 2020 AND [FiscalYear] <= 2100),
        CONSTRAINT [CK_BudgetAllocation_AllocatedAmount] CHECK ([AllocatedAmount] >= 0),
        CONSTRAINT [CK_BudgetAllocation_SpentAmount] CHECK ([SpentAmount] >= 0)
    )
    
    -- Add indexes
    CREATE NONCLUSTERED INDEX [IX_BudgetAllocation_DepartmentId] ON [dbo].[BudgetAllocation]([DepartmentId] ASC)
    CREATE NONCLUSTERED INDEX [IX_BudgetAllocation_FiscalYear] ON [dbo].[BudgetAllocation]([FiscalYear] ASC)
    CREATE NONCLUSTERED INDEX [IX_BudgetAllocation_Status] ON [dbo].[BudgetAllocation]([Status] ASC)
    
    PRINT '  ✓ BudgetAllocation table created with indexes'
END
ELSE
    PRINT '  - BudgetAllocation table already exists'
GO

-- =============================================
-- SUMMARY
-- =============================================
PRINT ''
PRINT '=== CLEANUP SUMMARY ==='
PRINT 'Tables dropped:'
PRINT '  - DisposalAppraisalReport'
PRINT '  - DisposalAppraisalMember'
PRINT '  - DisposalAppraisal'
PRINT '  - WarehouseAsset'
PRINT ''
PRINT 'Typos fixed:'
PRINT '  - DisposalRecord column names (Diposal → Disposal)'
PRINT ''
PRINT 'Indexes added:'
PRINT '  - IX_AssetRequest_Status'
PRINT '  - IX_AssetInstance_Status'
PRINT '  - IX_Procurement_Status'
PRINT '  - IX_InventorySession_Status'
PRINT '  - IX_GoodsReceipt_Status'
PRINT '  - IX_SupplierInvoice_Status'
PRINT ''
PRINT 'Tables created:'
PRINT '  - BudgetAllocation (with indexes)'
PRINT ''
PRINT '✓ Database cleanup and fixes completed successfully!'
GO
