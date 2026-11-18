-- Chạy sau 003 và 005. Lịch mặc định TẮT, không tự phát sinh lượt cào.
IF OBJECT_ID('dbo.LichCaoDuLieus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichCaoDuLieus (
        Id int NOT NULL PRIMARY KEY CHECK (Id = 1),
        Enabled bit NOT NULL DEFAULT 0,
        IntervalMinutes int NOT NULL DEFAULT 60 CHECK (IntervalMinutes BETWEEN 15 AND 1440),
        PostsPerSource int NOT NULL DEFAULT 10 CHECK (PostsPerSource BETWEEN 1 AND 100),
        LookbackDays int NOT NULL DEFAULT 1 CHECK (LookbackDays IN (1,7,30)),
        NextRunUtc datetime2 NULL, LastStartedUtc datetime2 NULL,
        ActiveRunId nvarchar(max) NULL, SourcesJson nvarchar(max) NULL,
        Status nvarchar(max) NOT NULL DEFAULT 'IDLE', LastMessage nvarchar(max) NULL
    );
END;
IF NOT EXISTS (SELECT 1 FROM dbo.LichCaoDuLieus WHERE Id = 1)
    INSERT INTO dbo.LichCaoDuLieus (Id) VALUES (1);
