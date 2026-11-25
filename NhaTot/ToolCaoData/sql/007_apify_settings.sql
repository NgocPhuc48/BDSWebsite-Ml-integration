IF COL_LENGTH('dbo.LichCaoDuLieu', 'EncryptedApifyToken') IS NULL
    ALTER TABLE dbo.LichCaoDuLieu ADD EncryptedApifyToken nvarchar(max) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.LichCaoDuLieu WHERE Id = 1)
    INSERT INTO dbo.LichCaoDuLieu(Id, Enabled, IntervalMinutes, PostsPerSource, LookbackDays, Status)
    VALUES(1, 0, 60, 10, 1, 'IDLE');
