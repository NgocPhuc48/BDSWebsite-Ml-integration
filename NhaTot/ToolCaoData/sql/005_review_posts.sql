IF COL_LENGTH('dbo.DuLieuThuThap', 'MaLoaiBDSDaDuyet') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD MaLoaiBDSDaDuyet int NULL;
GO
IF COL_LENGTH('dbo.DuLieuThuThap', 'MaPhuongXaDaDuyet') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD MaPhuongXaDaDuyet int NULL;
GO
IF COL_LENGTH('dbo.DuLieuThuThap', 'LoaiGiaoDichDaDuyet') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD LoaiGiaoDichDaDuyet nvarchar(30) NULL;
GO
IF COL_LENGTH('dbo.DuLieuThuThap', 'LoaiNoiDungDaDuyet') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD LoaiNoiDungDaDuyet nvarchar(30) NULL;
GO
IF OBJECT_ID('dbo.LichSuXuLyDuLieu', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuXuLyDuLieu (
        MaXuLy bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MaDuLieu bigint NULL,
        BuocXuLy nvarchar(100) NULL,
        KetQua nvarchar(20) NULL,
        ChiTiet nvarchar(max) NULL,
        ThoiDiem datetime2 NULL
    );
END;
