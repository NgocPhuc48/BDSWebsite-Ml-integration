IF COL_LENGTH('dbo.LichCaoDuLieus', 'EncryptedApifyToken') IS NULL
    ALTER TABLE dbo.LichCaoDuLieus ADD EncryptedApifyToken nvarchar(max) NULL;
