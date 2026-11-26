using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Services;

public static class SocialBotSchemaInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NhaTotDbContext>();

        await db.Database.OpenConnectionAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        async Task ExecuteAsync(string sql)
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        var isSqlServer = (db.Database.ProviderName ?? "").Contains("SqlServer", StringComparison.OrdinalIgnoreCase);

        if (isSqlServer)
        {
            await ExecuteAsync(@"
                IF OBJECT_ID(N'dbo.SocialBotSchemaVersions', N'U') IS NULL
                CREATE TABLE dbo.SocialBotSchemaVersions (
                    Version varchar(20) NOT NULL PRIMARY KEY,
                    AppliedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
                )");

            await ExecuteAsync(@"
                IF OBJECT_ID(N'dbo.LichCaoDuLieu', N'U') IS NULL
                CREATE TABLE dbo.LichCaoDuLieu (
                    Id int NOT NULL PRIMARY KEY,
                    Enabled bit NOT NULL DEFAULT 0,
                    IntervalMinutes int NOT NULL DEFAULT 60,
                    PostsPerSource int NOT NULL DEFAULT 10,
                    LookbackDays int NOT NULL DEFAULT 1,
                    NextRunUtc datetime2 NULL,
                    LastStartedUtc datetime2 NULL,
                    ActiveRunId varchar(100) NULL,
                    SourcesJson nvarchar(max) NULL,
                    Status varchar(30) NOT NULL DEFAULT 'IDLE',
                    LastMessage nvarchar(1000) NULL,
                    EncryptedApifyToken nvarchar(max) NULL
                )");

            await ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.LichCaoDuLieu WHERE Id = 1)
                INSERT INTO dbo.LichCaoDuLieu(Id, Enabled, IntervalMinutes, PostsPerSource, LookbackDays, Status)
                VALUES(1, 0, 60, 10, 1, 'IDLE')");
        }
        else
        {
            await ExecuteAsync("CREATE TABLE IF NOT EXISTS SocialBotSchemaVersions (Version varchar(20) NOT NULL PRIMARY KEY, AppliedUtc datetime NOT NULL DEFAULT CURRENT_TIMESTAMP)");

            await ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS LichCaoDuLieu (
                    Id int NOT NULL PRIMARY KEY,
                    Enabled boolean NOT NULL DEFAULT FALSE,
                    IntervalMinutes int NOT NULL DEFAULT 60,
                    PostsPerSource int NOT NULL DEFAULT 10,
                    LookbackDays int NOT NULL DEFAULT 1,
                    NextRunUtc datetime NULL,
                    LastStartedUtc datetime NULL,
                    ActiveRunId varchar(100) NULL,
                    SourcesJson longtext NULL,
                    Status varchar(30) NOT NULL DEFAULT 'IDLE',
                    LastMessage varchar(1000) NULL,
                    EncryptedApifyToken longtext NULL
                ) ENGINE=InnoDB");

            await ExecuteAsync("INSERT IGNORE INTO LichCaoDuLieu(Id, Enabled, IntervalMinutes, PostsPerSource, LookbackDays, Status) VALUES(1, FALSE, 60, 10, 1, 'IDLE')");
        }

        async Task EnsureColumnAsync(string columnName, string mysqlDefinition, string sqlServerDefinition)
        {
            await using var check = db.Database.GetDbConnection().CreateCommand();
            check.Transaction = transaction.GetDbTransaction();
            check.CommandText = isSqlServer
                ? "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DuLieuThuThap' AND COLUMN_NAME = @columnName"
                : "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'DuLieuThuThap' AND COLUMN_NAME = @columnName";
            var parameter = check.CreateParameter();
            parameter.ParameterName = "@columnName";
            parameter.Value = columnName;
            check.Parameters.Add(parameter);
            var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
            if (!exists)
            {
                var sql = isSqlServer
                    ? $"ALTER TABLE dbo.DuLieuThuThap ADD {columnName} {sqlServerDefinition}"
                    : $"ALTER TABLE DuLieuThuThap ADD COLUMN {columnName} {mysqlDefinition}";
                await ExecuteAsync(sql);
            }
        }

        await EnsureColumnAsync("NoiDungGoc", "longtext NULL", "nvarchar(max) NULL");
        await EnsureColumnAsync("DuLieuGocJson", "longtext NULL", "nvarchar(max) NULL");
        await EnsureColumnAsync("MaLoaiBdsDaDuyet", "int NULL", "int NULL");
        await EnsureColumnAsync("MaPhuongXaDaDuyet", "int NULL", "int NULL");
        await EnsureColumnAsync("LoaiGiaoDichDaDuyet", "varchar(30) NULL", "nvarchar(30) NULL");
        await EnsureColumnAsync("LoaiNoiDungDaDuyet", "varchar(30) NULL", "nvarchar(30) NULL");
        await EnsureColumnAsync("GiaRaoVND", "varchar(200) NULL", "nvarchar(200) NULL");
        await EnsureColumnAsync("GiaMinVND", "decimal(18, 2) NULL", "decimal(18, 2) NULL");
        await EnsureColumnAsync("GiaMaxVND", "decimal(18, 2) NULL", "decimal(18, 2) NULL");

        if (isSqlServer)
        {
            await ExecuteAsync(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'DuLieuThuThap' AND COLUMN_NAME = 'GiaRaoVND' AND DATA_TYPE <> 'nvarchar'
                )
                BEGIN
                    ALTER TABLE dbo.DuLieuThuThap ALTER COLUMN GiaRaoVND nvarchar(200) NULL;
                END");
        }

        // Đảm bảo loại trừ lẫn nhau: nếu GiaMinVND == GiaMaxVND thì chuyển sang GiaRaoVND và để trống khoảng giá;
        // nếu có GiaRaoVND cụ thể thì để trống GiaMinVND/GiaMaxVND.
        if (isSqlServer)
        {
            await ExecuteAsync("UPDATE DuLieuThuThap SET GiaRaoVND = CAST(CAST(GiaMinVND AS decimal(18, 0)) AS nvarchar(200)), GiaMinVND = NULL, GiaMaxVND = NULL WHERE GiaMinVND IS NOT NULL AND GiaMaxVND IS NOT NULL AND GiaMinVND = GiaMaxVND");
            await ExecuteAsync("UPDATE DuLieuThuThap SET GiaMinVND = NULL, GiaMaxVND = NULL WHERE GiaRaoVND IS NOT NULL AND LTRIM(RTRIM(GiaRaoVND)) <> '' AND GiaMinVND IS NOT NULL");
        }
        else
        {
            await ExecuteAsync("UPDATE DuLieuThuThap SET GiaRaoVND = CAST(GiaMinVND AS CHAR(200)), GiaMinVND = NULL, GiaMaxVND = NULL WHERE GiaMinVND IS NOT NULL AND GiaMaxVND IS NOT NULL AND GiaMinVND = GiaMaxVND");
            await ExecuteAsync("UPDATE DuLieuThuThap SET GiaMinVND = NULL, GiaMaxVND = NULL WHERE GiaRaoVND IS NOT NULL AND TRIM(GiaRaoVND) <> '' AND GiaMinVND IS NOT NULL");
        }

        await transaction.CommitAsync();

        await EnsurePropertyTypesAsync(db);
        await BackfillPriceRangesAsync(db);
    }

    private static async Task BackfillPriceRangesAsync(NhaTotDbContext db)
    {
        var candidates = await db.DuLieuThuThaps
            .Where(x => x.TrangThaiXuLy != "APPROVED" && x.NoiDungGoc != null)
            .ToListAsync();

        var updated = false;
        foreach (var row in candidates)
        {
            var (exact, pMin, pMax) = RealEstateTextExtractor.ExtractPriceInfo(row.NoiDungGoc);
            if (exact.HasValue)
            {
                var exactStr = exact.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                if (row.GiaRaoVnd != exactStr || row.GiaMinVnd != null || row.GiaMaxVnd != null)
                {
                    row.GiaRaoVnd = exactStr;
                    row.GiaMinVnd = null;
                    row.GiaMaxVnd = null;
                    updated = true;
                }
            }
            else if (pMin.HasValue && pMax.HasValue && string.IsNullOrWhiteSpace(row.GiaRaoVnd))
            {
                if (row.GiaMinVnd != pMin || row.GiaMaxVnd != pMax)
                {
                    row.GiaMinVnd = pMin;
                    row.GiaMaxVnd = pMax;
                    updated = true;
                }
            }
        }
        if (updated)
            await db.SaveChangesAsync();
    }

    private static async Task EnsurePropertyTypesAsync(NhaTotDbContext db)
    {
        var existing = await db.LoaiBds.ToListAsync();
        var renames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bán Căn hộ chung cư"] = "Căn hộ",
            ["Bán Đất nền"] = "Đất nền",
            ["Bán Nhà riêng / Biệt thự"] = "Nhà",
            ["Bán Nhà riêng"] = "Nhà",
            ["Cho thuê Nhà trọ / Căn hộ"] = "Cho thuê căn hộ"
        };

        var changed = false;
        foreach (var item in existing)
        {
            var currentName = (item.TenLoaiBds ?? "").Trim();
            if (renames.TryGetValue(currentName, out var targetName))
            {
                item.TenLoaiBds = targetName;
                item.TrangThai ??= true;
                changed = true;
            }
            else if (currentName.StartsWith("Bán ", StringComparison.OrdinalIgnoreCase))
            {
                item.TenLoaiBds = currentName[4..].Trim();
                changed = true;
            }
        }

        var requiredTypes = new (string Name, string Description)[]
        {
            ("Nhà", "Nhà ở riêng lẻ, nhà phố"),
            ("Căn hộ", "Căn hộ"),
            ("Chung cư", "Chung cư"),
            ("Đất nền", "Đất nền, đất thổ cư, đất dự án"),
            ("Biệt thự", "Biệt thự, liền kề")
        };

        foreach (var (name, desc) in requiredTypes)
        {
            var match = existing.FirstOrDefault(x => string.Equals((x.TenLoaiBds ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                var added = new LoaiBd { TenLoaiBds = name, MoTa = desc, TrangThai = true };
                db.LoaiBds.Add(added);
                existing.Add(added);
                changed = true;
            }
            else if (match.TrangThai != true)
            {
                match.TrangThai = true;
                changed = true;
            }
        }

        var rentalItems = existing
            .Where(x => (x.TenLoaiBds ?? "").Trim().StartsWith("Cho thuê", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var rentItem in rentalItems)
        {
            var id = rentItem.MaLoaiBds;
            var inUse = id > 0 && (
                await db.BatDongSans.AnyAsync(x => x.MaLoaiBds == id) ||
                await db.LichSuDinhGias.AnyAsync(x => x.MaLoaiBds == id) ||
                await db.MucTieuTheoDoiGias.AnyAsync(x => x.MaLoaiBds == id) ||
                await db.NhuCauTimKiems.AnyAsync(x => x.MaLoaiBds == id) ||
                await db.DuLieuThuThaps.AnyAsync(x => x.MaLoaiBdsDaDuyet == id));

            if (inUse)
            {
                if (rentItem.TrangThai != false)
                {
                    rentItem.TrangThai = false;
                    changed = true;
                }
            }
            else
            {
                db.LoaiBds.Remove(rentItem);
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync();
    }
}
