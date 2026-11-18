-- Chạy trong database NhaTot trước khi dùng chức năng lưu kết quả.
-- Có thể chạy lại; không thay đổi dữ liệu hiện có.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.DuLieuThuThap', 'NoiDungGoc') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD NoiDungGoc nvarchar(max) NULL;
IF COL_LENGTH('dbo.DuLieuThuThap', 'DuLieuGocJson') IS NULL
    ALTER TABLE dbo.DuLieuThuThap ADD DuLieuGocJson nvarchar(max) NULL;
COMMIT;
