-- Failed login lockout: 5 wrong passwords -> 30 minute lockout (see AuthController)
IF COL_LENGTH(N'dbo.[User]', N'AccessFailedCount') IS NULL
BEGIN
    ALTER TABLE dbo.[User] ADD AccessFailedCount INT NOT NULL CONSTRAINT DF_User_AccessFailedCount DEFAULT (0);
END
GO

IF COL_LENGTH(N'dbo.[User]', N'LockoutEnd') IS NULL
BEGIN
    ALTER TABLE dbo.[User] ADD LockoutEnd DATETIME2 NULL;
END
GO
