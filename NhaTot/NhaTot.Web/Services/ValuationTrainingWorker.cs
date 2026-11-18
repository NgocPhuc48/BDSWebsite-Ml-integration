using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace NhaTot.Web.Services;

public sealed class ValuationTrainingWorker(IServiceScopeFactory scopes, ILogger<ValuationTrainingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        string? previous = null;
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var trainer = scope.ServiceProvider.GetRequiredService<ValuationTrainingService>();
                var rows = await trainer.DataAsync();
                var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rows))));
                if (rows.Length >= 60 && previous != fingerprint)
                {
                    previous = fingerprint;
                    logger.LogInformation("Huấn luyện Định giá: {Result}", await trainer.TrainAsync());
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (InvalidOperationException ex) { logger.LogInformation("Chưa huấn luyện Định giá: {Reason}", ex.Message); }
            catch (Exception ex) { logger.LogError(ex, "Không huấn luyện được Định giá; giữ nguyên mô hình trước."); }
        } while (await timer.WaitForNextTickAsync(ct));
    }
}
