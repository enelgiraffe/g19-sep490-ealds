/*
  Asset allocation request flow — aligns with migration AddAssetAllocationRequestFlow.
  Run against your Ealds database (e.g. EaldsDB).

  - Adds AssetRequest.AllocationTargetDepartmentId
  - Creates AssetAllocationOrder, AssetAllocationOrderLine
  - Seeds RequestTypeId = 6 (same WorkflowId as RequestTypeId = 5) when missing
  - Section 6: AssetAllocationOrder reporting columns (requester, request time, receipt confirmation user)

  Idempotent: safe to run more than once if objects already exist.
*/

SET NOCOUNT ON;

/* 1) Column on AssetRequest */
IF COL_LENGTH(N'dbo.AssetRequest', N'AllocationTargetDepartmentId') IS NULL
BEGIN
    ALTER TABLE [dbo].[AssetRequest]
        ADD [AllocationTargetDepartmentId] INT NULL;
END
GO

/* 2) Table AssetAllocationOrder */
IF OBJECT_ID(N'dbo.AssetAllocationOrder', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssetAllocationOrder] (
        [AssetAllocationOrderId] INT NOT NULL IDENTITY(1, 1),
        [AssetRequestId]       INT NOT NULL,
        [DepartmentId]         INT NOT NULL,
        [Status]               TINYINT NOT NULL,
        [CreatedAt]            DATETIME2 NOT NULL CONSTRAINT [DF_AssetAllocationOrder_CreatedAt] DEFAULT (GETUTCDATE()),
        [ConfirmedAt]          DATETIME2 NULL,
        CONSTRAINT [PK_AssetAllocationOrder] PRIMARY KEY CLUSTERED ([AssetAllocationOrderId] ASC),
        CONSTRAINT [FK_AssetAllocationOrder_AssetRequest_AssetRequestId]
            FOREIGN KEY ([AssetRequestId]) REFERENCES [dbo].[AssetRequest] ([AssetRequestId]),
        CONSTRAINT [FK_AssetAllocationOrder_Department_DepartmentId]
            FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Department] ([DepartmentId])
    );
END
GO

/* 3) Table AssetAllocationOrderLine */
IF OBJECT_ID(N'dbo.AssetAllocationOrderLine', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssetAllocationOrderLine] (
        [AssetAllocationOrderLineId] INT NOT NULL IDENTITY(1, 1),
        [AssetAllocationOrderId]     INT NOT NULL,
        [AssetTypeId]                INT NOT NULL,
        [AssetId]                    INT NOT NULL,
        [Quantity]                   INT NOT NULL,
        [Reason]                     NVARCHAR(2000) NULL,
        CONSTRAINT [PK_AssetAllocationOrderLine] PRIMARY KEY CLUSTERED ([AssetAllocationOrderLineId] ASC),
        CONSTRAINT [FK_AssetAllocationOrderLine_AssetAllocationOrder_AssetAllocationOrderId]
            FOREIGN KEY ([AssetAllocationOrderId]) REFERENCES [dbo].[AssetAllocationOrder] ([AssetAllocationOrderId])
            ON DELETE CASCADE,
        CONSTRAINT [FK_AssetAllocationOrderLine_Asset_AssetId]
            FOREIGN KEY ([AssetId]) REFERENCES [dbo].[Asset] ([AssetId]),
        CONSTRAINT [FK_AssetAllocationOrderLine_AssetType_AssetTypeId]
            FOREIGN KEY ([AssetTypeId]) REFERENCES [dbo].[AssetType] ([AssetTypeId])
    );
END
GO

