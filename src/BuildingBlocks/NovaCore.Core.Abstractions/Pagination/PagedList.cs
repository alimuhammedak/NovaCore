namespace NovaCore.Core.Abstractions.Pagination;

/// <summary>A single page of results plus the metadata a client needs to navigate.</summary>
public sealed class PagedList<T>
{
    public PagedList(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public long TotalCount { get; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PagedList<T> Empty(int pageSize = PaginationRequest.DefaultPageSize) =>
        new([], 1, pageSize, 0);

    /// <summary>Projects each item without losing the paging metadata.</summary>
    public PagedList<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}
