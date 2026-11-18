namespace NhaTot.Web.Services;

public static class CrawlSuggestionPolicy
{
    // Gợi ý theo giá/diện tích; tiêu chí chi tiết khác cần người dùng xác minh ở bài gốc.
    public static bool Fits(decimal? price, decimal? area, decimal? minimumPrice, decimal? maximumPrice, decimal? minimumArea) =>
        price is > 0 && area is > 0 && (!minimumPrice.HasValue || price >= minimumPrice)
        && (!maximumPrice.HasValue || maximumPrice <= 0 || price <= maximumPrice * 1.1m)
        && (!minimumArea.HasValue || area >= minimumArea);
}
