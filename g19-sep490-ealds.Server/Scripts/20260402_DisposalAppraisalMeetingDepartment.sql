-- Them MeetingDepartmentId cho DisposalAppraisal (dia diem = phong ban)
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    IF COL_LENGTH(N'dbo.DisposalAppraisal', N'MeetingDepartmentId') IS NULL
    BEGIN
        ALTER TABLE dbo.DisposalAppraisal
        ADD MeetingDepartmentId INT NULL;

        ALTER TABLE dbo.DisposalAppraisal
        ADD CONSTRAINT FK_DisposalAppraisal_MeetingDepartment
            FOREIGN KEY (MeetingDepartmentId) REFERENCES dbo.Department(DepartmentId);
    END;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
