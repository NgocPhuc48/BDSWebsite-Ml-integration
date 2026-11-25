IF OBJECT_ID('dbo.LichCaoDuLieu', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichCaoDuLieu (
        Id int NOT NULL PRIMARY KEY,
        Enabled bit NOT NULL CONSTRAINT DF_LichCaoDuLieu_Enabled DEFAULT(0),
        IntervalMinutes int NOT NULL CONSTRAINT DF_LichCaoDuLieu_Interval DEFAULT(60),
        PostsPerSource int NOT NULL CONSTRAINT DF_LichCaoDuLieu_Posts DEFAULT(10),
        LookbackDays int NOT NULL CONSTRAINT DF_LichCaoDuLieu_Lookback DEFAULT(1),
        NextRunUtc datetime2 NULL,
        LastStartedUtc datetime2 NULL,
        ActiveRunId nvarchar(100) NULL,
        SourcesJson nvarchar(max) NULL,
        Status nvarchar(30) NOT NULL CONSTRAINT DF_LichCaoDuLieu_Status DEFAULT('IDLE'),
        LastMessage nvarchar(1000) NULL
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.LichCaoDuLieu WHERE Id = 1)
    INSERT INTO dbo.LichCaoDuLieu(Id, Enabled, IntervalMinutes, PostsPerSource, LookbackDays, Status)
    VALUES(1, 0, 60, 10, 1, 'IDLE');
