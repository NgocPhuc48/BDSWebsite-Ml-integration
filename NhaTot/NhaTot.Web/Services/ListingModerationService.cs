using Microsoft.Extensions.Caching.Memory;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Services;

public enum ModerationRiskLevel
{
    Unverified = 0,
    Normal = 1,
    Warning = 2,
    Critical = 3
}

public sealed record ModerationAssessment(
    ModerationRiskLevel Level,
    string BadgeText,
    string Summary,
    decimal? ListedPriceVnd,
    decimal? PredictedPriceVnd,
    double? DeviationPercent,
    string ModelName);

/// <summary>
/// Quy trình phân loại hỗ trợ Admin kiểm duyệt tin đăng bằng mô hình cây tăng cường (GBDT / XGBoost-style):
/// cảnh báo khi giá rao chênh lệch 100% -> 200% (hoặc > 200%) so với giá trị thực tế ước tính.
/// </summary>
public sealed class ListingModerationService(ValuationTrainingService training, IMemoryCache cache)
{
    private const string CacheKey = "NhaTot.Moderation.GbtModel.v1";

    public async Task<GbtArtifact?> GetOrTrainModelAsync()
    {
        if (cache.TryGetValue(CacheKey, out GbtArtifact? cached) && cached != null)
            return cached;

        var data = await training.DataAsync();
        if (data.Length < 60) return null;

        var artifact = GradientBoostedPriceModel.Train(data);
        cache.Set(CacheKey, artifact, TimeSpan.FromMinutes(30));
        return artifact;
    }

    public ModerationAssessment AssessListing(TinDang listing, GbtArtifact? model)
    {
        if (model == null)
        {
            return new ModerationAssessment(
                ModerationRiskLevel.Unverified,
                "Chưa có mô hình GBDT",
                "Chưa đủ dữ liệu sạch (≥ 60 mẫu) để huấn luyện cây GBDT đối soát giá.",
                listing.GiaRaoVnd,
                null,
                null,
                GbtArtifact.DefaultModelName);
        }

        if (!string.Equals(listing.LoaiGiaoDich, "BAN", StringComparison.OrdinalIgnoreCase))
        {
            return new ModerationAssessment(
                ModerationRiskLevel.Unverified,
                "Tin cho thuê",
                "Mô hình kiểm duyệt giá hiện áp dụng cho tin rao BÁN.",
                listing.GiaRaoVnd,
                null,
                null,
                model.ModelName);
        }

        var bds = listing.MaBdsNavigation;
        var area = bds?.DienTichDat ?? bds?.DienTichSuDung ?? 0m;
        var wardId = bds?.MaPhuongXa ?? 0;
        var distId = bds?.MaPhuongXaNavigation?.MaQuanHuyen ?? 0;
        var typeId = bds?.MaLoaiBds ?? 0;

        if (bds == null || area < 1 || wardId <= 0 || distId <= 0 || typeId <= 0)
        {
            return new ModerationAssessment(
                ModerationRiskLevel.Unverified,
                "Thiếu thông tin",
                "Thiếu diện tích, loại BĐS hoặc địa bàn phường/xã để cây GBDT ước tính giá thực tế.",
                listing.GiaRaoVnd,
                null,
                null,
                model.ModelName);
        }

        var refDate = listing.NgayDang ?? model.LastDate;
        if (refDate > model.LastDate.AddDays(180)) refDate = model.LastDate;

        var sample = new ValuationSample(
            $"check:{listing.MaTin}",
            typeId,
            distId,
            wardId,
            (double)area,
            0,
            refDate);

        var pred = model.Predict(sample);
        if (!double.IsFinite(pred) || pred <= 0)
        {
            return new ModerationAssessment(
                ModerationRiskLevel.Unverified,
                "Ngoài phạm vi",
                "Giá ước tính vượt ngoài phạm vi hợp lệ của mô hình.",
                listing.GiaRaoVnd,
                null,
                null,
                model.ModelName);
        }

        var predictedVnd = (decimal)Math.Round(pred);
        return ClassifyPriceDeviation(listing.GiaRaoVnd, predictedVnd, model.ModelName);
    }

    public static ModerationAssessment ClassifyPriceDeviation(
        decimal? listedPriceVnd,
        decimal? predictedPriceVnd,
        string modelName = GbtArtifact.DefaultModelName)
    {
        if (!listedPriceVnd.HasValue || listedPriceVnd.Value <= 0 || !predictedPriceVnd.HasValue || predictedPriceVnd.Value <= 0)
        {
            return new ModerationAssessment(
                ModerationRiskLevel.Unverified,
                "Chưa định giá được",
                "Tin đăng chưa có giá rao cụ thể (Thỏa thuận/Liên hệ) hoặc chưa ước tính được giá thực tế.",
                listedPriceVnd,
                predictedPriceVnd,
                null,
                modelName);
        }

        var listed = listedPriceVnd.Value;
        var predicted = predictedPriceVnd.Value;
        var deviationPct = Math.Round((double)((listed - predicted) / predicted) * 100.0, 1);

        if (deviationPct >= 200.0 || listed <= predicted / 3m)
        {
            var dirLabel = deviationPct >= 0 ? $"cao hơn +{deviationPct:0.#}%" : $"thấp hơn {Math.Abs(deviationPct):0.#}%";
            return new ModerationAssessment(
                ModerationRiskLevel.Critical,
                "Nguy cơ cao",
                $"Cảnh báo nghiêm trọng: giá rao {dirLabel} so với ước tính GBDT (~{RealEstateTextExtractor.FormatVndReadable(predicted)}). Cần kiểm tra kỹ trước khi duyệt hoặc từ chối.",
                listed,
                predicted,
                deviationPct,
                modelName);
        }

        if (deviationPct >= 100.0 || listed <= predicted * 0.5m)
        {
            var dirLabel = deviationPct >= 0 ? $"chênh lệch +{deviationPct:0.#}% (100%–200%)" : $"thấp bất thường -{Math.Abs(deviationPct):0.#}%";
            return new ModerationAssessment(
                ModerationRiskLevel.Warning,
                "Cảnh báo giá",
                $"Cảnh báo kiểm duyệt: giá rao {dirLabel} so với giá thị trường ước tính (~{RealEstateTextExtractor.FormatVndReadable(predicted)}). Đề nghị Admin xác minh trước khi duyệt.",
                listed,
                predicted,
                deviationPct,
                modelName);
        }

        return new ModerationAssessment(
            ModerationRiskLevel.Normal,
            "Bình thường",
            $"Giá rao hợp lý (chênh {(deviationPct >= 0 ? "+" : "")}{deviationPct:0.#}% so với ước tính GBDT ~{RealEstateTextExtractor.FormatVndReadable(predicted)}).",
            listed,
            predicted,
            deviationPct,
            modelName);
    }
}
