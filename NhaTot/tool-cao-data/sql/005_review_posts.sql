-- Chạy sau 003_raw_posts.sql, trong database ứng dụng, trước khi dùng phiên bản này.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.DuLieuThuThap', 'MaLoaiBdsDaDuyet') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD MaLoaiBdsDaDuyet int NULL;
IF COL_LENGTH('dbo.DuLieuThuThap', 'MaPhuongXaDaDuyet') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD MaPhuongXaDaDuyet int NULL;
IF COL_LENGTH('dbo.DuLieuThuThap', 'LoaiGiaoDichDaDuyet') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD LoaiGiaoDichDaDuyet nvarchar(20) NULL;
IF COL_LENGTH('dbo.DuLieuThuThap', 'LoaiNoiDungDaDuyet') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD LoaiNoiDungDaDuyet nvarchar(20) NULL;
COMMIT;
GO
IF COL_LENGTH('dbo.MucTieuTheoDoiGia', 'MaTinh') IS NULL
    ALTER TABLE dbo.MucTieuTheoDoiGia ADD MaTinh int NULL;
GO
-- Chỉ bổ sung tỉnh cho mục tiêu cũ khi có địa phương xác định, không dùng mặc định.
UPDATE m SET MaTinh = q.MaTinh
FROM dbo.MucTieuTheoDoiGia m
LEFT JOIN dbo.PhuongXa p ON p.MaPhuongXa = m.MaPhuongXa
JOIN dbo.QuanHuyen q ON q.MaQuanHuyen = COALESCE(m.MaQuanHuyen, p.MaQuanHuyen)
WHERE m.MaTinh IS NULL;
