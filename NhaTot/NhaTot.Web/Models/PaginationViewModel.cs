namespace NhaTot.Web.Models;

/// <summary>
/// Shared pagination metadata for user and admin list views.
/// </summary>
public sealed class PaginationViewModel
{
    public int CurrentPage { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages => TotalItems <= 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
    public int StartItem => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

    public PaginationViewModel(int currentPage, int pageSize, int totalItems)
    {
        PageSize = Math.Max(1, pageSize);
        TotalItems = Math.Max(0, totalItems);
        CurrentPage = NormalizePage(currentPage, PageSize, TotalItems);
    }

    public static int NormalizePage(int page, int pageSize, int totalItems)
    {
        var safeSize = Math.Max(1, pageSize);
        var totalPages = totalItems <= 0 ? 1 : (int)Math.Ceiling(totalItems / (double)safeSize);
        return Math.Clamp(page, 1, totalPages);
    }
}
