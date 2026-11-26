using System.Globalization;
using System.Text;

namespace NhaTot.Web.Services;

public sealed record BayGiaDemandProfile(
    string TransactionType,
    int PropertyTypeId,
    string? PropertyTypeName,
    int? ProvinceId,
    int? DistrictId,
    int? WardId,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinArea,
    string? Direction,
    string? LegalStatus,
    int? MinBedrooms,
    decimal? MinFrontageM);

public sealed record BayGiaCandidate(
    string Source,
    long Id,
    string TransactionType,
    int? PropertyTypeId,
    string? PropertyTypeName,
    int? ProvinceId,
    int? DistrictId,
    int? WardId,
    decimal? Price,
    decimal? Area,
    string? Direction,
    string? LegalStatus,
    int? Bedrooms,
    decimal? FrontageM,
    DateTime? PublishedAt);

public sealed record BayGiaSimilarityResult(double ScorePercent, List<string> Reasons);

/// <summary>
/// Tính độ tương đồng đa tiêu chí giữa nhu cầu Bẫy giá của người dùng và tin đăng/bài thu thập.
/// </summary>
public static class BayGiaRecommendationEngine
{
    public const double DefaultMinSimilarityPercent = 80.0;

    public static string NormalizeTypeGroup(string? name)
    {
        var n = NormalizeText(name);
        if (string.IsNullOrEmpty(n)) return "";
        if (n.Contains("dat")) return "DAT";
        if (n.Contains("can ho") || n.Contains("chung cu")) return "CAN_HO";
        if (n.Contains("biet thu")) return "BIET_THU";
        if (n.Contains("nha") || n.Contains("shophouse") || n.Contains("mat bang")) return "NHA";
        return n.ToUpperInvariant();
    }

    public static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch == 'đ' ? 'd' : ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string NormalizeDirection(string? dir)
    {
        var n = NormalizeText(dir).Replace("chinh ", "").Replace(" ", "");
        return n;
    }

