IF COL_LENGTH('dbo.DuLieuThuThap', 'MaTinNguon') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD MaTinNguon nvarchar(200) NULL;
GO
IF COL_LENGTH('dbo.DuLieuThuThap', 'URLTin') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD URLTin nvarchar(1500) NULL;
GO
IF COL_LENGTH('dbo.DuLieuThuThap', 'DuLieuGocJson') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD DuLieuGocJson nvarchar(max) NULL;
GO
IF COL_LENGTH('dbo.DuLieuThuThap', 'HashNoiDung') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD HashNoiDung char(64) NULL;
GO
IF COL_LENGTH('dbo.DuLieuThuThap', 'NgayThuThap') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD NgayThuThap datetime2 NULL;
GO
IF COL_LENGTH('dbo.DuLieuThuThap', 'TrangThaiXuLy') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD TrangThaiXuLy varchar(20) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DuLieuThuThap_Hash' AND object_id = OBJECT_ID('dbo.DuLieuThuThap'))
    CREATE UNIQUE INDEX UX_DuLieuThuThap_Hash ON dbo.DuLieuThuThap(HashNoiDung) WHERE HashNoiDung IS NOT NULL;
