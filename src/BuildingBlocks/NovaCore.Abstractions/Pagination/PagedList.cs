namespace NovaCore.Abstractions.Pagination;

/// <summary>A single page of results plus the metadata a client needs to navigate.</summary>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed class PagedList<T>
{
    /// <summary>Creates a page.</summary>
    /// <param name="items">The items on this page.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="totalCount">The number of items across all pages.</param>
    public PagedList(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>The 1-based page number.</summary>
    public int Page { get; }

    /// <summary>The requested page size. The last page can hold fewer items.</summary>
    public int PageSize { get; }

    /// <summary>The number of items across all pages.</summary>
    public long TotalCount { get; }

    /// <summary>The number of pages; 0 when <see cref="PageSize"/> is 0.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True when this is not the first page.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>True when a page follows this one.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Projects each item without losing the paging metadata.</summary>
    /// <typeparam name="TOut">The type of the projected items.</typeparam>
    /// <param name="selector">The projection applied to each item.</param>
    public PagedList<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}
