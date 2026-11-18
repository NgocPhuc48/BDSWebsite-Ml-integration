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
        async Task<object?> Execute(string sql, bool scalar = false, string? version = null)
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandTimeout = 60;
            command.CommandText = sql;
            if (version != null)
            {
                var parameter = command.CreateParameter(); parameter.ParameterName = "@version"; parameter.Value = version;
                command.Parameters.Add(parameter);
            }
            return scalar ? await command.ExecuteScalarAsync() : await command.ExecuteNonQueryAsync();
        }
        var locked = await Execute("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource='NhaTot.SocialBot.Schema', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=60000; SELECT @r;", true);
        if (Convert.ToInt32(locked) < 0) throw new InvalidOperationException("Không lấy được khóa cập nhật database SocialBot.");
        await Execute("IF OBJECT_ID('dbo.SocialBotSchemaVersions', 'U') IS NULL CREATE TABLE dbo.SocialBotSchemaVersions (Version nvarchar(20) NOT NULL PRIMARY KEY, AppliedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME());");
        foreach (var version in new[] { "003", "005", "006", "007" })
        {
            if (Convert.ToInt32(await Execute("SELECT COUNT(*) FROM dbo.SocialBotSchemaVersions WHERE Version=@version", true, version)) != 0) continue;
            await using var stream = typeof(SocialBotSchemaInitializer).Assembly.GetManifestResourceStream("SocialBot.Sql." + version)
                ?? throw new InvalidOperationException("Thiếu bản cập nhật SocialBot " + version);
            using var reader = new StreamReader(stream);
            foreach (var batch in Regex.Split(await reader.ReadToEndAsync(), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                if (!string.IsNullOrWhiteSpace(batch)) await Execute(batch);
            await Execute("INSERT INTO dbo.SocialBotSchemaVersions (Version) VALUES (@version)", version: version);
        }
        await transaction.CommitAsync();
    }
}
