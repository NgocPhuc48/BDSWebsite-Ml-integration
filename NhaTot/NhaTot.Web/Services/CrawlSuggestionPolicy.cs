namespace NhaTot.Web.Services;

public static class CrawlSuggestionPolicy
{
    // Gợi ý theo giá/diện tích; tiêu chí chi tiết khác cần người dùng xác minh ở bài gốc.
    public static bool Fits(decimal? price, decimal? area, decimal? minimumPrice, decimal? maximumPrice, decimal? minimumArea) =>
        price is > 0 && area is > 0 && (!minimumPrice.HasValue || price >= minimumPrice)
        && (!maximumPrice.HasValue || maximumPrice <= 0 || price <= maximumPrice * 1.1m)
        && (!minimumArea.HasValue || area >= minimumArea);

    public static bool FitsRange(decimal? priceMin, decimal? priceMax, decimal? area, decimal? minimumPrice, decimal? maximumPrice, decimal? minimumArea)
    {
        var lo = priceMin ?? priceMax;
        var hi = priceMax ?? priceMin;
        return lo is > 0 && hi is > 0 && hi >= lo && area is > 0
            && (!minimumPrice.HasValue || hi >= minimumPrice)
            && (!maximumPrice.HasValue || maximumPrice <= 0 || lo <= maximumPrice * 1.1m)
            && (!minimumArea.HasValue || area >= minimumArea);
    }
}