    public static BayGiaSimilarityResult CalculateSimilarity(BayGiaDemandProfile demand, BayGiaCandidate candidate)
    {
        if (!string.Equals(demand.TransactionType, candidate.TransactionType, StringComparison.OrdinalIgnoreCase))
            return new BayGiaSimilarityResult(0.0, []);

        double earned = 0;
        double totalWeight = 0;
        var reasons = new List<string>();

        // 1. Loại BĐS (trọng số 25)
        const double typeWeight = 25.0;
        totalWeight += typeWeight;
        var dGroup = NormalizeTypeGroup(demand.PropertyTypeName);
        var cGroup = NormalizeTypeGroup(candidate.PropertyTypeName);
        if (candidate.PropertyTypeId == demand.PropertyTypeId || (!string.IsNullOrEmpty(dGroup) && dGroup == cGroup))
        {
            earned += typeWeight;
            reasons.Add("Đúng loại BĐS");
        }
        else if ((dGroup == "NHA" && cGroup == "BIET_THU") || (dGroup == "BIET_THU" && cGroup == "NHA"))
        {
            earned += typeWeight * 0.65;
        }

        // 2. Khu vực (trọng số 25)
        if (demand.WardId is > 0 || demand.DistrictId is > 0 || demand.ProvinceId is > 0)
        {
            const double locWeight = 25.0;
            totalWeight += locWeight;
            if (demand.WardId is > 0)
            {
                if (candidate.WardId == demand.WardId)
                {
                    earned += locWeight;
                    reasons.Add("Đúng phường/xã");
                }
                else if (demand.DistrictId is > 0 && candidate.DistrictId == demand.DistrictId)
                {
                    earned += locWeight * 0.75;
                    reasons.Add("Cùng quận/huyện");
                }
                else if (demand.ProvinceId is > 0 && candidate.ProvinceId == demand.ProvinceId)
                {
                    earned += locWeight * 0.40;
                }
            }
            else if (demand.DistrictId is > 0)
            {
                if (candidate.DistrictId == demand.DistrictId)
                {
                    earned += locWeight;
                    reasons.Add("Đúng quận/huyện");
                }
                else if (demand.ProvinceId is > 0 && candidate.ProvinceId == demand.ProvinceId)
                {
                    earned += locWeight * 0.55;
                    reasons.Add("Cùng tỉnh/thành");
                }
            }
            else if (demand.ProvinceId is > 0 && candidate.ProvinceId == demand.ProvinceId)
            {
                earned += locWeight;
                reasons.Add("Đúng tỉnh/thành");
            }
        }

        // 3. Ngân sách (trọng số 30)
        if (demand.MaxPrice is > 0 || demand.MinPrice is > 0)
        {
            const double priceWeight = 30.0;
            totalWeight += priceWeight;
            if (!candidate.Price.HasValue || candidate.Price.Value <= 0)
            {
                earned += priceWeight * 0.50;
            }
            else
            {
                var p = candidate.Price.Value;
                var minP = demand.MinPrice ?? 0m;
                var maxP = demand.MaxPrice is > 0 ? demand.MaxPrice.Value : decimal.MaxValue;
                if (p >= minP && p <= maxP)
                {
                    earned += priceWeight;
                    reasons.Add("Trong tầm giá");
                }
                else if (p > maxP && maxP > 0)
                {
                    var over = (double)((p - maxP) / maxP);
                    var ratio = Math.Clamp(1.0 - over / 0.30, 0.0, 1.0);
                    earned += priceWeight * ratio;
                    if (over <= 0.10) reasons.Add("Gần ngân sách");
                }
                else if (p < minP && minP > 0)
                {
                    var under = (double)((minP - p) / minP);
                    earned += priceWeight * Math.Clamp(1.0 - under / 0.30, 0.0, 1.0);
                }
            }
        }

        // 4. Diện tích tối thiểu (trọng số 15)
        if (demand.MinArea is > 0)
        {
            const double areaWeight = 15.0;
            totalWeight += areaWeight;
            if (!candidate.Area.HasValue || candidate.Area.Value <= 0)
            {
                earned += areaWeight * 0.40;
            }
            else if (candidate.Area.Value >= demand.MinArea.Value)
            {
                earned += areaWeight;
                reasons.Add("Đạt diện tích");
            }
            else
            {
                var ratio = Math.Clamp((double)(candidate.Area.Value / demand.MinArea.Value), 0.0, 1.0);
                earned += areaWeight * ratio;
            }
        }

        // 5. Tiêu chí tùy chọn (trọng số 5 mỗi tiêu chí khi người dùng có đặt)
        if (!string.IsNullOrWhiteSpace(demand.Direction))
        {
            const double optWeight = 5.0;
            totalWeight += optWeight;
            if (string.IsNullOrWhiteSpace(candidate.Direction))
                earned += optWeight * 0.5;
            else if (NormalizeDirection(demand.Direction) == NormalizeDirection(candidate.Direction))
            {
                earned += optWeight;
                reasons.Add("Hợp hướng");
            }
        }

        if (!string.IsNullOrWhiteSpace(demand.LegalStatus))
        {
            const double optWeight = 5.0;
            totalWeight += optWeight;
            if (string.IsNullOrWhiteSpace(candidate.LegalStatus))
                earned += optWeight * 0.5;
            else
            {
                var dLeg = NormalizeText(demand.LegalStatus);
                var cLeg = NormalizeText(candidate.LegalStatus);
                if (cLeg.Contains(dLeg) || dLeg.Contains(cLeg) ||
                    ((dLeg.Contains("so do") || dLeg.Contains("so hong")) && (cLeg.Contains("so do") || cLeg.Contains("so hong"))))
                {
                    earned += optWeight;
                    reasons.Add("Đúng pháp lý");
                }
            }
        }

        if (demand.MinBedrooms is > 0)
        {
            const double optWeight = 5.0;
            totalWeight += optWeight;
            if (!candidate.Bedrooms.HasValue)
                earned += optWeight * 0.5;
            else if (candidate.Bedrooms.Value >= demand.MinBedrooms.Value)
            {
                earned += optWeight;
                reasons.Add("Đủ phòng ngủ");
            }
        }

        if (demand.MinFrontageM is > 0)
        {
            const double optWeight = 5.0;
            totalWeight += optWeight;
            if (!candidate.FrontageM.HasValue)
                earned += optWeight * 0.5;
            else if (candidate.FrontageM.Value >= demand.MinFrontageM.Value)
            {
                earned += optWeight;
                reasons.Add("Đạt mặt tiền");
            }
        }

        var scorePercent = totalWeight > 0 ? Math.Round((earned / totalWeight) * 100.0, 1) : 0.0;
        return new BayGiaSimilarityResult(scorePercent, reasons);
    }

    public static List<(BayGiaCandidate Candidate, BayGiaSimilarityResult Similarity)> FilterRecommendations(
        BayGiaDemandProfile demand,
        IEnumerable<BayGiaCandidate> candidates,
        double minSimilarityPercent = DefaultMinSimilarityPercent,
        int maxResults = 18)
    {
        return candidates
            .Select(c => (Candidate: c, Similarity: CalculateSimilarity(demand, c)))
            .Where(x => x.Similarity.ScorePercent >= minSimilarityPercent)
            .OrderByDescending(x => x.Similarity.ScorePercent)
            .ThenByDescending(x => x.Candidate.PublishedAt ?? DateTime.MinValue)
            .Take(maxResults)
            .ToList();
    }
}