/* 4) Indexes */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'AssetRequest' AND i.name = N'IX_AssetRequest_AllocationTargetDepartmentId'
)
    CREATE NONCLUSTERED INDEX [IX_AssetRequest_AllocationTargetDepartmentId]
        ON [dbo].[AssetRequest] ([AllocationTargetDepartmentId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrder' AND i.name = N'IX_AssetAllocationOrder_AssetRequestId'
)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_AssetAllocationOrder_AssetRequestId]
        ON [dbo].[AssetAllocationOrder] ([AssetRequestId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrder' AND i.name = N'IX_AssetAllocationOrder_DepartmentId'
)
    CREATE NONCLUSTERED INDEX [IX_AssetAllocationOrder_DepartmentId]
        ON [dbo].[AssetAllocationOrder] ([DepartmentId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrderLine' AND i.name = N'IX_AssetAllocationOrderLine_AssetAllocationOrderId'
)
    CREATE NONCLUSTERED INDEX [IX_AssetAllocationOrderLine_AssetAllocationOrderId]
        ON [dbo].[AssetAllocationOrderLine] ([AssetAllocationOrderId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrderLine' AND i.name = N'IX_AssetAllocationOrderLine_AssetId'
)
    CREATE NONCLUSTERED INDEX [IX_AssetAllocationOrderLine_AssetId]
        ON [dbo].[AssetAllocationOrderLine] ([AssetId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrderLine' AND i.name = N'IX_AssetAllocationOrderLine_AssetTypeId'
)
    CREATE NONCLUSTERED INDEX [IX_AssetAllocationOrderLine_AssetTypeId]
        ON [dbo].[AssetAllocationOrderLine] ([AssetTypeId]);
GO

/* 5) Seed RequestType 6 (allocation) — workflow: 5 (disposal) → 3 (transfer) → 1 (purchase) */
IF NOT EXISTS (SELECT 1 FROM [dbo].[RequestType] WHERE [RequestTypeId] = 6)
BEGIN
    DECLARE @wf6 INT =
        (SELECT TOP (1) [WorkflowId] FROM [dbo].[RequestType] WHERE [RequestTypeId] = 5);
    IF @wf6 IS NULL
        SET @wf6 = (SELECT TOP (1) [WorkflowId] FROM [dbo].[RequestType] WHERE [RequestTypeId] = 3);
    IF @wf6 IS NULL
        SET @wf6 = (SELECT TOP (1) [WorkflowId] FROM [dbo].[RequestType] WHERE [RequestTypeId] = 1);

    IF @wf6 IS NOT NULL
    BEGIN
        SET IDENTITY_INSERT [dbo].[RequestType] ON;
        INSERT INTO [dbo].[RequestType] ([RequestTypeId], [WorkflowId]) VALUES (6, @wf6);
        SET IDENTITY_INSERT [dbo].[RequestType] OFF;
    END
    ELSE
        PRINT N'Warning: No RequestType 5/3/1 found; RequestType 6 was not inserted. Add any RequestType with WorkflowId first.';
END
GO

/* 6) Reporting: requester, request time snapshot, who confirmed receipt */
IF COL_LENGTH(N'dbo.AssetAllocationOrder', N'RequestedByUserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[AssetAllocationOrder] ADD [RequestedByUserId] INT NULL;
END
GO

IF COL_LENGTH(N'dbo.AssetAllocationOrder', N'RequestSubmittedAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[AssetAllocationOrder] ADD [RequestSubmittedAt] DATETIME2 NULL;
END
GO

IF COL_LENGTH(N'dbo.AssetAllocationOrder', N'ConfirmedByUserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[AssetAllocationOrder] ADD [ConfirmedByUserId] INT NULL;
END
GO

UPDATE o
SET
    o.[RequestedByUserId] = ar.[UserId],
    o.[RequestSubmittedAt] = ar.[CreateDate]
FROM [dbo].[AssetAllocationOrder] o
INNER JOIN [dbo].[AssetRequest] ar ON ar.[AssetRequestId] = o.[AssetRequestId]
WHERE o.[RequestedByUserId] IS NULL;
GO

UPDATE [dbo].[AssetAllocationOrder]
SET [RequestSubmittedAt] = [CreatedAt]
WHERE [RequestSubmittedAt] IS NULL;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrder' AND c.name = N'RequestedByUserId' AND c.is_nullable = 1
)
BEGIN
    ALTER TABLE [dbo].[AssetAllocationOrder] ALTER COLUMN [RequestedByUserId] INT NOT NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrder' AND c.name = N'RequestSubmittedAt' AND c.is_nullable = 1
)
BEGIN
    ALTER TABLE [dbo].[AssetAllocationOrder] ALTER COLUMN [RequestSubmittedAt] DATETIME2 NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrder' AND i.name = N'IX_AssetAllocationOrder_RequestedByUserId'
)
    CREATE NONCLUSTERED INDEX [IX_AssetAllocationOrder_RequestedByUserId]
        ON [dbo].[AssetAllocationOrder] ([RequestedByUserId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = N'AssetAllocationOrder' AND i.name = N'IX_AssetAllocationOrder_ConfirmedByUserId'
)
    CREATE NONCLUSTERED INDEX [IX_AssetAllocationOrder_ConfirmedByUserId]
        ON [dbo].[AssetAllocationOrder] ([ConfirmedByUserId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssetAllocationOrder_User_RequestedByUserId'
)
    ALTER TABLE [dbo].[AssetAllocationOrder]
        ADD CONSTRAINT [FK_AssetAllocationOrder_User_RequestedByUserId]
        FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[User] ([UserId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssetAllocationOrder_User_ConfirmedByUserId'
)
    ALTER TABLE [dbo].[AssetAllocationOrder]
        ADD CONSTRAINT [FK_AssetAllocationOrder_User_ConfirmedByUserId]
        FOREIGN KEY ([ConfirmedByUserId]) REFERENCES [dbo].[User] ([UserId]);
GO

PRINT N'Done: Asset allocation request schema update.';
GO
