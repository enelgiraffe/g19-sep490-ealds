/*
  Handover (thu hồi về kho) — RequestType 7 + AssetAllocationOrder.Kind
  Run after allocation flow script. Idempotent where possible.
*/

SET NOCOUNT ON;

/* 1) Order kind: 1 = cấp phát, 2 = thu hồi */
IF COL_LENGTH(N'dbo.AssetAllocationOrder', N'Kind') IS NULL
BEGIN
    ALTER TABLE [dbo].[AssetAllocationOrder]
        ADD [Kind] TINYINT NOT NULL CONSTRAINT [DF_AssetAllocationOrder_Kind] DEFAULT (1);
END
GO

/* 2) RequestType 7 — same workflow as allocation (6) */
IF NOT EXISTS (SELECT 1 FROM [dbo].[RequestType] WHERE [RequestTypeId] = 7)
BEGIN
    DECLARE @wf7 INT = (SELECT TOP (1) [WorkflowId] FROM [dbo].[RequestType] WHERE [RequestTypeId] = 6);
    IF @wf7 IS NULL
        SET @wf7 = (SELECT TOP (1) [WorkflowId] FROM [dbo].[RequestType] WHERE [RequestTypeId] = 5);
    IF @wf7 IS NULL
        SET @wf7 = (SELECT TOP (1) [WorkflowId] FROM [dbo].[RequestType] WHERE [RequestTypeId] = 1);

    IF @wf7 IS NOT NULL
    BEGIN
        SET IDENTITY_INSERT [dbo].[RequestType] ON;
        INSERT INTO [dbo].[RequestType] ([RequestTypeId], [WorkflowId]) VALUES (7, @wf7);
        SET IDENTITY_INSERT [dbo].[RequestType] OFF;
    END
    ELSE
        PRINT N'Warning: No workflow found; RequestType 7 not inserted.';
END
GO

PRINT N'Done: handover request type + order Kind.';
GO
