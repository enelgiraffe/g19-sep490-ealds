-- Cột kết quả thẩm định (tách với tóm tắt / kiến nghị)
SET NOCOUNT ON;
IF COL_LENGTH('dbo.DisposalAppraisalReport', 'AppraisalOutcome') IS NULL
BEGIN
    ALTER TABLE dbo.DisposalAppraisalReport
    ADD AppraisalOutcome NVARCHAR(MAX) NULL;
END
